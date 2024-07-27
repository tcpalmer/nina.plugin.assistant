using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
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

    /// <summary>
    /// Watch for saved images and update the associated exposure plan in the scheduler database.
    ///
    /// This class works in conjunction with PlanTakeExposure (the scheduler version of the core
    /// NINA instruction to take an exposure).  PlanTakeExposure.Execute will call WaitForExposure
    /// to register an image Id with the associated exposure plan database Id and tell the watcher that
    /// it can't stop until all image Ids have come through the pipeline.
    ///
    /// This is crucial: we have to update the exposure plan database for each exposure while handling
    /// the asychronous nature of the NINA image save pipeline.  We also have to ensure we've processed
    /// them all before allowing this watcher to be stopped (which means the planner will be called again
    /// immediately).
    ///
    /// </summary>
    public class ImageSaveWatcher : IImageSaveWatcher {
        public static readonly string REJECTED_SUBDIR = "rejected";

        private IProfile profile;
        private ProfilePreference profilePreference;
        private IImageSaveMediator imageSaveMediator;
        private ConcurrentDictionary<int, int> exposureDictionary;

        private IPlanTarget planTarget;
        private bool enableGrader;

        public ImageSaveWatcher(IProfile profile, IImageSaveMediator imageSaveMediator, IPlanTarget planTarget, bool synchronizationEnabled) {
            this.profile = profile;
            this.profilePreference = new SchedulerPlanLoader(profile).GetProfilePreferences();
            this.imageSaveMediator = imageSaveMediator;
            exposureDictionary = new ConcurrentDictionary<int, int>(Environment.ProcessorCount * 2, 31);
            this.planTarget = planTarget;
            this.enableGrader = planTarget.Project.EnableGrader;
        }

        public void Start() {
            exposureDictionary.Clear();
            imageSaveMediator.ImageSaved += ImageSaved;
            imageSaveMediator.BeforeFinalizeImageSaved += BeforeFinalizeImageSaved;
            TSLogger.Debug($"start watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
        }

        public void WaitForExposure(int imageId, int exposurePlanDatabaseId) {
            TSLogger.Debug($"registering waitFor exposure: iId={imageId} eId={exposurePlanDatabaseId}");
            exposureDictionary.TryAdd(imageId, exposurePlanDatabaseId);
        }

        public void Stop() {
            TSLogger.Debug($"stopping image save watcher, waiting for exposures to complete:\n{ExposureIdsLog()}");

            // We need to wait on all exposures to process (and the database to be updated) before we can stop.
            // Otherwise, the planner will be called again with the exposure plan counts not reflecting the
            // latest exposures.  The wait can be considerable given processing like platesolves for Center After Drift.

            // Poll every 400ms and bail out after 80 secs
            int count = 0;
            while (!exposureDictionary.IsEmpty) {
                if (++count == 200) {
                    TSLogger.Warning($"timed out waiting on all exposures to be processed and scheduler database updated.  Remaining:\n{ExposureIdsLog()}");
                    break;
                }

                Thread.Sleep(400);
            }

            imageSaveMediator.ImageSaved -= ImageSaved;
            imageSaveMediator.BeforeFinalizeImageSaved -= BeforeFinalizeImageSaved;

            TSLogger.Debug($"stopped watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
        }

        private void ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (msg.MetaData.Image.ImageType != "LIGHT") {
                return;
            }

            bool accepted = false;
            string rejectReason = "not graded";

            int? imageId = msg.MetaData?.Image?.Id;
            IPlanExposure planExposure = GetPlanExposure(imageId);
            if (planExposure == null) {
                TSLogger.Error($"failed to get planExposure for image ID: {imageId}, aborting image save");
                return;
            }

            if (enableGrader) {
                (accepted, rejectReason) = new ImageGrader(profile).GradeImage(planTarget, msg, planExposure.FilterName);
                if (!accepted && profilePreference.EnableMoveRejected) {
                    string dstDir = Path.Combine(Path.GetDirectoryName(msg.PathToImage.LocalPath), REJECTED_SUBDIR);
                    TSLogger.Debug($"moving rejected image to {dstDir}");
                    Utils.MoveFile(msg.PathToImage.LocalPath, dstDir);
                }
            }

            TSLogger.Debug($"image save for {planTarget.Project.Name}/{planTarget.Name}, filter={planExposure.FilterName}, grader enabled={enableGrader}, accepted={accepted}, rejectReason={rejectReason}, image id={imageId}");
            UpdateDatabase(planTarget, planExposure, planExposure.FilterName, accepted, rejectReason, msg);

            if (imageId != null) {
                int old;
                exposureDictionary.TryRemove((int)imageId, out old);
            }

            TSLogger.Debug($"ImageSaved: id={imageId}");
        }

        private IPlanExposure GetPlanExposure(int? imageId) {
            IPlanExposure planExposure = null;

            if (imageId != null) {
                int exposureDatabaseId;
                bool found = exposureDictionary.TryGetValue((int)imageId, out exposureDatabaseId);
                if (found) {
                    planExposure = planTarget.ExposurePlans.FirstOrDefault(ep => ep.DatabaseId == exposureDatabaseId);
                    if (planExposure == null) {
                        planExposure = planTarget.CompletedExposurePlans.FirstOrDefault(ep => ep.DatabaseId == exposureDatabaseId);
                    }
                }
            }

            return planExposure;
        }

        private void UpdateDatabase(IPlanTarget planTarget, IPlanExposure planExposure, string filterName, bool accepted, string rejectReason, ImageSavedEventArgs msg) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                using (var transaction = context.Database.BeginTransaction()) {
                    try {
                        // Update the exposure plan record
                        ExposurePlan exposurePlan = context.GetExposurePlan(planExposure.DatabaseId);
                        if (exposurePlan != null) {
                            exposurePlan.Acquired++;
                            if (accepted) { exposurePlan.Accepted++; }
                            context.ExposurePlanSet.AddOrUpdate(exposurePlan);
                        } else {
                            TSLogger.Warning($"failed to get exposure plan for id={planExposure.DatabaseId}");
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
                            new ImageMetadata(msg, planTarget.Project.SessionId, planTarget.ROI, planExposure.ReadoutMode));
                        context.AcquiredImageSet.Add(acquiredImage);

                        context.SaveChanges();
                        transaction.Commit();
                    } catch (Exception e) {
                        TSLogger.Error($"exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                        SchedulerDatabaseContext.CheckValidationErrors(e);
                    }
                }
            }
        }

        private Task BeforeFinalizeImageSaved(object sender, BeforeFinalizeImageSavedEventArgs args) {
            string sessionIdentifier = new FlatsExpert().FormatSessionIdentifier(planTarget.Project.SessionId);
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
                sb.AppendLine($"{entry.Key}: {entry.Value}");
            }

            return sb.ToString();
        }
    }

    public class ImageSaveWatcherEmulator : IImageSaveWatcher {

        public void Start() {
            TSLogger.Debug("Scheduler ImageSaveWatcherEmulator Start");
        }

        public void WaitForExposure(int imageId, int exposurePlanDatabaseId) {
            TSLogger.Debug($"WaitForExposure: iId={imageId} eId={exposurePlanDatabaseId}");
        }

        public void Stop() {
            TSLogger.Debug("Scheduler ImageSaveWatcherEmulator Stop");
        }
    }
}