using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Concurrent;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class SyncImageSaveWatcher : ISyncImageSaveWatcher {
        private IProfile profile;
        private ProfilePreference profilePreference;
        private IImageSaveMediator imageSaveMediator;
        private ConcurrentDictionary<int, ExposureDetails> exposureDictionary;
        private IPlanTarget planTarget;

        public SyncImageSaveWatcher(IProfile profile, IImageSaveMediator imageSaveMediator) {
            this.profile = profile;
            this.profilePreference = new SchedulerPlanLoader(profile).GetProfilePreferences();
            this.imageSaveMediator = imageSaveMediator;
            exposureDictionary = new ConcurrentDictionary<int, ExposureDetails>(Environment.ProcessorCount * 2, 31);
        }

        public void Start() {
            exposureDictionary.Clear();
            imageSaveMediator.ImageSaved += ImageSaved;
            imageSaveMediator.BeforeFinalizeImageSaved += BeforeFinalizeImageSaved;
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
            imageSaveMediator.BeforeFinalizeImageSaved -= BeforeFinalizeImageSaved;
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
                TSLogger.Warning("SYNC client failed to get image ID for saved image, aborting image save");
                return;
            }

            ExposureDetails exposureDetails;
            if (!exposureDictionary.TryGetValue((int)imageId, out exposureDetails)) {
                TSLogger.Warning($"SYNC client failed to get exposure details for saved image ID: {imageId}, aborting image save");
                return;
            }

            planTarget = GetPlanTarget(exposureDetails.targetDatabaseId);
            IPlanExposure planExposure = GetPlanExposure(exposureDetails);
            if (planExposure == null) {
                TSLogger.Error($"SYNC client failed to get planExposure for saved image ID: {imageId}, aborting image save");
                return;
            }

            bool enableGrader = planTarget.Project.EnableGrader;
            bool accepted = false;
            string rejectReason = "not graded";

            if (enableGrader) {
                (accepted, rejectReason) = new ImageGrader(profile).GradeImage(planTarget, msg, planExposure.FilterName);
                if (!accepted && profilePreference.EnableMoveRejected) {
                    string dstDir = Path.Combine(Path.GetDirectoryName(msg.PathToImage.LocalPath), ImageSaveWatcher.REJECTED_SUBDIR);
                    TSLogger.Debug($"moving rejected image to {dstDir}");
                    Utils.MoveFile(msg.PathToImage.LocalPath, dstDir);
                }
            }

            TSLogger.Debug($"SYNC client image save for {planTarget.Project.Name}/{planTarget.Name}, filter={planExposure.FilterName}, grader enabled={enableGrader}, accepted={accepted}, rejectReason={rejectReason}, image id={imageId}");
            UpdateDatabase(planTarget, planExposure.FilterName, accepted, rejectReason, msg, imageId, exposureDetails.exposureDatabaseId);

            SyncClient.Instance.SubmitCompletedExposure(exposureDetails.exposureId).Wait();
            exposureDictionary.TryRemove((int)imageId, out exposureDetails);
        }

        private IPlanExposure GetPlanExposure(ExposureDetails exposureDetails) {
            IPlanExposure planExposure = planTarget.ExposurePlans.FirstOrDefault(ep => ep.DatabaseId == exposureDetails.exposureDatabaseId);
            if (planExposure != null) {
                return planExposure;
            }

            return planTarget.CompletedExposurePlans.FirstOrDefault(ep => ep.DatabaseId == exposureDetails.exposureDatabaseId);
        }

        private IPlanTarget GetPlanTarget(int targetDatabaseId) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target target = context.GetTargetOnly(targetDatabaseId);
                target = context.GetTargetByProject(target.ProjectId, targetDatabaseId);

                ProfilePreference profilePreference = context.GetProfilePreference(target.Project.ProfileId, true);
                ExposureCompletionHelper helper = new ExposureCompletionHelper(target.Project.EnableGrader, profilePreference.ExposureThrottle);

                PlanProject planProject = new PlanProject(profile, target.Project, helper);
                return new PlanTarget(planProject, target);
            }
        }

        private void UpdateDatabase(IPlanTarget planTarget, string filterName, bool accepted, string rejectReason, ImageSavedEventArgs msg, int? imageId, int exposureDatabaseId) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                using (var transaction = context.Database.BeginTransaction()) {
                    try {
                        ExposurePlan exposurePlan = null;

                        // Update the exposure plan record
                        exposurePlan = context.GetExposurePlan(exposureDatabaseId);
                        if (exposurePlan != null) {
                            exposurePlan.Acquired++;

                            if (accepted) { exposurePlan.Accepted++; }
                            context.ExposurePlanSet.AddOrUpdate(exposurePlan);
                        } else {
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
                            new ImageMetadata(msg, planTarget.Project.SessionId, planTarget.ROI, exposurePlan?.ExposureTemplate.ReadoutMode));
                        context.AcquiredImageSet.Add(acquiredImage);

                        context.SaveChanges();
                        transaction.Commit();
                    } catch (Exception e) {
                        TSLogger.Error($"SYNC client exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                        SchedulerDatabaseContext.CheckValidationErrors(e);
                    }
                }
            }
        }

        private Task BeforeFinalizeImageSaved(object sender, BeforeFinalizeImageSavedEventArgs args) {
            string sessionIdentifier = new FlatsExpert().FormatSessionIdentifier(planTarget?.Project?.SessionId);
            ImagePattern proto = AssistantPlugin.FlatSessionIdImagePattern;
            args.AddImagePattern(new ImagePattern(proto.Key, proto.Description) { Value = sessionIdentifier });

            string projectName = planTarget?.Project?.Name ?? string.Empty;
            proto = AssistantPlugin.ProjectNameImagePattern;
            args.AddImagePattern(new ImagePattern(proto.Key, proto.Description) { Value = projectName });

            return Task.CompletedTask;
        }

        private string ExposureIdsLog() {
            StringBuilder sb = new StringBuilder();
            foreach (var entry in exposureDictionary) {
                sb.AppendLine($"{entry.Key}: {entry.Value.exposureId}");
            }

            return sb.ToString();
        }
    }

    internal class ExposureDetails {
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