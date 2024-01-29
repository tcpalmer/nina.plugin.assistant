using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    internal class SyncTakeExposure : SchedulerTakeExposure {
        private ISyncImageSaveWatcher syncImageSaveWatcher;
        private SyncedExposure syncedExposure;
        private Action<string> UpdateDisplayTextAction;
        private ExposurePlan exposurePlan;
        private ExposureTemplate exposureTemplate;
        private Target target;

        private static int exposureCount = 0;

        public SyncTakeExposure(
            ExposurePlan exposurePlan,
            ExposureTemplate exposureTemplate,
            Target target,
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            ISyncImageSaveWatcher syncImageSaveWatcher,
            SyncedExposure syncedExposure,
            Action<String> UpdateDisplayTextAction) : base(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {
            this.exposurePlan = exposurePlan;
            this.exposureTemplate = exposureTemplate;
            this.target = target;
            this.syncImageSaveWatcher = syncImageSaveWatcher;
            this.syncedExposure = syncedExposure;
            this.UpdateDisplayTextAction = UpdateDisplayTextAction;

            Category = PlanTargetContainer.INSTRUCTION_CATEGORY;

            ExposureTime = GetExposureLength();
            Binning = exposureTemplate.BinningMode;
            Gain = GetGain();
            Offset = GetOffset();
            ImageType = CaptureSequence.ImageTypes.LIGHT;
            ROI = target.ROI;

            UpdateDisplayTextAction($"Exposing: {ExposureTime}s, Filter: {exposureTemplate.FilterName}, Gain: {Gain}, Offset: {Offset}, Bin: {Binning}");
            ExposureCount = exposureCount++;
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

            var exposureData = await imagingMediator.CaptureImage(capture, token, progress);
            var imageData = await exposureData.ToImageData(progress, token);
            var imageParams = new PrepareImageParameters(true, true);
            var prepareTask = imagingMediator.PrepareImage(imageData, imageParams, token);

            imageData.MetaData.Target.Name = syncedExposure.TargetName;
            imageData.MetaData.Target.Coordinates = GetCoordinates(syncedExposure);
            imageData.MetaData.Target.PositionAngle = syncedExposure.TargetPositionAngle;

            var root = ItemUtility.GetRootContainer(this.Parent);
            if (root != null) {
                imageData.MetaData.Sequence.Title = root.SequenceTitle;
            }

            syncImageSaveWatcher.WaitForExposure(imageData.MetaData.Image.Id, syncedExposure.TargetDatabaseId, syncedExposure.ExposurePlanDatabaseId, syncedExposure.ExposureId);

            await imageSaveMediator.Enqueue(imageData, prepareTask, progress, token);
            imageHistoryVM.Add(imageData.MetaData.Image.Id, await imageData.Statistics, ImageType);

            ExposureCount++;
        }

        private Coordinates GetCoordinates(SyncedExposure syncedExposure) {
            return new Coordinates(AstroUtil.HMSToDegrees(syncedExposure.TargetRA), AstroUtil.DMSToDegrees(syncedExposure.TargetDec), Epoch.J2000, Coordinates.RAType.Degrees);
        }

        private double GetExposureLength() {
            return exposurePlan.Exposure > 0 ? exposurePlan.Exposure : exposureTemplate.defaultExposure;
        }

        private int GetGain() {
            return exposureTemplate.Gain < 0 ? cameraMediator.GetInfo().DefaultGain : exposureTemplate.Gain;
        }

        private int GetOffset() {
            return exposureTemplate.Offset < 0 ? cameraMediator.GetInfo().DefaultOffset : exposureTemplate.Offset;
        }
    }
}