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
using NINA.Sequencer.Trigger.Guider;
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
        private readonly IPlanTarget planTarget;
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
                IPlanTarget planTarget,
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
            this.planTarget = planTarget;
            this.activeProfile = profileService.ActiveProfile;
            SetTarget();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Trace("AssistantTargetContainer: execute");
            ImageSaveWatcher imageSaveWatcher = null;

            try {
                AddEndTimeTrigger(planTarget);
                AddDitherTrigger(planTarget);

                // If target is different from previous, slew/center/rotate
                if (!planTarget.Equals(previousPlanTarget)) {
                    // TODO: pain to simulate
                    //SlewAndCenter(planTarget, progress, token);
                }

                // Add the planned exposures
                AddExposures(planTarget);

                imageSaveWatcher = new ImageSaveWatcher(imageSaveMediator, planTarget);
                base.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                // TODO: nice to handle TaskCanceledException and OperationCanceledException separately
                // Really need to since I think we have to bubble this up - at least a TaskCanceledException
                // which is from hitting the stop button
                Logger.Error($"Assistant: exception\n{ex.ToString()}");
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
            }

            return Task.CompletedTask;
        }

        public override Task Interrupt() {
            Logger.Warning("AssistantTargetContainer: interrupt");
            return base.Interrupt();
        }

        private void AddSlewAndCenter(PlanTarget planTarget, IProgress<ApplicationStatus> progress, CancellationToken token) {
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

        private void AddExposures(IPlanTarget planTarget) {

            /* TODO: since our determination of target visibility over the course of a night will not detect the
             * 'horizon tree gap' problem, we need to mitigate.
             * - If that was the only potential target, it would still get picked so the sequence container may also have to detect and
             *   skip exposures, maybe sleeping and rechecking for some period until it appears to be visible.
             */

            foreach (PlanFilter planFilter in planTarget.FilterPlans) {
                if (planFilter.Rejected) { continue; }

                AddSwitchFilter(planFilter);
                Logger.Info($"Assistant: adding exposures: count={planFilter.PlannedExposures}, filter={planFilter.FilterName}, exposure={planFilter.ExposureLength}, id={planFilter.PlanId}");

                for (int i = 0; i < planFilter.PlannedExposures; i++) {
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
            }
        }

        private void AddSwitchFilter(PlanFilter planFilter) {
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

        private FilterInfo LookupFilter(string filterName) {
            foreach (FilterInfo filterInfo in activeProfile.FilterWheelSettings.FilterWheelFilters) {
                if (filterInfo.Name == filterName) {
                    return filterInfo;
                }
            }

            throw new SequenceEntityFailedException($"failed to find FilterInfo for filter: {filterName}");
        }

        private int GetGain(int? gain) {
            // TODO: if null, pull from camera/filter default
            return (int)(gain == null ? 0 : gain);
        }

        private int GetOffset(int? offset) {
            // TODO: if null, pull from camera/filter default
            return (int)((int)(offset == null ? 0 : offset));
        }

        private void SetTarget() {
            _target = new InputTarget(
            Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                activeProfile.AstrometrySettings.Horizon);

            _target.TargetName = planTarget.Name;
            _target.InputCoordinates = new InputCoordinates(planTarget.Coordinates);
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
