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

            bool accepted = enableGrader ? new ImageGrader().GradeImage(planTarget, msg) : false;
            Logger.Trace($"Assistant: image save for {planTarget.Project.Name}/{planTarget.Name}, filter={msg.Filter}, grader enabled={enableGrader}, accepted={accepted}");

            /* TODO: Update the database.  Load the DB project/target matching planTarget
             *  Match the filter used for this image (msg.Filter) to the planTarget.FilterPlans[i]
             *   planFilter.Acquired++
             *   planFilter.Accepted++ if accepted
             */
        }
    }
}

