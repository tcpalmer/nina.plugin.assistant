using Assistant.NINAPlugin.Plan;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class AssistantTargetContainer : SequentialContainer, IDeepSkyObjectContainer {

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
        private readonly IWindowServiceFactory windowServiceFactory;

        private readonly IPlanTarget previousPlanTarget;
        private readonly AssistantPlan plan;
        private readonly IProfile activeProfile;
        private AssistantStatusMonitor monitor;

        public AssistantTargetContainer(
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
                IWindowServiceFactory windowServiceFactory,
                IPlanTarget previousPlanTarget,
                AssistantPlan plan,
                AssistantStatusMonitor monitor) : base() {
            Name = nameof(AssistantTargetContainer);
            Description = "";
            Category = "Assistant";

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
            this.windowServiceFactory = windowServiceFactory;

            this.monitor = monitor;
            this.previousPlanTarget = previousPlanTarget;
            this.plan = plan;
            this.activeProfile = profileService.ActiveProfile;

            SetTarget();
            Attempts = 1;
            ErrorBehavior = InstructionErrorBehavior.SkipInstructionSetOnError;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("Assistant: executing target container");
            ImageSaveWatcher imageSaveWatcher = null;

            try {
                AddEndTimeTrigger(plan.PlanTarget);
                AddDitherTrigger(plan.PlanTarget);

                // If target is different from previous, slew/center/rotate
                if (!plan.PlanTarget.Equals(previousPlanTarget)) {
                    // TODO: pain to simulate
                    //AddSlewAndCenter(plan.PlanTarget, progress, token);
                    // TODO: just slew for garage testing
                    AddSlew(plan.PlanTarget, progress, token);
                }

                // Add the planned exposures
                AddExposures(plan);

                imageSaveWatcher = new ImageSaveWatcher(imageSaveMediator, plan.PlanTarget);
                base.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                imageSaveWatcher?.Stop();

                foreach (var item in Items) {
                    item.AttachNewParent(null);
                }

                foreach (var condition in Triggers) {
                    condition.AttachNewParent(null);
                }

                Items.Clear();
                Triggers.Clear();
                Logger.Debug("Assistant: done executing target container");
            }

            return Task.CompletedTask;
        }

        public override Task Interrupt() {
            Logger.Warning("AssistantTargetContainer: interrupt");
            return base.Interrupt();
        }

        private void AddSlewAndCenter(IPlanTarget planTarget, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info($"Assistant: adding slew/center target instruction for {planTarget.Name}, id={planTarget.PlanId}");

            Center center = null;
            if (planTarget.Rotation == 0) {
                center = new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                center.Name = nameof(Center);
            }
            else {
                center = new CenterAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                center.Name = nameof(CenterAndRotate);
                (center as CenterAndRotate).Rotation = planTarget.Rotation;
            }

            center.Category = "Assistant";
            center.Description = "";
            center.ErrorBehavior = this.ErrorBehavior;
            center.Attempts = this.Attempts;
            center.Coordinates = new InputCoordinates(planTarget.Coordinates);
            Add(new WrappedInstruction(monitor, planTarget.PlanId, center));
        }

        private void AddSlew(IPlanTarget planTarget, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info($"Assistant: adding slew target instruction for {planTarget.Name}, id={planTarget.PlanId}");

            // Really just used for testing when platesolving won't work

            SlewScopeToRaDec slew = null;
            slew = new SlewScopeToRaDec(telescopeMediator, guiderMediator);
            slew.Name = nameof(SlewScopeToRaDec);
            slew.Category = "Assistant";
            slew.Description = "";
            slew.ErrorBehavior = this.ErrorBehavior;
            slew.Attempts = this.Attempts;
            slew.Coordinates = new InputCoordinates(planTarget.Coordinates);
            Add(new WrappedInstruction(monitor, planTarget.PlanId, slew));
        }

        private void AddEndTimeTrigger(IPlanTarget planTarget) {
            Logger.Info($"Assistant: adding target end time trigger, run until: {planTarget.EndTime}");
            Add(new AssistantTargetEndTimeTrigger(planTarget.EndTime));
        }

        private void AddDitherTrigger(IPlanTarget planTarget) {
            int ditherEvery = planTarget.Project.Preferences.DitherEvery;
            if (ditherEvery > 0) {
                Logger.Info($"Assistant: adding dither trigger: every {ditherEvery} exposures");
                DitherAfterExposures ditherTrigger = new DitherAfterExposures(guiderMediator, imageHistoryVM, profileService);
                ditherTrigger.AfterExposures = ditherEvery;
                Add(ditherTrigger);
            }
        }

        private void AddExposures(AssistantPlan plan) {

            /* TODO: since our determination of target visibility over the course of a night will not detect the
             * 'horizon tree gap' problem, we need to mitigate.
             * - If that was the only potential target, it would still get picked so the sequence container may also have to detect and
             *   skip exposures, maybe sleeping and rechecking for some period until it appears to be visible.
             */

            foreach (IPlanInstruction instruction in plan.PlanInstructions) {

                if (instruction is PlanMessage) {
                    Logger.Debug($"exp plan msg: {((PlanMessage)instruction).msg}");
                    continue;
                }

                if (instruction is PlanSwitchFilter) {
                    AddSwitchFilter(instruction.planFilter);
                    continue;
                }

                if (instruction is PlanTakeExposure) {
                    AddTakeExposure(instruction.planFilter);
                    continue;
                }

                if (instruction is PlanWait) {
                    AddWait(((PlanWait)instruction).waitForTime, plan.PlanTarget);
                    continue;
                }

                throw new Exception($"unknown instruction type: {instruction.GetType().FullName}");
            }
        }

        private void AddSwitchFilter(IPlanFilter planFilter) {
            Logger.Info($"Assistant: adding switch filter: {planFilter.FilterName}");

            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            switchFilter.Name = nameof(SwitchFilter);
            switchFilter.Category = "Assistant";
            switchFilter.Description = "";
            switchFilter.ErrorBehavior = this.ErrorBehavior;
            switchFilter.Attempts = this.Attempts;

            switchFilter.Filter = LookupFilter(planFilter.FilterName);
            Add(new WrappedInstruction(monitor, planFilter.PlanId, switchFilter));
        }

        private void AddTakeExposure(IPlanFilter planFilter) {
            Logger.Info($"Assistant: adding take exposure: {planFilter.FilterName}");

            TakeExposure takeExposure = new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM);
            takeExposure.Name = nameof(TakeExposure);
            takeExposure.Category = "Assistant";
            takeExposure.Description = "";
            takeExposure.ErrorBehavior = this.ErrorBehavior;
            takeExposure.Attempts = this.Attempts;

            takeExposure.ExposureTime = planFilter.ExposureLength;
            takeExposure.Gain = GetGain(planFilter.Gain);
            takeExposure.Offset = GetOffset(planFilter.Offset);
            takeExposure.Binning = planFilter.BinningMode;

            Add(new WrappedInstruction(monitor, planFilter.PlanId, takeExposure));
        }

        private void AddWait(DateTime waitForTime, IPlanTarget planTarget) {
            Add(new WrappedInstruction(monitor, planTarget.PlanId, new AssistantWaitInstruction(guiderMediator, telescopeMediator, waitForTime)));
        }

        private FilterInfo LookupFilter(string filterName) {
            foreach (FilterInfo filterInfo in activeProfile.FilterWheelSettings.FilterWheelFilters) {
                if (filterInfo.Name == filterName) {
                    return filterInfo;
                }
            }

            throw new SequenceEntityFailedException($"failed to find FilterInfo for filter: {filterName}");
        }

        private int GetGain(int? gain) {
            return (int)(gain == null ? cameraMediator.GetInfo().DefaultGain : gain);
        }

        private int GetOffset(int? offset) {
            return (int)((int)(offset == null ? cameraMediator.GetInfo().DefaultOffset : offset));
        }

        private void SetTarget() {
            _target = new InputTarget(
            Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                activeProfile.AstrometrySettings.Horizon);

            _target.TargetName = plan.PlanTarget.Name;
            _target.InputCoordinates = new InputCoordinates(plan.PlanTarget.Coordinates);
            _target.Rotation = 0;
        }

        // IDeepSkyObjectContainer behavior
        private InputTarget _target;
        public InputTarget Target {
            get => _target;
            set {
                _target = value;
                RaisePropertyChanged();
            }
        }

        // IDeepSkyObjectContainer behavior
        public NighttimeData NighttimeData => null;

    }

    public class AssistantStatusMonitor {

        private IPlanTarget planTarget;

        public AssistantStatusMonitor(IPlanTarget planTarget) {
            this.planTarget = planTarget;
        }

        public void ItemStart(string itemId, string sequenceItemName) {

            //if (sequenceItemName == nameof(SwitchFilter)) {
            // then we know it's the filter switch associated with the planExposure with ID=itemId
            //}

            Logger.Debug($"WRAP item start: {itemId} {sequenceItemName}");
        }

        public void ItemFinsh(string itemId, string sequenceItemName) {
            Logger.Debug($"WRAP item finish: {itemId} {sequenceItemName}");
        }
    }

    public class WrappedInstruction : SequenceItem {

        private SequenceItem Instruction;
        private AssistantStatusMonitor Monitor;
        private string PlanItemId;

        public WrappedInstruction(AssistantStatusMonitor monitor, string planItemId, SequenceItem instruction) {
            this.Monitor = monitor;
            this.PlanItemId = planItemId;
            this.Instruction = instruction;

            this.Name = instruction.Name;
            this.Category = instruction.Category;
            this.Description = instruction.Description;
            this.ErrorBehavior = instruction.ErrorBehavior;
            this.Attempts = instruction.Attempts;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Monitor.ItemStart(PlanItemId, Name);
            await Instruction.Execute(progress, token);
            Monitor.ItemFinsh(PlanItemId, Name);
        }

        public override string ToString() {
            return Instruction.ToString();
        }

        public override object Clone() {
            return Instruction.Clone();
        }

    }

}
