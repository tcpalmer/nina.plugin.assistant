using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.Utility;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    class SyncTakeExposure : SchedulerTakeExposure {

        private ISyncImageSaveWatcher syncImageSaveWatcher;
        private SyncedExposure syncedExposure;
        private ExposurePlan exposurePlan;
        private ExposureTemplate exposureTemplate;
        private Target target;

        private readonly IRotatorMediator rotatorMediator;
        private readonly IFilterWheelMediator filterWheelMediator;

        private static int exposureCount = 0;

        public SyncTakeExposure(
            IProfileService profileService,
            IRotatorMediator rotatorMediator,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IFilterWheelMediator filterWheelMediator,
            ISyncImageSaveWatcher syncImageSaveWatcher,
            SyncedExposure syncedExposure) : base(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {

            this.rotatorMediator = rotatorMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.syncImageSaveWatcher = syncImageSaveWatcher;
            this.syncedExposure = syncedExposure;

            LoadExposureDetails();

            ExposureTime = GetExposureLength();
            Binning = exposureTemplate.BinningMode;
            Gain = GetGain();
            Offset = GetOffset();
            ImageType = CaptureSequence.ImageTypes.LIGHT;
            ROI = target.ROI;

            ExposureCount = exposureCount++;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            await SwitchFilter(progress, token);
            await SetReadoutMode(progress, token);

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

        private void LoadExposureDetails() {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                this.exposurePlan = context.GetExposurePlan(syncedExposure.ExposurePlanDatabaseId);
                this.exposureTemplate = GetExposureTemplate(context, exposurePlan);
                this.target = GetTarget(context, exposurePlan);
            }
        }

        private ExposureTemplate GetExposureTemplate(SchedulerDatabaseContext context, ExposurePlan exposurePlan) {

            // Get the template being used by the server instance
            ExposureTemplate serverExposureTemplate = context.GetExposureTemplate(exposurePlan.ExposureTemplateId);

            // If this (client) instance has a template by the same name, use that
            List<ExposureTemplate> list = context.GetExposureTemplates(SyncClient.Instance.ProfileId);
            foreach (ExposureTemplate et in list) {
                if (et.Name == serverExposureTemplate.Name) {
                    return et;
                }
            }

            // Otherwise use the same as the server
            return serverExposureTemplate;
        }

        private Target GetTarget(SchedulerDatabaseContext context, ExposurePlan exposurePlan) {
            return context.GetTargetOnly(exposurePlan.TargetId);
        }

        private async Task SwitchFilter(IProgress<ApplicationStatus> progress, CancellationToken token) {
            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            switchFilter.Filter = Utils.LookupFilter(profileService, exposureTemplate.FilterName);
            await switchFilter.Execute(progress, token);
        }

        private async Task SetReadoutMode(IProgress<ApplicationStatus> progress, CancellationToken token) {
            SetReadoutMode setReadoutMode = new SetReadoutMode(cameraMediator);
            setReadoutMode.Mode = GetReadoutMode(exposureTemplate.ReadoutMode);
            await setReadoutMode.Execute(progress, token);
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

        private short GetReadoutMode(int? readoutMode) {
            return (short)((readoutMode == null || readoutMode < 0) ? 0 : readoutMode);
        }
    }
}
