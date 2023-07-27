using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// Cribbed from NINA.Sequencer.Container.DeepSkyObjectContainer
    /// </summary>
    [ExportMetadata("Name", "Target Scheduler Container")]
    [ExportMetadata("Description", "Container for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerContainer : SequentialContainer, IDeepSkyObjectContainer {

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
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IApplicationMediator applicationMediator;

        [JsonProperty]
        public InstructionContainer BeforeWaitContainer { get; set; }

        [JsonProperty]
        public InstructionContainer AfterWaitContainer { get; set; }

        [JsonProperty]
        public InstructionContainer BeforeTargetContainer { get; set; }

        [JsonProperty]
        public InstructionContainer AfterTargetContainer { get; set; }

        private ProfilePreference profilePreferences;

        public object lockObj = new object();
        public int TotalExposureCount { get; set; }

        [ImportingConstructor]
        public TargetSchedulerContainer(
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
                IWindowServiceFactory windowServiceFactory,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator) : base() {

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
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;

            BeforeWaitContainer = new InstructionContainer("BeforeWait", Parent);
            AfterWaitContainer = new InstructionContainer("AfterWait", Parent);
            BeforeTargetContainer = new InstructionContainer("BeforeTarget", Parent);
            AfterTargetContainer = new InstructionContainer("AfterTarget", Parent);

            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate());
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);

            TotalExposureCount = -1;
            ClearTarget();

            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.LocationChanged), ProfileService_LocationChanged);
            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.HorizonChanged), ProfileService_HorizonChanged);
            WeakEventManager<INighttimeCalculator, EventArgs>.AddHandler(nighttimeCalculator, nameof(nighttimeCalculator.OnReferenceDayChanged), NighttimeCalculator_OnReferenceDayChanged);
        }

        public override void Initialize() {
            TSLogger.Debug("Scheduler instruction: Initialize");

            if (SchedulerProgress != null) {
                SchedulerProgress.Reset();
                SchedulerProgress.PropertyChanged -= SchedulerProgress_PropertyChanged;
            }

            SchedulerProgress = new SchedulerProgressVM();
            SchedulerProgress.PropertyChanged += SchedulerProgress_PropertyChanged;
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            if (Parent == null) {
                SequenceBlockTeardown();
            }
            else {
                BeforeWaitContainer.AttachNewParent(Parent);
                AfterWaitContainer.AttachNewParent(Parent);
                BeforeTargetContainer.AttachNewParent(Parent);
                AfterTargetContainer.AttachNewParent(Parent);

                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override void ResetProgress() {
            TSLogger.Debug("Scheduler instruction: ResetProgress");

            BeforeWaitContainer.ResetProgress();
            AfterWaitContainer.ResetProgress();
            BeforeTargetContainer.ResetProgress();
            AfterTargetContainer.ResetProgress();

            if (SchedulerProgress != null) {
                SchedulerProgress.Reset();
            }

            ClearTarget();

            base.ResetProgress();
        }

        public override void Teardown() {
            TSLogger.Debug("Scheduler instruction: Teardown");
            base.Teardown();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Debug("Scheduler instruction: Execute");

            IPlanTarget previousPlanTarget = null;

            while (true) {
                DateTime atTime = DateTime.Now;
                profilePreferences = GetProfilePreferences();
                SchedulerPlan plan = new Planner(atTime, profileService, profilePreferences, false).GetPlan(previousPlanTarget);

                if (plan == null) {
                    if (previousPlanTarget != null) {
                        ExecuteEventContainer(AfterTargetContainer, progress, token);
                    }

                    SchedulerProgress.End();

                    TSLogger.Info("planner returned empty plan, done");
                    return Task.CompletedTask;
                }

                if (plan.WaitForNextTargetTime != null) {
                    if (previousPlanTarget != null) {
                        ExecuteEventContainer(AfterTargetContainer, progress, token);
                        previousPlanTarget = null;
                    }

                    TSLogger.Info($"planner waiting for next target to become available: {Utils.FormatDateTimeFull(plan.WaitForNextTargetTime)}");

                    SchedulerProgress.WaitStart(plan.WaitForNextTargetTime);
                    ExecuteEventContainer(BeforeWaitContainer, progress, token);
                    SchedulerProgress.Add("Wait");

                    WaitForNextTarget(plan.WaitForNextTargetTime, progress, token);

                    ExecuteEventContainer(AfterWaitContainer, progress, token);
                    SchedulerProgress.End();
                }
                else {
                    try {
                        IPlanTarget planTarget = plan.PlanTarget;

                        if (previousPlanTarget != null && !planTarget.Equals(previousPlanTarget)) {
                            ExecuteEventContainer(AfterTargetContainer, progress, token);
                        }

                        SchedulerProgress.End();

                        TSLogger.Info("--BEGIN PLAN EXECUTION--------------------------------------------------------");
                        TSLogger.Info($"plan target: {planTarget.Name}");
                        SetTarget(atTime, planTarget);
                        SchedulerProgress.TargetStart(planTarget.Project.Name, planTarget.Name);

                        // Create a container for this target, add the instructions, and execute
                        PlanTargetContainer targetContainer = GetPlanTargetContainer(previousPlanTarget, plan, SchedulerProgress);
                        targetContainer.Execute(progress, token).Wait();
                        previousPlanTarget = planTarget;
                    }
                    catch (Exception ex) {
                        if (ex.InnerException?.Message == "A task was canceled.") {
                            TSLogger.Warning("sequence was canceled");
                        }
                        else {
                            TSLogger.Error($"exception executing plan: {ex}");
                        }

                        if (ex is SequenceEntityFailedException) {
                            throw;
                        }

                        throw new SequenceEntityFailedException($"exception executing plan: {ex.Message}", ex);
                    }
                    finally {
                        ClearTarget();
                        TSLogger.Info("-- END PLAN EXECUTION ----------------------------------------------------------");
                    }
                }
            }
        }

        public void ExecuteEventContainer(InstructionContainer container, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (container.Items?.Count > 0) {
                // TODO: for AfterTarget, we need the name of the container for the previous target
                SchedulerProgress.Add(container.Name);

                try {
                    container.Execute(progress, token).Wait();
                }
                catch (Exception ex) {
                    SchedulerProgress.End();
                    TSLogger.Error($"exception executing {container.Name} instruction container: {ex}");

                    if (ex is SequenceEntityFailedException) {
                        throw;
                    }

                    throw new SequenceEntityFailedException($"exception executing {container.Name} instruction container: {ex.Message}", ex);
                }
            }
        }

        private ProfilePreference GetProfilePreferences() {
            SchedulerPlanLoader loader = new SchedulerPlanLoader(profileService.ActiveProfile);
            return loader.GetProfilePreferences(new SchedulerDatabaseInteraction().GetContext());
        }

        private void WaitForNextTarget(DateTime? waitForNextTargetTime, IProgress<ApplicationStatus> progress, CancellationToken token) {

            TimeSpan duration = ((DateTime)waitForNextTargetTime) - DateTime.Now;
            bool parked = false;

            if (profilePreferences.ParkOnWait && duration.TotalSeconds > 60) {
                TSLogger.Info($"stopping guiding/tracking, parking mount, then waiting for next target to be available at {Utils.FormatDateTimeFull(waitForNextTargetTime)}");
                SequenceCommands.SetTelescopeTracking(telescopeMediator, TrackingMode.Stopped, token);
                _ = SequenceCommands.ParkTelescope(telescopeMediator, guiderMediator, progress, token);
                parked = true;
            }
            else {
                TSLogger.Info($"stopping guiding/tracking, then waiting for next target to be available at {Utils.FormatDateTimeFull(waitForNextTargetTime)}");
                SequenceCommands.StopGuiding(guiderMediator, token);
                SequenceCommands.SetTelescopeTracking(telescopeMediator, TrackingMode.Stopped, token);
            }

            CoreUtil.Wait(duration, token, progress).Wait(token);
            TSLogger.Info("done waiting for next target");

            if (parked) {
                SequenceCommands.UnparkTelescope(telescopeMediator, progress, token).Wait();
            }
        }

        private PlanTargetContainer GetPlanTargetContainer(IPlanTarget previousPlanTarget, SchedulerPlan plan, SchedulerProgressVM schedulerProgress) {
            PlanTargetContainer targetContainer = new PlanTargetContainer(this, profileService, dateTimeProviders, telescopeMediator,
            rotatorMediator, guiderMediator, cameraMediator, imagingMediator, imageSaveMediator,
            imageHistoryVM, filterWheelMediator, domeMediator, domeFollower,
                plateSolverFactory, windowServiceFactory, previousPlanTarget, plan, schedulerProgress);
            return targetContainer;
        }

        private SchedulerProgressVM schedulerProgress;
        public SchedulerProgressVM SchedulerProgress {
            get => schedulerProgress;
            set {
                schedulerProgress = value;
                RaisePropertyChanged(nameof(SchedulerProgress));
                RaisePropertyChanged(nameof(ProgressItemsView));
            }
        }

        public ICollectionView ProgressItemsView {
            get => SchedulerProgress?.ItemsView;
        }

        public override bool Validate() {
            var issues = new List<string>();

            if (Conditions.Count > 0) {
                TSLogger.Error("Huh?  Somehow a condition was added to TSC ... ?  Will be ignored but ...");
            }

            if (Items.Count > 0) {
                TSLogger.Error("Huh?  Somehow an instruction was added to TSC ... ?  Will be ignored but ...");
            }

            Issues = issues;
            return issues.Count == 0;
        }

        public void ClearTarget() {
            lock (lockObj) {
                Target = GetEmptyTarget();
                ProjectTargetDisplay = "";
                CoordinatesDisplay = "";
                StopAtDisplay = "";

                RaisePropertyChanged(nameof(ProjectTargetDisplay));
                RaisePropertyChanged(nameof(CoordinatesDisplay));
                RaisePropertyChanged(nameof(StopAtDisplay));
                RaisePropertyChanged(nameof(NighttimeData));
                RaisePropertyChanged(nameof(Target));
            }
        }

        public void SetTarget(DateTime atTime, IPlanTarget planTarget) {
            lock (lockObj) {
                IProfile activeProfile = profileService.ActiveProfile;
                DateTime referenceDate = NighttimeCalculator.GetReferenceDate(atTime);
                CustomHorizon customHorizon = GetCustomHorizon(activeProfile, planTarget.Project);

                InputTarget inputTarget = new InputTarget(
                    Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                    customHorizon);

                Coordinates coords = new Coordinates(Angle.ByDegree(15), Angle.ByDegree(5), Epoch.J2000);

                inputTarget.DeepSkyObject = GetDeepSkyObject(referenceDate, activeProfile, planTarget, customHorizon);
                inputTarget.TargetName = planTarget.Name;
                inputTarget.InputCoordinates = new InputCoordinates(planTarget.Coordinates);
                inputTarget.PositionAngle = planTarget.Rotation;
                inputTarget.Expanded = true;
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
        }

        private InputTarget GetEmptyTarget() {
            IProfile activeProfile = profileService.ActiveProfile;
            InputTarget inputTarget = new InputTarget(
                Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                activeProfile.AstrometrySettings.Horizon);
            inputTarget.TargetName = string.Empty;
            inputTarget.InputCoordinates.Coordinates = new Coordinates(Angle.Zero, Angle.Zero, Epoch.J2000);
            inputTarget.PositionAngle = 0;
            return inputTarget;
        }

        private void SchedulerProgress_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(nameof(SchedulerProgress));
            RaisePropertyChanged(nameof(ProgressItemsView));
        }

        public NighttimeData NighttimeData { get; private set; }

        private InputTarget target;
        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.RemoveHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                target = value;
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.AddHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                RaisePropertyChanged();
            }
        }

        public string ProjectTargetDisplay { get; private set; }
        public string CoordinatesDisplay { get; private set; }
        public string StopAtDisplay { get; private set; }

        private void Target_OnCoordinatesChanged(object sender, EventArgs e) {
            AfterParentChanged();
        }

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        private void ProfileService_HorizonChanged(object sender, EventArgs e) {
            Target?.DeepSkyObject?.SetCustomHorizon(profileService.ActiveProfile.AstrometrySettings.Horizon);
        }

        private void ProfileService_LocationChanged(object sender, EventArgs e) {
            Target?.SetPosition(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
        }

        private CustomHorizon GetCustomHorizon(IProfile activeProfile, IPlanProject project) {

            // For display in the Nighttime altitude chart, we either use the profile's custom horizon as-is or generate
            // a fixed constant horizon using the project's minimum altitude.  If using the regular custom horizon, we
            // won't show any modifications due to horizon offset or a base minimum altitude.  Not ideal but core
            // CustomHorizon is rather locked up so doesn't make it easy to get the interal alt/az values to regen it.

            CustomHorizon customHorizon = project.UseCustomHorizon && activeProfile.AstrometrySettings.Horizon != null ?
                activeProfile.AstrometrySettings.Horizon :
                HorizonDefinition.GetConstantHorizon(project.MinimumAltitude);
            return customHorizon;
        }

        private DeepSkyObject GetDeepSkyObject(DateTime referenceDate, IProfile activeProfile, IPlanTarget planTarget, CustomHorizon customHorizon) {
            DeepSkyObject dso = new DeepSkyObject(string.Empty, planTarget.Coordinates, null, customHorizon);
            dso.Name = planTarget.Name;
            dso.SetDateAndPosition(referenceDate, activeProfile.AstrometrySettings.Latitude, activeProfile.AstrometrySettings.Longitude);
            dso.Refresh();
            return dso;
        }

        public TargetSchedulerContainer(TargetSchedulerContainer cloneMe) : this(
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
                cloneMe.windowServiceFactory,
                cloneMe.framingAssistantVM,
                cloneMe.applicationMediator
            ) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            var clone = new TargetSchedulerContainer(
                profileService,
                dateTimeProviders,
                telescopeMediator,
                rotatorMediator,
                guiderMediator,
                cameraMediator,
                imagingMediator,
                imageSaveMediator,
                imageHistoryVM,
                filterWheelMediator,
                domeMediator,
                domeFollower,
                plateSolverFactory,
                nighttimeCalculator,
                windowServiceFactory,
                framingAssistantVM,
                applicationMediator) {

                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
            };

            clone.BeforeWaitContainer = (InstructionContainer)BeforeWaitContainer.Clone();
            clone.AfterWaitContainer = (InstructionContainer)AfterWaitContainer.Clone();
            clone.BeforeTargetContainer = (InstructionContainer)BeforeTargetContainer.Clone();
            clone.AfterTargetContainer = (InstructionContainer)AfterTargetContainer.Clone();

            clone.BeforeWaitContainer.AttachNewParent(clone);
            clone.AfterWaitContainer.AttachNewParent(clone);
            clone.BeforeTargetContainer.AttachNewParent(clone);
            clone.AfterTargetContainer.AttachNewParent(clone);

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        public override string ToString() {
            var baseString = base.ToString();
            return $"{baseString}, Target: {Target?.TargetName} {Target?.InputCoordinates?.Coordinates} {Target?.PositionAngle}";
        }
    }
}
