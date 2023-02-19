using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Data.Entity.Migrations;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageSaveWatcher {

        private IImageSaveMediator imageSaveMediator;
        private IPlanTarget planTarget;
        private bool enableGrader;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator, IPlanTarget planTarget) {
            this.imageSaveMediator = imageSaveMediator;
            this.planTarget = planTarget;
            this.enableGrader = planTarget.Project.EnableGrader;

            imageSaveMediator.ImageSaved += ImageSaved;
            Logger.Debug($"Assistant: start watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
        }

        public void Stop() {
            Logger.Debug($"Assistant: stop watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
            imageSaveMediator.ImageSaved -= ImageSaved;
        }

        private void ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (msg.MetaData.Image.ImageType != "LIGHT") {
                return;
            }

            // TODO: work here needs to be async
            // https://markheath.net/post/starting-threads-in-dotnet

            bool accepted = enableGrader ? new ImageGrader().GradeImage(planTarget, msg) : false;
            Logger.Debug($"Assistant: image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}");

            // HACK
            accepted = true;
            //Update(planTarget, msg.Filter, accepted, msg);  NEED A WAY TO NOT DO THIS FOR EMULATOR
        }

        private void Update(IPlanTarget planTarget, string filterName, bool accepted, ImageSavedEventArgs msg) {

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                using (var transaction = context.Database.BeginTransaction()) {

                    try {
                        // Update the filter plan record
                        ExposurePlan exposurePlan = context.GetExposurePlan(planTarget.DatabaseId, filterName);
                        exposurePlan.Acquired++;

                        if (accepted) {
                            exposurePlan.Accepted++;
                        }

                        context.ExposurePlanSet.AddOrUpdate(exposurePlan);

                        // Save the acquired image record
                        AcquiredImage acquiredImage = new AcquiredImage(planTarget.DatabaseId, msg.MetaData.Image.ExposureStart, filterName, new ImageMetadata(msg));
                        context.AcquiredImageSet.Add(acquiredImage);

                        context.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception e) {
                        Logger.Error($"Assistant: exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
    }
}

