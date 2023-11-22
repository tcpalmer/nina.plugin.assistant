using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFlatDevice;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Rotator;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Flats")]
    [ExportMetadata("Description", "Flats automation for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerFlats : SequenceItem, IValidatable {

        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IFilterWheelMediator filterWheelMediator;
        private IRotatorMediator rotatorMediator;
        private IFlatDeviceMediator flatDeviceMediator;

        SchedulerDatabaseInteraction database;

        [ImportingConstructor]
        public TargetSchedulerFlats(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM, IFilterWheelMediator filterWheelMediator, IRotatorMediator rotatorMediator, IFlatDeviceMediator flatDeviceMediator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.rotatorMediator = rotatorMediator;
            this.flatDeviceMediator = flatDeviceMediator;
        }

        public TargetSchedulerFlats(TargetSchedulerFlats cloneMe) : this(
            cloneMe.profileService,
            cloneMe.cameraMediator,
            cloneMe.imagingMediator,
            cloneMe.imageSaveMediator,
            cloneMe.imageHistoryVM,
            cloneMe.filterWheelMediator,
            cloneMe.rotatorMediator,
            cloneMe.flatDeviceMediator) {
            CopyMetaData(cloneMe);
        }

        public override void Initialize() {
            database = new SchedulerDatabaseInteraction();
        }

        public override object Clone() {
            return new TargetSchedulerFlats(this);
        }

        public override void AfterParentChanged() {
            Validate();
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            try {
                DisplayText = "Determining needed flats";
                List<LightSession> neededFlats = GetNeededFlats();
                if (neededFlats == null) {
                    DisplayText = "";
                    return;
                }

                // Prep the flat device
                DisplayText = "Preparing flat panel";
                await CloseCover(progress, token);
                await ToggleLight(true, progress, token);

                List<FlatSpec> takenFlats = new List<FlatSpec>();
                foreach (LightSession neededFlat in neededFlats) {
                    if (!takenFlats.Contains(neededFlat.FlatSpec)) {
                        await TakeFlatSet(neededFlat.FlatSpec, progress, token);
                        takenFlats.Add(neededFlat.FlatSpec);
                    }

                    // Write the flat history record
                    using (var context = database.GetContext()) {
                        context.FlatHistorySet.Add(GetFlatHistoryRecord(neededFlat));
                        context.SaveChanges();
                    }
                }

                DisplayText = "";
                await ToggleLight(false, progress, token);
            }
            catch (Exception ex) {
                if (Utils.IsCancelException(ex)) {
                    TSLogger.Warning("TS Flats: sequence was canceled");
                }
                else {
                    TSLogger.Error($"Exception taking flats: {ex.Message}:\n{ex.StackTrace}");
                }

                DisplayText = "";

                if (ex is SequenceEntityFailedException) {
                    throw;
                }

                throw new SequenceEntityFailedException($"exception taking flats: {ex.Message}", ex);
            }

            return;
        }

        private string displayText = "";
        public string DisplayText {
            get => displayText;
            set {
                displayText = value;
                RaisePropertyChanged(nameof(DisplayText));
            }
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();

            CameraInfo cameraInfo = this.cameraMediator.GetInfo();
            if (!cameraInfo.Connected) {
                i.Add(Loc.Instance["LblCameraNotConnected"]);
            }
            else {
                if (!cameraInfo.CanSetGain) {
                    i.Add("camera can't set gain, unusable for Target Scheduler Flats");
                }
                if (!cameraInfo.CanSetOffset) {
                    i.Add("camera can't set offset, unusable for Target Scheduler Flats");
                }
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

        private async Task TakeFlatSet(FlatSpec flatSpec, IProgress<ApplicationStatus> progress, CancellationToken token) {

            TrainedFlatExposureSetting setting = GetTrainedFlatExposureSetting(flatSpec);
            if (setting == null) {
                TSLogger.Warning($"TS Flats: failed to find trained settings for {flatSpec}");
                return;
            }

            int count = profileService.ActiveProfile.FlatWizardSettings.FlatCount;
            DisplayText = $"Taking flat set: {count}x @ {setting.Time}s , panel brightness: {setting.Brightness} for {GetFlatSpecDisplay(flatSpec)}";

            // Set rotation angle, if applicable
            if (rotatorMediator.GetInfo().Connected) {
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

            // TODO: not sure we're handling ExposureCount correctly by repeatedly running TakeExposure.Execute() ...
            //   The file increment is good
            //   Are we getting progress display?

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
            }
        }

        private string GetFlatSpecDisplay(FlatSpec flatSpec) {
            return $"Filter: {flatSpec.FilterName} Gain: {flatSpec.Gain} Offset: {flatSpec.Offset} Binning: {flatSpec.BinningMode} Rotation: {flatSpec.Rotation} ROI: {flatSpec.ROI}";
        }

        private async Task CloseCover(IProgress<ApplicationStatus> progress, CancellationToken token) {

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

        private async Task ToggleLight(bool onOff, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (flatDeviceMediator.GetInfo().LightOn == onOff) {
                return;
            }

            TSLogger.Info($"TS Flats: toggling flat device light: {onOff}");
            await flatDeviceMediator.ToggleLight(onOff, progress, token);

            if (flatDeviceMediator.GetInfo().LightOn != onOff) {
                throw new SequenceEntityFailedException($"Failed to toggle flat panel light");
            }
        }

        private List<LightSession> GetNeededFlats() {
            List<LightSession> neededFlats;
            FlatsExpert flatsExpert = new FlatsExpert();
            DateTime cutoff = DateTime.Now.Date.AddDays(FlatsExpert.ACQUIRED_IMAGES_CUTOFF_DAYS);

            using (var context = database.GetContext()) {
                List<Project> activeProjects = context.GetActiveProjects(profileService.ActiveProfile.Id.ToString());

                // Handle flats taken periodically
                List<Target> targets = flatsExpert.GetTargetsForPeriodicFlats(activeProjects);
                if (targets.Count == 0) {
                    TSLogger.Info("TS Flats: no targets active for periodic flats");
                    return null;
                }

                List<AcquiredImage> acquiredImages = context.GetAcquiredImages(cutoff);
                List<LightSession> lightSessions = flatsExpert.GetLightSessions(targets, acquiredImages);
                if (lightSessions.Count == 0) {
                    TSLogger.Info("TS Flats: no light sessions for targets active for periodic flats");
                    return null;
                }

                List<FlatHistory> takenFlats = context.GetFlatsHistory(targets);
                neededFlats = flatsExpert.GetNeededPeriodicFlats(DateTime.Now, targets, lightSessions, takenFlats);

                // Handle flats taken on target completion
                targets = flatsExpert.GetCompletedTargetsForFlats(activeProjects);
                // TODO HERE
                //neededFlats.AddRange(...);

                if (neededFlats.Count == 0) {
                    TSLogger.Info("TS Flats: all light sessions covered by taken flats history");
                    return null;
                }

                return neededFlats;
            }
        }

        private TrainedFlatExposureSetting GetTrainedFlatExposureSetting(FlatSpec flatSpec) {

            TSLogger.Warning("SPOOFING trained flat settings!");
            return new TrainedFlatExposureSetting() {
                Brightness = 10,
                Time = 2
            };

            /*
            int filterPosition = GetFilterPosition(flatSpec.FilterName);
            if (filterPosition == -1) { return null; }

            Collection<TrainedFlatExposureSetting> settings = profileService.ActiveProfile.FlatDeviceSettings.TrainedFlatExposureSettings;
            return settings.FirstOrDefault(
                setting => setting.Filter == filterPosition
                && setting.Binning.X == flatSpec.BinningMode.X
                && setting.Binning.Y == flatSpec.BinningMode.Y
                && setting.Gain == flatSpec.Gain
                && setting.Offset == flatSpec.Offset);
            */
        }

        private short GetFilterPosition(string filterName) {
            FilterInfo info = Utils.LookupFilter(profileService, filterName);
            if (info != null) {
                return info.Position;
            }

            TSLogger.Error($"No configured filter in filter wheel for filter '{filterName}'");
            return -1;
        }

        private FlatHistory GetFlatHistoryRecord(LightSession neededFlat) {
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
    }
}
