using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using Scheduler.SyncService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Text;
using System.Threading;

namespace Assistant.NINAPlugin.Sequencer {

    public class SyncImageSaveWatcher : ISyncImageSaveWatcher {

        private IProfile profile;
        private IImageSaveMediator imageSaveMediator;
        private ConcurrentDictionary<int, ExposureDetails> exposureDictionary;

        public SyncImageSaveWatcher(IProfile profile, IImageSaveMediator imageSaveMediator) {
            this.profile = profile;
            this.imageSaveMediator = imageSaveMediator;
            exposureDictionary = new ConcurrentDictionary<int, ExposureDetails>(Environment.ProcessorCount * 2, 31);
        }

        public void Start() {
            exposureDictionary.Clear();
            imageSaveMediator.ImageSaved += ImageSaved;
            TSLogger.Debug($"SYNC client start watching image saves");
        }

        public void Stop() {
            // Poll every 400ms and bail out after 80 secs
            int count = 0;
            while (!exposureDictionary.IsEmpty) {
                if (++count == 200) {
                    TSLogger.Warning($"SYNC client timed out waiting on all exposures to be processed and scheduler database updated.  Remaining:\n{ExposureIdsLog()}");
                    break;
                }

                Thread.Sleep(400);
            }

            imageSaveMediator.ImageSaved -= ImageSaved;
        }

        public void WaitForExposure(int imageId, int targetDatabaseId, int exposurePlanDatabaseId, string exposureId) {
            TSLogger.Debug($"SYNC client registering waitFor exposure: iId={imageId} eId={exposurePlanDatabaseId}");
            exposureDictionary.TryAdd(imageId, new ExposureDetails(imageId, exposureId, targetDatabaseId, exposurePlanDatabaseId));
        }

        private void ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (msg.MetaData.Image.ImageType != "LIGHT") {
                return;
            }

            int? imageId = msg.MetaData?.Image?.Id;
            if (imageId == null) {
                TSLogger.Warning("SYNC client failed to get image ID for saved image");
                return;
            }

            ExposureDetails exposureDetails;
            if (!exposureDictionary.TryGetValue((int)imageId, out exposureDetails)) {
                TSLogger.Warning($"SYNC client failed to get exposure details for saved image ID: {imageId}");
                return;
            }

            IPlanTarget planTarget = GetPlanTarget(exposureDetails.targetDatabaseId);
            bool enableGrader = planTarget.Project.EnableGrader;

            bool accepted = false;
            string rejectReason = "not graded";
            if (enableGrader) {
                (accepted, rejectReason) = new ImageGrader(profile).GradeImage(planTarget, msg);
            }

            TSLogger.Debug($"SYNC client image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}, rejectReason={rejectReason}, image id={imageId}");

            UpdateDatabase(planTarget, msg.Filter, accepted, rejectReason, msg, imageId, exposureDetails.exposureDatabaseId);

            SyncClient.Instance.SubmitCompletedExposure(exposureDetails.exposureId).Wait();
            exposureDictionary.TryRemove((int)imageId, out exposureDetails);
        }

        private IPlanTarget GetPlanTarget(int targetDatabaseId) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target target = context.GetTargetOnly(targetDatabaseId);
                target = context.GetTargetByProject(target.ProjectId, targetDatabaseId);
                PlanProject planProject = new PlanProject(profile, target.Project);
                return new PlanTarget(planProject, target);
            }
        }

        private void UpdateDatabase(IPlanTarget planTarget, string filterName, bool accepted, string rejectReason, ImageSavedEventArgs msg, int? imageId, int exposureDatabaseId) {

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                using (var transaction = context.Database.BeginTransaction()) {

                    try {
                        // Update the exposure plan record
                        ExposurePlan exposurePlan = context.GetExposurePlan(exposureDatabaseId);
                        if (exposurePlan != null) {
                            exposurePlan.Acquired++;

                            if (accepted) { exposurePlan.Accepted++; }
                            context.ExposurePlanSet.AddOrUpdate(exposurePlan);
                        }
                        else {
                            TSLogger.Warning($"SYNC client failed to get exposure plan for id={exposureDatabaseId}, image id={imageId}");
                        }

                        // Save the acquired image record
                        AcquiredImage acquiredImage = new AcquiredImage(
                            profile.Id.ToString(),
                            planTarget.Project.DatabaseId,
                            planTarget.DatabaseId,
                            msg.MetaData.Image.ExposureStart,
                            filterName,
                            accepted,
                            rejectReason,
                            new ImageMetadata(msg, planTarget.ROI));
                        context.AcquiredImageSet.Add(acquiredImage);

                        context.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception e) {
                        TSLogger.Error($"SYNC client exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }

        private string ExposureIdsLog() {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in exposureDictionary) {
                sb.AppendLine($"{entry.Key}: {entry.Value.exposureId}");
            }

            return sb.ToString();
        }

    }

    class ExposureDetails {

        public int imageId { get; private set; }
        public string exposureId { get; private set; }
        public int targetDatabaseId { get; private set; }
        public int exposureDatabaseId { get; private set; }

        public ExposureDetails(int imageId, string exposureId, int targetDatabaseId, int exposureDatabaseId) {
            this.imageId = imageId;
            this.exposureId = exposureId;
            this.targetDatabaseId = targetDatabaseId;
            this.exposureDatabaseId = exposureDatabaseId;
        }
    }
}
