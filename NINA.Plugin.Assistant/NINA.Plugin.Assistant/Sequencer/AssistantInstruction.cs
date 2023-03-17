using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler")]
    [ExportMetadata("Description", "Run the Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AssistantInstruction : SequenceItem, IValidatable {

        private readonly IProfileService profileService;
        private readonly IList<IDateTimeProvider> dateTimeProviders;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM imageHistoryVM;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IDomeMediator domeMediator;
        private readonly IDomeFollower domeFollower;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly INighttimeCalculator nighttimeCalculator;
        private readonly IWindowServiceFactory windowServiceFactory;

        public int TotalExposureCount { get; set; }

        [ImportingConstructor]
        public AssistantInstruction(
                IProfileService profileService,
                IList<IDateTimeProvider> dateTimeProviders,
                ITelescopeMediator telescopeMediator,
                IRotatorMediator rotatorMediator,
                IGuiderMediator guiderMediator,
                ICameraMediator cameraMediator,
                IImagingMediator imagingMediator,
                IImageSaveMediator imageSaveMediator,
                IImageHistoryVM imageHistoryVM,
                IFilterWheelMediator filterWheelMediator,
                IDomeMediator domeMediator,
                IDomeFollower domeFollower,
                IPlateSolverFactory plateSolverFactory,
                INighttimeCalculator nighttimeCalculator,
                IWindowServiceFactory windowServiceFactory
            ) {

            this.profileService = profileService;
            this.dateTimeProviders = dateTimeProviders;
            this.telescopeMediator = telescopeMediator;
            this.rotatorMediator = rotatorMediator;
            this.guiderMediator = guiderMediator;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.domeMediator = domeMediator;
            this.domeFollower = domeFollower;
            this.plateSolverFactory = plateSolverFactory;
            this.nighttimeCalculator = nighttimeCalculator;
            this.windowServiceFactory = windowServiceFactory;

            // TODO: this can better be set via the ... on the instruction (see Smart Exposure)
            // Interestingly, DSO Container doesn't have ... but this instruction does.
            // TODO: also need to pay attention to Attempts - can also be set via ...
            //Attempts = 1;
            //ErrorBehavior = InstructionErrorBehavior.SkipInstructionSetOnError;

            TotalExposureCount = -1;
            ClearTarget();
        }

        public AssistantInstruction(AssistantInstruction cloneMe) : this(
                cloneMe.profileService,
                cloneMe.dateTimeProviders,
                cloneMe.telescopeMediator,
                cloneMe.rotatorMediator,
                cloneMe.guiderMediator,
                cloneMe.cameraMediator,
                cloneMe.imagingMediator,
                cloneMe.imageSaveMediator,
                cloneMe.imageHistoryVM,
                cloneMe.filterWheelMediator,
                cloneMe.domeMediator,
                cloneMe.domeFollower,
                cloneMe.plateSolverFactory,
                cloneMe.nighttimeCalculator,
                cloneMe.windowServiceFactory
            ) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AssistantInstruction(this) { };
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
        }

        public override void Initialize() {
            Logger.Debug("Scheduler instruction: Initialize");

            if (StatusMonitor != null) {
                StatusMonitor.Reset();
                StatusMonitor.PropertyChanged -= StatusMonitor_PropertyChanged;
            }

            StatusMonitor = new AssistantStatusMonitor();
            StatusMonitor.PropertyChanged += StatusMonitor_PropertyChanged;
        }

        public override void ResetProgress() {
            Logger.Debug("Scheduler instruction: ResetProgress");
            StatusMonitor.Reset();
            base.ResetProgress();
        }

        public override void Teardown() {
            Logger.Debug("Scheduler instruction: Teardown");
            base.Teardown();
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(AssistantInstruction)}";
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("Scheduler instruction: Execute");

            IPlanTarget previousPlanTarget = null;

            while (true) {
                DateTime atTime = DateTime.Now;
                AssistantPlan plan = new Planner(atTime, profileService).GetPlan(previousPlanTarget);

                if (plan == null) {
                    Logger.Info("Scheduler: planner returned empty plan, done");
                    return Task.CompletedTask;
                }

                if (plan.WaitForNextTargetTime != null) {
                    Logger.Info("Scheduler: planner waiting for next target to become available");
                    StatusMonitor.BeginWait((DateTime)plan.WaitForNextTargetTime);
                    WaitForNextTarget(plan.WaitForNextTargetTime, progress, token);
                    StatusMonitor.EndWait();
                    //Notification.ShowInformation("REMINDER: skipping wait");
                }
                else {
                    try {
                        IPlanTarget planTarget = plan.PlanTarget;
                        Logger.Info($"Scheduler: starting execution of plan target: {planTarget.Name}");
                        SetTarget(atTime, planTarget);
                        StatusMonitor.BeginTarget(planTarget);

                        // Create a container for this target, add the instructions, and execute
                        AssistantTargetContainer targetContainer = GetTargetContainer(previousPlanTarget, plan, StatusMonitor);
                        targetContainer.Execute(progress, token).Wait();
                        previousPlanTarget = planTarget;
                    }
                    catch (Exception ex) {
                        if (ex is SequenceEntityFailedException) {
                            throw ex;
                        }

                        Logger.Error($"Scheduler: exception executing plan: {ex}");
                        throw new SequenceEntityFailedException($"Scheduler: exception executing plan: {ex.Message}", ex);
                    }
                    finally {
                        ClearTarget();
                        StatusMonitor.EndTarget();
                    }
                }
            }
        }

        private void WaitForNextTarget(DateTime? waitForNextTargetTime, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info($"Scheduler: stopping guiding/tracking, then waiting for next target to be available at {Utils.FormatDateTimeFull(waitForNextTargetTime)}");
            SequenceCommands.StopGuiding(guiderMediator, token);
            SequenceCommands.SetTelescopeTracking(telescopeMediator, TrackingMode.Stopped, token);

            TimeSpan duration = ((DateTime)waitForNextTargetTime) - DateTime.Now;
            CoreUtil.Wait(duration, token, progress).Wait(token);
            Logger.Debug("Scheduler: done waiting for next target");
        }

        private AssistantTargetContainer GetTargetContainer(IPlanTarget previousPlanTarget, AssistantPlan plan, AssistantStatusMonitor monitor) {
            AssistantTargetContainer targetContainer = new AssistantTargetContainer(this, profileService, dateTimeProviders, telescopeMediator,
                rotatorMediator, guiderMediator, cameraMediator, imagingMediator, imageSaveMediator,
                imageHistoryVM, filterWheelMediator, domeMediator, domeFollower,
                plateSolverFactory, windowServiceFactory, previousPlanTarget, plan, monitor);
            return targetContainer;
        }

        private AssistantStatusMonitor statusMonitor;
        public AssistantStatusMonitor StatusMonitor {
            get => statusMonitor;
            set {
                statusMonitor = value;
                RaisePropertyChanged(nameof(StatusMonitor));
            }
        }

        public AsyncObservableCollection<IStatusItem> StatusItemList {
            get => StatusMonitor?.StatusItemList;
        }

        public string Summary {
            get { return StatusMonitor.Summary; }
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public bool Validate() {
            var i = new ObservableCollection<string>();

            // TODO: see RoboCopyStart for howto
            // - could fire the Assistant and if nothing comes back -> 'nothing to image'
            // - could ensure connections: telescope, rotator (if), camera, filter wheel (if), guider (if dither), etc

            Issues = i;
            return i.Count == 0;
        }

        private void SetTarget(DateTime atTime, IPlanTarget planTarget) {
            IProfile activeProfile = profileService.ActiveProfile;
            DateTime referenceDate = NighttimeCalculator.GetReferenceDate(atTime);
            CustomHorizon customHorizon = GetCustomHorizon(activeProfile, planTarget.Project);

            InputTarget inputTarget = new InputTarget(
                Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                customHorizon);

            inputTarget.DeepSkyObject = GetDeepSkyObject(referenceDate, activeProfile, planTarget, customHorizon);
            inputTarget.TargetName = planTarget.Name;
            inputTarget.InputCoordinates = new InputCoordinates(planTarget.Coordinates);
            inputTarget.Rotation = planTarget.Rotation;
            Target = inputTarget;

            ProjectTargetDisplay = $"{planTarget.Project.Name} / {planTarget.Name}";
            CoordinatesDisplay = $"{inputTarget.InputCoordinates.RAHours}h  {inputTarget.InputCoordinates.RAMinutes}m  {inputTarget.InputCoordinates.RASeconds}s   " +
                                $"{inputTarget.InputCoordinates.DecDegrees}°  {inputTarget.InputCoordinates.DecMinutes}'  {inputTarget.InputCoordinates.DecSeconds}\",   " +
                                $"Rotation {planTarget.Rotation}°";
            StopAtDisplay = $"{planTarget.EndTime:HH:mm:ss}";

            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate(referenceDate)).Wait();

            RaisePropertyChanged(nameof(ProjectTargetDisplay));
            RaisePropertyChanged(nameof(CoordinatesDisplay));
            RaisePropertyChanged(nameof(StopAtDisplay));
            RaisePropertyChanged(nameof(NighttimeData));
            RaisePropertyChanged(nameof(Target));
        }

        private void ClearTarget() {
            Target = null;
            ProjectTargetDisplay = "";
            CoordinatesDisplay = "";
            StopAtDisplay = "";

            RaisePropertyChanged(nameof(ProjectTargetDisplay));
            RaisePropertyChanged(nameof(CoordinatesDisplay));
            RaisePropertyChanged(nameof(StopAtDisplay));
            RaisePropertyChanged(nameof(NighttimeData));
            RaisePropertyChanged(nameof(Target));
        }

        private DeepSkyObject GetDeepSkyObject(DateTime referenceDate, IProfile activeProfile, IPlanTarget planTarget, CustomHorizon customHorizon) {
            DeepSkyObject dso = new DeepSkyObject(string.Empty, planTarget.Coordinates, null, customHorizon);
            dso.SetDateAndPosition(referenceDate, activeProfile.AstrometrySettings.Latitude, activeProfile.AstrometrySettings.Longitude);
            dso.Refresh();
            return dso;
        }

        private CustomHorizon GetCustomHorizon(IProfile activeProfile, IPlanProject project) {
            CustomHorizon customHorizon = project.UseCustomHorizon && activeProfile.AstrometrySettings.Horizon != null ?
                activeProfile.AstrometrySettings.Horizon :
                HorizonDefinition.GetConstantHorizon(project.MinimumAltitude);
            return customHorizon;
        }

        private void StatusMonitor_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(StatusMonitor));
            RaisePropertyChanged(nameof(StatusItemList));
        }

        public InputTarget Target { get; private set; }
        public NighttimeData NighttimeData { get; private set; }
        public string ProjectTargetDisplay { get; private set; }
        public string CoordinatesDisplay { get; private set; }
        public string StopAtDisplay { get; private set; }
    }
}
