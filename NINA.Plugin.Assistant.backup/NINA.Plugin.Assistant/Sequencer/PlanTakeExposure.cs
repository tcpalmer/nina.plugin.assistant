using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
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
    public class PlanTakeExposure : TakeExposure {

        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IImageSaveWatcher imageSaveWatcher;
        private IDeepSkyObjectContainer dsoContainer;
        private int exposureDatabaseId;

        private double roi;

        public double ROI {
            get => roi;
            set {
                if (value <= 0) { value = 1; }
                if (value > 1) { value = 1; }
                roi = value;
                RaisePropertyChanged();
            }
        }

        public PlanTakeExposure(
            IDeepSkyObjectContainer dsoContainer,
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IImageSaveWatcher imageSaveWatcher,
            int exposureDatabaseId) : base(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {

            this.dsoContainer = dsoContainer;

            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;

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

            // From NINA TakeSubframeExposure
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

            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);

            var imageData = await exposureData.ToImageData(progress, token);

            var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

            if (target != null) {
                imageData.MetaData.Target.Name = target.DeepSkyObject.NameAsAscii;
                imageData.MetaData.Target.Coordinates = target.InputCoordinates.Coordinates;
                imageData.MetaData.Target.Rotation = target.Rotation;
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

            ExposureCount++;
        }

        private ObservableRectangle GetObservableRectangle() {
            var info = cameraMediator.GetInfo();
            ObservableRectangle rect = null;

            if (ROI < 1 && info.CanSubSample) {
                var centerX = info.XSize / 2d;
                var centerY = info.YSize / 2d;
                var subWidth = info.XSize * ROI;
                var subHeight = info.YSize * ROI;
                var startX = centerX - subWidth / 2d;
                var startY = centerY - subHeight / 2d;
                rect = new ObservableRectangle(startX, startY, subWidth, subHeight);
            }

            if (ROI < 1 && !info.CanSubSample) {
                TSLogger.Warning($"ROI {ROI} was specified, but the camera is not able to take sub frames");
                Logger.Warning($"ROI {ROI} was specified, but the camera is not able to take sub frames");
            }

            return rect;
        }

        private bool IsLightSequence() {
            return ImageType == CaptureSequence.ImageTypes.SNAPSHOT || ImageType == CaptureSequence.ImageTypes.LIGHT;
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
