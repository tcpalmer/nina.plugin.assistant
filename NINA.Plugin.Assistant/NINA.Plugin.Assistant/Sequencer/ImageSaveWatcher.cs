using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Data.Entity.Migrations;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageSaveWatcher : IImageSaveWatcher {

        private IImageSaveMediator imageSaveMediator;
        private IPlanTarget planTarget;
        private bool enableGrader;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator, IPlanTarget planTarget) {
            this.imageSaveMediator = imageSaveMediator;
            this.planTarget = planTarget;
            this.enableGrader = planTarget.Project.EnableGrader;
        }

        public int PlanExposureDatabaseId { get; set; }

        public void Start() {
            imageSaveMediator.ImageSaved += ImageSaved;
            Logger.Debug($"Scheduler: start watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
        }

        public void Stop() {
            Logger.Debug($"Scheduler: stop watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
            imageSaveMediator.ImageSaved -= ImageSaved;
        }

        private void ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (msg.MetaData.Image.ImageType != "LIGHT") {
                return;
            }

            bool accepted = enableGrader ? new ImageGrader().GradeImage(planTarget, msg) : false;
            Logger.Debug($"Scheduler: image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}");

            Update(planTarget, msg.Filter, accepted, msg);
        }

        private void Update(IPlanTarget planTarget, string filterName, bool accepted, ImageSavedEventArgs msg) {

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                using (var transaction = context.Database.BeginTransaction()) {

                    try {
                        // Update the exposure plan record
                        ExposurePlan exposurePlan = context.GetExposurePlan(PlanExposureDatabaseId);
                        exposurePlan.Acquired++;

                        if (accepted) {
                            exposurePlan.Accepted++;
                        }

                        context.ExposurePlanSet.AddOrUpdate(exposurePlan);

                        // Save the acquired image record
                        AcquiredImage acquiredImage = new AcquiredImage(
                            planTarget.Project.DatabaseId,
                            planTarget.DatabaseId,
                            msg.MetaData.Image.ExposureStart,
                            filterName,
                            accepted,
                            new ImageMetadata(msg));
                        context.AcquiredImageSet.Add(acquiredImage);

                        context.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception e) {
                        Logger.Error($"Scheduler: exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
    }

    public class ImageSaveWatcherEmulator : IImageSaveWatcher {
        public int PlanExposureDatabaseId { get; set; }

        public void Start() {
            Logger.Debug("Scheduler ImageSaveWatcherEmulator Start");
        }

        public void Stop() {
            Logger.Debug("Scheduler ImageSaveWatcherEmulator Stop");
        }
    }

    public class Fooster : IImageSaveWatcher {
        public int PlanExposureDatabaseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Start() {
            throw new NotImplementedException();
        }

        public void Stop() {
            throw new NotImplementedException();
        }
    }
}

