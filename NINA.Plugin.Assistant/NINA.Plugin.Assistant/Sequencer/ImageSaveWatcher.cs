using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;

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

            // TODO: work here needs to be async
            // https://markheath.net/post/starting-threads-in-dotnet

            bool accepted = enableGrader ? new ImageGrader().GradeImage(planTarget, msg) : false;
            Logger.Debug($"Scheduler: image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}");

            // HACK
            accepted = true;
            // TODO: Need a way to NOT do this for the emulator
            // TODO: But for a 'perfect plan' we do need to update proxy exposure plans so it thinks it's making progress
            //Update(planTarget, msg.Filter, accepted, msg);
        }

        /* TODO: can't do this until we figure out how to find the EP applicable for this so it can be updated ...
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
        }*/
    }
}

