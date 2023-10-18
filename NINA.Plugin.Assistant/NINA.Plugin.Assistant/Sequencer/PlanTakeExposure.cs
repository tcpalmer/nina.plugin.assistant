using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// Extend TakeExposure so we can establish the association between an image id and the scheduler
    /// exposure plan that initiated the exposure.  We also have a reference to the InstructionWrapper
    /// that contains this so we can handle parent relationships.  Works in conjunction with the provided
    /// IImageSaveWatcher.
    /// 
    /// We also handle ROI < 1 by using parts of TakeSubframeExposure here rather than a separate instruction.
    /// 
    /// This is far from ideal.  If the core TakeExposure instruction changes, we'd be doing something different
    /// until this code was updated.  Ideally, NINA would provide a way to track some metadata or id all the way
    /// through the image pipeline to the save operation.
    /// </summary>
    public class PlanTakeExposure : SchedulerTakeExposure {

        private bool synchronizationEnabled;
        private int syncExposureTimeout;
        private IImageSaveWatcher imageSaveWatcher;
        private IDeepSkyObjectContainer dsoContainer;
        private int exposureDatabaseId;

        public PlanTakeExposure(
            IDeepSkyObjectContainer dsoContainer,
            bool synchronizationEnabled,
            int syncExposureTimeout,
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IImageSaveWatcher imageSaveWatcher,
            int exposureDatabaseId) : base(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {

            this.dsoContainer = dsoContainer;
            this.synchronizationEnabled = synchronizationEnabled;
            this.syncExposureTimeout = syncExposureTimeout;
            this.imageSaveWatcher = imageSaveWatcher;
            this.exposureDatabaseId = exposureDatabaseId;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            var capture = new CaptureSequence() {
                ExposureTime = ExposureTime,
                Binning = Binning,
                Gain = Gain,
                Offset = Offset,
                ImageType = ImageType,
                ProgressExposureCount = ExposureCount,
                TotalExposureCount = ExposureCount + 1,
            };

            ObservableRectangle rect = GetObservableRectangle();
            if (rect != null) {
                capture.EnableSubSample = true;
                capture.SubSambleRectangle = rect;
            }

            var imageParams = new PrepareImageParameters(null, false);
            if (IsLightSequence()) {
                imageParams = new PrepareImageParameters(true, true);
            }

            var target = RetrieveTarget(dsoContainer);

            if (synchronizationEnabled) {
                await TrySendExposureToSecondaries(target, token);
            }

            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);

            var imageData = await exposureData.ToImageData(progress, token);

            var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

            if (target != null) {
                imageData.MetaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                imageData.MetaData.Target.PositionAngle = target.PositionAngle;
            }

            ISequenceContainer parent = dsoContainer.Parent;
            while (parent != null && !(parent is SequenceRootContainer)) {
                parent = parent.Parent;
            }
            if (parent is SequenceRootContainer item) {
                imageData.MetaData.Sequence.Title = item.SequenceTitle;
            }

            // This is a modification to TakeExposure.Execute() (plus the addition of the Wrapper as parent)
            imageSaveWatcher.WaitForExposure(imageData.MetaData.Image.Id, exposureDatabaseId);

            await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);

            if (IsLightSequence()) {
                imageHistoryVM.Add(imageData.MetaData.Image.Id, await imageData.Statistics, ImageType);
            }

            if (synchronizationEnabled) {
                /* I think we have to wait (with timeout) for each client that accepted the exposure to report back
                 * that it's done.  We can't continue until then since next operation could be a dither, slew, etc.
                 */
            }

            ExposureCount++;
        }

        private async Task TrySendExposureToSecondaries(InputTarget target, CancellationToken token) {
            await SyncServer.Instance.SyncExposure(target, token, exposureDatabaseId, syncExposureTimeout);
        }

        private InputTarget RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container.Target;
                }
                else {
                    return RetrieveTarget(parent.Parent);
                }
            }
            else {
                return null;
            }
        }
    }
}
