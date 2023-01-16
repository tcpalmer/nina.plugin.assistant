using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.WPF.Base.Interfaces.Mediator;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageGrader {

        public bool GradeImage(IPlanTarget planTarget, ImageSavedEventArgs msg) {
            // TODO: implement
            Logger.Debug($"Assistant: image grade: DEFAULTING TO ACCEPT, FIX");
            return true;
        }

        /* TODO:
         * - examine stats for this image
         * - if we save metadata for previous images in DB, we could detect a significant delta
         */
    }
}
