using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Equipment.MyRotator;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.FlatDevice;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public abstract class TargetSchedulerFlatsBase : SequenceItem, IValidatable {

        protected IProfileService profileService;
        protected ICameraMediator cameraMediator;
        protected IImagingMediator imagingMediator;
        protected IImageSaveMediator imageSaveMediator;
        protected IImageHistoryVM imageHistoryVM;
        protected IFilterWheelMediator filterWheelMediator;
        protected IRotatorMediator rotatorMediator;
        protected IFlatDeviceMediator flatDeviceMediator;

        protected SchedulerDatabaseInteraction database;

        public TargetSchedulerFlatsBase(IProfileService profileService,
                                        ICameraMediator cameraMediator,
                                        IImagingMediator imagingMediator,
                                        IImageSaveMediator imageSaveMediator,
                                        IImageHistoryVM imageHistoryVM,
                                        IFilterWheelMediator filterWheelMediator,
                                        IRotatorMediator rotatorMediator,
                                        IFlatDeviceMediator flatDeviceMediator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.rotatorMediator = rotatorMediator;
            this.flatDeviceMediator = flatDeviceMediator;
        }

        public override void Initialize() {
            database = new SchedulerDatabaseInteraction();
            Validate();
        }

        public override void AfterParentChanged() {
            Validate();
        }

        private bool alwaysRepeatFlatSet = true;

        [JsonProperty]
        public bool AlwaysRepeatFlatSet {
            get => alwaysRepeatFlatSet;
            set {
                alwaysRepeatFlatSet = value;
                RaisePropertyChanged(nameof(AlwaysRepeatFlatSet));
            }
        }

        private string displayText = "";
        public string DisplayText {
            get => displayText;
            set {
                displayText = value;
                RaisePropertyChanged(nameof(DisplayText));
            }
        }

        private int iterations = 0;
        public int Iterations {
            get => iterations;
            set {
                iterations = value;
                RaisePropertyChanged(nameof(Iterations));
            }
        }

        private int completedIterations = 0;
        public int CompletedIterations {
            get => completedIterations;
            set {
                completedIterations = value;
                RaisePropertyChanged(nameof(CompletedIterations));
            }
        }

        private string targetName = null;
        public string TargetName { get => targetName; set => targetName = value; }

        protected async Task<bool> TakeFlatSet(FlatSpec flatSpec, bool applyRotation, IProgress<ApplicationStatus> progress, CancellationToken token) {

            try {
                TrainedFlatExposureSetting setting = GetTrainedFlatExposureSetting(flatSpec);
                if (setting == null) {
                    TSLogger.Warning($"TS Flats: failed to find trained settings for {flatSpec}");
                    Notification.ShowWarning($"TS Flats: failed to find trained settings for {flatSpec}");
                    return false;
                }

                int count = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
                DisplayText = $"{flatSpec.FilterName} {setting.Time.ToString("0.##")}s ({GetFlatSpecDisplay(flatSpec)})";
                Iterations = count;
                CompletedIterations = 0;

                // Set rotation angle, if applicable
                if (applyRotation && flatSpec.Rotation != ImageMetadata.NO_ROTATOR_ANGLE && rotatorMediator.GetInfo().Connected) {
                    TSLogger.Info($"TS Flats: setting rotation angle: {flatSpec.Rotation}");
                    MoveRotatorMechanical rotate = new MoveRotatorMechanical(rotatorMediator) { MechanicalPosition = (float)flatSpec.Rotation };
                    await rotate.Execute(progress, token);
                }

                // Set the camera readout mode
                TSLogger.Info($"TS Flats: setting readout mode: {flatSpec.ReadoutMode}");
                SetReadoutMode setReadoutMode = new SetReadoutMode(cameraMediator) { Mode = (short)flatSpec.ReadoutMode };
                await setReadoutMode.Execute(progress, token);

                // Switch filters
                TSLogger.Info($"TS Flats: switching filter: {flatSpec.FilterName}");
                SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator) { Filter = Utils.LookupFilter(profileService, flatSpec.FilterName) };
                await switchFilter.Execute(progress, token);

                // Set the panel brightness
                TSLogger.Info($"TS Flats: setting panel brightness: {setting.Brightness}");
                SetBrightness setBrightness = new SetBrightness(flatDeviceMediator) { Brightness = setting.Brightness };
                await setBrightness.Execute(progress, token);

                // Take the exposures
                TakeSubframeExposure takeExposure = new TakeSubframeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM) {
                    ImageType = CaptureSequence.ImageTypes.FLAT,
                    ExposureCount = 0,
                    Gain = flatSpec.Gain,
                    Offset = flatSpec.Offset,
                    Binning = flatSpec.BinningMode,
                    ExposureTime = setting.Time,
                    ROI = flatSpec.ROI
                };

                TSLogger.Info($"TS Flats: taking {count} flats: exp:{setting.Time}, brightness: {setting.Brightness}, for {flatSpec}");

                for (int i = 0; i < count; i++) {
                    await takeExposure.Execute(progress, token);
                    CompletedIterations++;
                }

                return true;
            }
            catch (Exception ex) {
                TSLogger.Error($"Exception taking automated flat: {ex.Message}\n{ex}");
                return false;
            }
        }

        protected void SetTargetName(int targetId) {
            using (var context = database.GetContext()) {
                Target target = context.GetTargetOnly(targetId);
                TargetName = target?.Name;
            }
        }

        // TODO: replace with ImageSaveMediator_BeforeFinalizeImageSaved below
        protected Task BeforeImageSaved(object sender, BeforeImageSavedEventArgs args) {
            if (string.IsNullOrEmpty(args.Image.MetaData.Target.Name) && TargetName != null) {
                args.Image.MetaData.Target.Name = TargetName;

                // TODO: is there not another way to get TARGETNAME set 
                /// It is possible to wait for the image processing by awaiting the BeforeFinalizeImageSavedEventArgs.ImagePrepareTask if necessary
                //await args.ImagePrepareTask;
                //
                // Or some sort of closure that wraps the current TNAME just for 1 flat set?
            }

            return Task.CompletedTask;
        }

        /*
      private Task ImageSaveMediator_BeforeFinalizeImageSaved(object sender, BeforeFinalizeImageSavedEventArgs e) {
            // Populate the example image pattern with data. This can provide data that may not be immediately available
            e.AddImagePattern(new ImagePattern(exampleImagePattern.Key, exampleImagePattern.Description, exampleImagePattern.Category) {
                Value = $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.ffffffK}"
            });

            return Task.CompletedTask;
        }
         */

        protected void SaveFlatHistory(LightSession neededFlat) {
            if (database == null) {
                database = new SchedulerDatabaseInteraction();
            }

            TSLogger.Info($"TS Flats: saving flat history: {neededFlat}");
            using (var context = database.GetContext()) {
                context.FlatHistorySet.Add(GetFlatHistoryRecord(neededFlat));
                context.SaveChanges();
            }
        }

        protected string GetFlatSpecDisplay(FlatSpec flatSpec) {
            string rot = flatSpec.Rotation != ImageMetadata.NO_ROTATOR_ANGLE ? flatSpec.Rotation.ToString() : "n/a";
            return $"Filter: {flatSpec.FilterName} Gain: {flatSpec.Gain} Offset: {flatSpec.Offset} Binning: {flatSpec.BinningMode} Rotation: {rot} ROI: {flatSpec.ROI}";
        }

        protected async Task CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token) {

            if (!flatDeviceMediator.GetInfo().SupportsOpenClose) {
                return;
            }

            CoverState coverState = flatDeviceMediator.GetInfo().CoverState;

            // Last chance to skip if flat device doesn't support open/close
            if (coverState == CoverState.Unknown || coverState == CoverState.NeitherOpenNorClosed) {
                return;
            }

            if (coverState == CoverState.Closed) {
                return;
            }

            TSLogger.Info("TS Flats: closing flat device");
            await flatDeviceMediator.CloseCover(progress, token);

            coverState = flatDeviceMediator.GetInfo().CoverState;
            if (coverState != CoverState.Closed) {
                throw new SequenceEntityFailedException($"Failed to close flat cover");
            }
        }

        protected async Task ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (flatDeviceMediator.GetInfo().LightOn == onOff) {
                return;
            }

            TSLogger.Info($"TS Flats: toggling flat device light: {onOff}");
            await flatDeviceMediator.ToggleLight(onOff, progress, token);

            if (flatDeviceMediator.GetInfo().LightOn != onOff) {
                throw new SequenceEntityFailedException($"Failed to toggle flat panel light");
            }
        }

        protected double GetCurrentRotation() {
            RotatorInfo info = rotatorMediator.GetInfo();
            return info.Connected ? info.MechanicalPosition : ImageMetadata.NO_ROTATOR_ANGLE;
        }

        public bool Validate() {
            var i = new List<string>();

            CameraInfo cameraInfo = this.cameraMediator.GetInfo();
            if (!cameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }

            FlatDeviceInfo flatDeviceInfo = flatDeviceMediator.GetInfo();
            if (!flatDeviceInfo.Connected) {
                i.Add(Loc.Instance["LblFlatDeviceNotConnected"]);
            }
            else {
                if (!flatDeviceInfo.SupportsOnOff) {
                    i.Add(Loc.Instance["LblFlatDeviceCannotControlBrightness"]);
                }
            }

            Issues = i;
            return i.Count == 0;
        }

        protected TrainedFlatExposureSetting GetTrainedFlatExposureSetting(FlatSpec flatSpec) {

            int filterPosition = GetFilterPosition(flatSpec.FilterName);
            if (filterPosition == -1) { return null; }

            Collection<TrainedFlatExposureSetting> settings = profileService.ActiveProfile.FlatDeviceSettings.TrainedFlatExposureSettings;
            TrainedFlatExposureSetting setting;

            // Exact match?
            setting = settings.FirstOrDefault(
                setting => setting.Filter == filterPosition
                && setting.Binning.X == flatSpec.BinningMode.X
                && setting.Binning.Y == flatSpec.BinningMode.Y
                && setting.Gain == flatSpec.Gain
                && setting.Offset == flatSpec.Offset);
            if (setting != null) { return setting; }

            // Match without gain?
            setting = settings.FirstOrDefault(
                x => x.Filter == filterPosition
                && x.Binning.X == flatSpec.BinningMode.X
                && x.Binning.Y == flatSpec.BinningMode.Y
                && x.Gain == -1
                && x.Offset == flatSpec.Offset);
            if (setting != null) { return setting; }

            // Match without offset?
            setting = settings.FirstOrDefault(
                x => x.Filter == filterPosition
                && x.Binning.X == flatSpec.BinningMode.X
                && x.Binning.Y == flatSpec.BinningMode.Y
                && x.Gain == flatSpec.Gain
                && x.Offset == -1);
            if (setting != null) { return setting; }

            // Match without gain or offset?
            setting = settings.FirstOrDefault(
                x => x.Filter == filterPosition
                && x.Binning.X == flatSpec.BinningMode.X
                && x.Binning.Y == flatSpec.BinningMode.Y
                && x.Gain == -1
                && x.Offset == -1);

            return setting;
        }

        protected short GetFilterPosition(string filterName) {
            FilterInfo info = Utils.LookupFilter(profileService, filterName);
            if (info != null) {
                return info.Position;
            }

            TSLogger.Error($"No configured filter in filter wheel for filter '{filterName}'");
            return -1;
        }

        protected void LogTrainedFlatDetails() {
            Collection<TrainedFlatExposureSetting> settings = profileService.ActiveProfile.FlatDeviceSettings?.TrainedFlatExposureSettings;

            /* Write training flats for testing.
            BinningMode binning = new BinningMode(1, 1);

            settings.Add(new TrainedFlatExposureSetting() { Filter = 0, Gain = -1, Offset = -1, Binning = binning, Time = 0.78125, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 1, Gain = -1, Offset = -1, Binning = binning, Time = 4.0625, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 2, Gain = -1, Offset = -1, Binning = binning, Time = 2.875, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 3, Gain = -1, Offset = -1, Binning = binning, Time = 2.28125, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 4, Gain = -1, Offset = -1, Binning = binning, Time = 8.8125, Brightness = 30 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 5, Gain = -1, Offset = -1, Binning = binning, Time = 9.125, Brightness = 40 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 6, Gain = -1, Offset = -1, Binning = binning, Time = 6.25, Brightness = 30 });

            settings.Add(new TrainedFlatExposureSetting() { Filter = 0, Gain = 139, Offset = 21, Binning = binning, Time = 0.78125, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 1, Gain = 139, Offset = 21, Binning = binning, Time = 4.0625, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 2, Gain = 139, Offset = 21, Binning = binning, Time = 2.875, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 3, Gain = 139, Offset = 21, Binning = binning, Time = 2.28125, Brightness = 21 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 4, Gain = 139, Offset = 21, Binning = binning, Time = 8.8125, Brightness = 30 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 5, Gain = 139, Offset = 21, Binning = binning, Time = 9.125, Brightness = 40 });
            settings.Add(new TrainedFlatExposureSetting() { Filter = 6, Gain = 139, Offset = 21, Binning = binning, Time = 6.25, Brightness = 30 });
            */

            if (settings == null || settings.Count == 0) {
                TSLogger.Debug("TS Flats: no trained flat exposure details found");
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (TrainedFlatExposureSetting trainedFlat in settings) {
                sb.AppendLine($"    filter pos: {trainedFlat.Filter} gain: {trainedFlat.Gain} offset: {trainedFlat.Offset} binning: {trainedFlat.Binning} exposure: {trainedFlat.Time} brightness: {trainedFlat.Brightness}");
            }

            TSLogger.Debug($"TS Flats: trained flat exposure details:\n{sb}");
        }

        protected FlatHistory GetFlatHistoryRecord(LightSession neededFlat) {
            return new FlatHistory(neededFlat.TargetId,
                neededFlat.SessionDate,
                DateTime.Now,
                profileService.ActiveProfile.Id.ToString(),
                FlatHistory.FLAT_TYPE_PANEL,
                neededFlat.FlatSpec.FilterName,
                neededFlat.FlatSpec.Gain,
                neededFlat.FlatSpec.Offset,
                neededFlat.FlatSpec.BinningMode,
                neededFlat.FlatSpec.ReadoutMode,
                neededFlat.FlatSpec.Rotation,
                neededFlat.FlatSpec.ROI);
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }
    }
}
