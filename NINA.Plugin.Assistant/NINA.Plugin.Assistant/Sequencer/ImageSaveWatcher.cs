using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using System;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageSaveWatcher {

        private IImageSaveMediator imageSaveMediator;
        private IPlanTarget planTarget;
        private bool enableGrader;

        public ImageSaveWatcher(IImageSaveMediator imageSaveMediator, IPlanTarget planTarget) {
            this.imageSaveMediator = imageSaveMediator;
            this.planTarget = planTarget;
            this.enableGrader = planTarget.Project.Preferences.EnableGrader;

            imageSaveMediator.ImageSaved += ImageSaved;
            Logger.Trace($"Assistant: start watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
        }

        public void Stop() {
            Logger.Trace($"Assistant: stop watching image saves for {planTarget.Project.Name}/{planTarget.Name}");
            imageSaveMediator.ImageSaved -= ImageSaved;
        }

        private void ImageSaved(object sender, ImageSavedEventArgs msg) {
            if (msg.MetaData.Image.ImageType != "LIGHT") {
                return;
            }

            // TODO: work here needs to be async
            // https://markheath.net/post/starting-threads-in-dotnet

            bool accepted = enableGrader ? new ImageGrader().GradeImage(planTarget, msg) : false;
            Logger.Trace($"Assistant: image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}");

            // HACK
            accepted = true;
            Update(planTarget, msg.Filter, accepted, msg);
        }

        private void Update(IPlanTarget planTarget, string filterName, bool accepted, ImageSavedEventArgs msg) {

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                try {

                    // Update the filter plan record
                    FilterPlan filterPlan = context.GetFilterPlan(planTarget.DatabaseId, filterName);
                    filterPlan.acquired++;

                    if (accepted) {
                        filterPlan.accepted++;
                    }

                    // Save the acquired image record
                    AcquiredImage acquiredImage = new AcquiredImage(planTarget.DatabaseId, msg.MetaData.Image.ExposureStart, filterName, new ImageMetadata(msg));
                    context.AcquiredImageSet.Add(acquiredImage);

                    context.SaveChanges();
                }
                catch (Exception e) {
                    Logger.Error($"Assistant: exception updating database for saved image: {e.Message}\n{e.StackTrace}");
                }
            }
        }
    }
}

