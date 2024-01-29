using Assistant.NINAPlugin.Sync;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
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
    /// If synchronization is enabled, we handle getting exposures to registered clients and then waiting for
    /// them to complete.
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
        private int targetDatabaseId;
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
            int targetDatabaseId,
            int exposureDatabaseId) : base(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {
            this.dsoContainer = dsoContainer;
            this.synchronizationEnabled = synchronizationEnabled;
            this.syncExposureTimeout = syncExposureTimeout;
            this.imageSaveWatcher = imageSaveWatcher;
            this.targetDatabaseId = targetDatabaseId;
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

            string exposureId = "";
            if (synchronizationEnabled) {
                exposureId = Guid.NewGuid().ToString();
                progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for sync clients to accept exposure" });
                await TrySendExposureToClients(exposureId, target, token);
                progress?.Report(new ApplicationStatus() { Status = "" });
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

            // If any sync clients accepted this exposure, we have to wait for them to finish before continuing
            if (synchronizationEnabled) {
                progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for sync clients to complete exposure" });
                await SyncServer.Instance.WaitForClientExposureCompletion(exposureId, token);
                progress?.Report(new ApplicationStatus() { Status = "" });
            }

            ExposureCount++;
        }

        private async Task TrySendExposureToClients(string exposureId, InputTarget target, CancellationToken token) {
            await SyncServer.Instance.SyncExposure(exposureId, target, targetDatabaseId, exposureDatabaseId, syncExposureTimeout, token);
        }

        private InputTarget RetrieveTarget(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return container.Target;
                } else {
                    return RetrieveTarget(parent.Parent);
                }
            } else {
                return null;
            }
        }
    }
}