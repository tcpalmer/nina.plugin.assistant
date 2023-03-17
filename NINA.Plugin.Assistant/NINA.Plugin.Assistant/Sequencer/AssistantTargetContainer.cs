using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
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

        public readonly static string INSTRUCTION_CATEGORY = "Scheduler";

        private readonly AssistantInstruction parentInstruction;
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

        private IImageSaveWatcher ImageSaveWatcher;

        public AssistantTargetContainer(
                AssistantInstruction parentInstruction,
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

            this.parentInstruction = parentInstruction;
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

            Attempts = 1;
            ErrorBehavior = InstructionErrorBehavior.SkipInstructionSetOnError;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("Scheduler: executing target container");

            try {
                if (!plan.IsEmulator)
                    ImageSaveWatcher = new ImageSaveWatcher(imageSaveMediator, plan.PlanTarget);
                else
                    ImageSaveWatcher = new ImageSaveWatcherEmulator();

                AddEndTimeTrigger(plan.PlanTarget);
                AddDitherTrigger(plan.PlanTarget);
                AddInstructions(plan);

                ImageSaveWatcher.Start();
                base.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                ImageSaveWatcher.Stop();

                foreach (var item in Items) {
                    item.AttachNewParent(null);
                }

                foreach (var condition in Triggers) {
                    condition.AttachNewParent(null);
                }

                Items.Clear();
                Triggers.Clear();
                Logger.Debug("Scheduler: done executing target container");
            }

            return Task.CompletedTask;
        }

        public override Task Interrupt() {
            Logger.Warning("AssistantTargetContainer: interrupt");
            return base.Interrupt();
        }

        private void AddEndTimeTrigger(IPlanTarget planTarget) {
            Logger.Info($"Scheduler: adding target end time trigger, run until: {Utils.FormatDateTimeFull(planTarget.EndTime)}");
            Add(new AssistantTargetEndTimeTrigger(planTarget.EndTime));
        }

        private void AddDitherTrigger(IPlanTarget planTarget) {
            int ditherEvery = planTarget.Project.DitherEvery;
            if (ditherEvery > 0) {
                Logger.Info($"Scheduler: adding dither trigger: every {ditherEvery} exposures");
                DitherAfterExposures ditherTrigger = new DitherAfterExposures(guiderMediator, imageHistoryVM, profileService);
                ditherTrigger.AfterExposures = ditherEvery;
                Add(ditherTrigger);
            }
        }

        private void AddInstructions(AssistantPlan plan) {

            /* TODO: since our determination of target visibility over the course of a night will not detect the
             * 'horizon tree gap' problem, we need to mitigate.
             * - If that was the only potential target, it would still get picked so the sequence container may also have to detect and
             *   skip exposures, maybe a custom trigger that looks for horizon dynamically and takes action?
             */

            foreach (IPlanInstruction instruction in plan.PlanInstructions) {

                if (instruction is PlanMessage) {
                    Logger.Debug($"exp plan msg: {((PlanMessage)instruction).msg}");
                    continue;
                }

                if (instruction is PlanSlew) {
                    AddSlew((PlanSlew)instruction, plan.PlanTarget);
                    //Notification.ShowInformation("REMINDER: SKIPPING SLEW");
                    continue;
                }

                if (instruction is PlanSwitchFilter) {
                    AddSwitchFilter(instruction.planExposure);
                    continue;
                }

                if (instruction is PlanSetReadoutMode) {
                    AddSetReadoutMode(instruction.planExposure);
                    continue;
                }

                if (instruction is PlanTakeExposure) {
                    AddTakeExposure(instruction.planExposure);
                    continue;
                }

                if (instruction is PlanWait) {
                    AddWait(((PlanWait)instruction).waitForTime, plan.PlanTarget);
                    continue;
                }

                throw new Exception($"unknown instruction type: {instruction.GetType().FullName}");
            }
        }

        private void AddSlew(PlanSlew instruction, IPlanTarget planTarget) {

            bool isPlateSolve = instruction.center || planTarget.Rotation != 0;
            InputCoordinates slewCoordinates = new InputCoordinates(planTarget.Coordinates);
            SequenceItem slewCenter;

            isPlateSolve = false;
            Notification.ShowInformation("REMINDER: center is disabled for slews");

            string with = isPlateSolve ? "with" : "without";
            Logger.Info($"Scheduler: slew ({with} center): {Utils.FormatCoordinates(planTarget.Coordinates)}");

            if (isPlateSolve) {
                if (planTarget.Rotation == 0) {
                    slewCenter = new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                    slewCenter.Name = nameof(Center);
                    (slewCenter as Center).Coordinates = slewCoordinates;
                }
                else {
                    slewCenter = new CenterAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                    slewCenter.Name = nameof(CenterAndRotate);
                    (slewCenter as Center).Coordinates = slewCoordinates;
                    (slewCenter as CenterAndRotate).Rotation = planTarget.Rotation;
                }
            }
            else {
                slewCenter = new SlewScopeToRaDec(telescopeMediator, guiderMediator);
                slewCenter.Name = nameof(SlewScopeToRaDec);
                (slewCenter as SlewScopeToRaDec).Coordinates = slewCoordinates;
            }

            slewCenter.Category = INSTRUCTION_CATEGORY;
            slewCenter.Description = "";
            slewCenter.ErrorBehavior = this.ErrorBehavior;
            slewCenter.Attempts = this.Attempts;
            Add(new InstructionWrapper(monitor, planTarget.PlanId, slewCenter));
        }

        private void AddSwitchFilter(IPlanExposure planExposure) {
            Logger.Info($"Scheduler: adding switch filter: {planExposure.FilterName}");

            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            switchFilter.Name = nameof(SwitchFilter);
            switchFilter.Category = INSTRUCTION_CATEGORY;
            switchFilter.Description = "";
            switchFilter.ErrorBehavior = this.ErrorBehavior;
            switchFilter.Attempts = this.Attempts;

            switchFilter.Filter = LookupFilter(planExposure.FilterName);
            Add(new InstructionWrapper(monitor, planExposure.PlanId, switchFilter));
        }

        private void AddSetReadoutMode(IPlanExposure planExposure) {
            int? readoutMode = planExposure.ReadoutMode;
            readoutMode = (readoutMode == null || readoutMode < 0) ? 0 : readoutMode;

            Logger.Info($"Scheduler: adding set readout mode: {readoutMode}");
            SetReadoutMode setReadoutMode = new SetReadoutMode(cameraMediator);
            setReadoutMode.Name = nameof(SetReadoutMode);
            setReadoutMode.Category = INSTRUCTION_CATEGORY;
            setReadoutMode.Description = "";
            setReadoutMode.ErrorBehavior = this.ErrorBehavior;
            setReadoutMode.Attempts = this.Attempts;

            setReadoutMode.Mode = (short)readoutMode;

            Add(new InstructionWrapper(monitor, planExposure.PlanId, setReadoutMode));
        }

        private void AddTakeExposure(IPlanExposure planExposure) {
            Logger.Info($"Scheduler: adding take exposure: {planExposure.FilterName}");

            TakeExposure takeExposure = new AssistantTakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, ImageSaveWatcher, planExposure.DatabaseId);
            takeExposure.Name = nameof(TakeExposure);
            takeExposure.Category = INSTRUCTION_CATEGORY;
            takeExposure.Description = "";
            takeExposure.ErrorBehavior = this.ErrorBehavior;
            takeExposure.Attempts = this.Attempts;
            takeExposure.ExposureCount = GetExposureCount();

            takeExposure.ExposureTime = planExposure.ExposureLength;
            takeExposure.Gain = GetGain(planExposure.Gain);
            takeExposure.Offset = GetOffset(planExposure.Offset);
            takeExposure.Binning = planExposure.BinningMode;

            Add(new InstructionWrapper(monitor, planExposure.PlanId, takeExposure));
        }

        private void AddWait(DateTime waitForTime, IPlanTarget planTarget) {
            Add(new InstructionWrapper(monitor, planTarget.PlanId, new AssistantWaitInstruction(guiderMediator, telescopeMediator, waitForTime)));
        }

        private int GetExposureCount() {
            parentInstruction.TotalExposureCount++;
            return parentInstruction.TotalExposureCount;
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

        // IDeepSkyObjectContainer behavior, defer to parent
        public InputTarget Target { get => parentInstruction.Target; set { } }
        public NighttimeData NighttimeData => parentInstruction.NighttimeData;
    }

    public class InstructionWrapper : SequenceItem {

        private AssistantStatusMonitor Monitor;
        private string PlanItemId;
        private SequenceItem Instruction;

        public InstructionWrapper(AssistantStatusMonitor monitor, string planItemId, SequenceItem instruction) {
            this.Monitor = monitor;
            this.PlanItemId = planItemId;
            this.Instruction = instruction;

            this.Name = $"{instruction.Name}";
            this.Category = AssistantTargetContainer.INSTRUCTION_CATEGORY;
            this.Description = "Wrapper";
            this.ErrorBehavior = instruction.ErrorBehavior;
            this.Attempts = instruction.Attempts;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            try {
                Monitor.ItemStart(PlanItemId, Name);
                Instruction.Execute(progress, token).Wait();
                Monitor.ItemFinish(PlanItemId, Name);
            }
            catch (Exception ex) {
                throw ex;
            }

            return Task.CompletedTask;
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(InstructionWrapper)}";
        }

        public override object Clone() {
            throw new NotImplementedException();
        }
    }

}
