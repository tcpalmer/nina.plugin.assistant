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

        private readonly PlanTarget planTarget;
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
                PlanTarget planTarget,
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
            this.planTarget = planTarget;
            SetTarget();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Trace("AssistantTargetContainer: execute");

            try {
                // Add a trigger to stop at target hard stop time
                AddEndTimeTrigger(planTarget);

                // Slew to target and center
                //SlewAndCenter(planTarget, progress, token);

                // Add the planned exposures
                AddExposures(planTarget);

                base.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                // TODO: nice to handle TaskCanceledException and OperationCanceledException separately
                // Really need to since I think we have to bubble this up - at least a TaskCanceledException
                // which is from hitting the stop button
                Logger.Error($"Assistant: exception\n{ex.ToString()}");
            }
            finally {
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
            Logger.Info($"Assistant: adding slew/center target instruction for {planTarget.Target.name}, id={planTarget.Id}");

            Center center = null;
            if (planTarget.Target.rotation == 0) {
                center = new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                center.Name = nameof(Center);
            }
            else {
                center = new CenterAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                center.Name = nameof(CenterAndRotate);
                (center as CenterAndRotate).Rotation = planTarget.Target.rotation;
            }

            center.Category = "Assistant";
            center.Description = "";
            center.ErrorBehavior = this.ErrorBehavior;
            center.Attempts = this.Attempts;
            center.Coordinates = new InputCoordinates(planTarget.Target.GetCoordinates());
            Add(new WrappedInstruction(monitor, planTarget.Id, center));
        }

        private void AddEndTimeTrigger(PlanTarget planTarget) {
            Logger.Info($"Assistant: adding target end time trigger, run until: {planTarget.TimeInterval.EndTime}");
            Add(new AssistantTargetEndTimeTrigger(planTarget.TimeInterval.EndTime));
        }

        private void AddExposures(PlanTarget planTarget) {
            foreach (PlanExposure planExposure in planTarget.PlanExposures) {
                AddSwitchFilter(planExposure);
                Logger.Info($"Assistant: adding exposures: count={planExposure.Exposures}, filter={planExposure.ExposurePlan.filtername}, exposure={planExposure.ExposurePlan.exposure}, id={planExposure.Id}");

                for (int i = 0; i < planExposure.Exposures; i++) {
                    TakeExposure takeExposure = new TakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM);
                    takeExposure.Name = nameof(TakeExposure);
                    takeExposure.Category = "Assistant";
                    takeExposure.Description = "";
                    takeExposure.ErrorBehavior = this.ErrorBehavior;
                    takeExposure.Attempts = this.Attempts;

                    //takeExposure.ExposureCount = planExposure.Exposures;
                    takeExposure.ExposureTime = planExposure.ExposurePlan.exposure;
                    takeExposure.Gain = planExposure.ExposurePlan.gain;
                    takeExposure.Offset = planExposure.ExposurePlan.offset;
                    takeExposure.Binning = GetBinning(planExposure);

                    Add(new WrappedInstruction(monitor, planExposure.Id, takeExposure));
                }
            }
        }

        private void AddSwitchFilter(PlanExposure planExposure) {
            Logger.Info($"Assistant: adding switch filter: {planExposure.ExposurePlan.filtername}");

            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            switchFilter.Name = nameof(SwitchFilter);
            switchFilter.Category = "Assistant";
            switchFilter.Description = "";
            switchFilter.ErrorBehavior = this.ErrorBehavior;
            switchFilter.Attempts = this.Attempts;

            switchFilter.Filter = LookupFilter(planExposure.ExposurePlan.filtername);
            Add(new WrappedInstruction(monitor, planExposure.Id, switchFilter));
        }

        private FilterInfo LookupFilter(string filtername) {
            foreach (FilterInfo filterInfo in profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters) {
                if (filterInfo.Name == filtername) {
                    return filterInfo;
                }
            }

            throw new SequenceEntityFailedException($"failed to find FilterInfo for filter: {filtername}");
        }

        private BinningMode GetBinning(PlanExposure planExposure) {
            short bin = (short)planExposure.ExposurePlan.bin;
            return new BinningMode(bin, bin);
        }

        private void SetTarget() {
            _target = new InputTarget(
            Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude),
                Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude),
                profileService.ActiveProfile.AstrometrySettings.Horizon);

            _target.TargetName = planTarget.Target.name;
            _target.InputCoordinates = new InputCoordinates(planTarget.Target.GetCoordinates());
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

        private PlanTarget planTarget;

        public AssistantStatusMonitor(PlanTarget planTarget) {
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
