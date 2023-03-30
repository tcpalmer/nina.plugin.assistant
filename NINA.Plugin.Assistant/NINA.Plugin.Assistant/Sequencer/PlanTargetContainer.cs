using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Enum;
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
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.Guider;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class PlanTargetContainer : SequenceContainer, IDeepSkyObjectContainer {

        public readonly static string INSTRUCTION_CATEGORY = "Scheduler";

        private readonly TargetSchedulerContainer parentContainer;
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
        private readonly SchedulerPlan plan;
        private readonly IProfile activeProfile;
        private SchedulerStatusMonitor monitor;

        private IImageSaveWatcher ImageSaveWatcher;

        public PlanTargetContainer(
                TargetSchedulerContainer parentContainer,
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
                SchedulerPlan plan,
                SchedulerStatusMonitor monitor) : base(new PlanTargetContainerStrategy()) {
            Name = nameof(PlanTargetContainer);
            Description = "";
            Category = "Assistant";

            this.parentContainer = parentContainer;
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

            PlanTargetContainerStrategy containerStrategy = Strategy as PlanTargetContainerStrategy;
            containerStrategy.SetContext(parentContainer, plan, monitor);

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
                AddParentTriggers();
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
            Logger.Warning("PlanTargetContainer: interrupt");
            return base.Interrupt();
        }

        private void AddEndTimeTrigger(IPlanTarget planTarget) {
            Logger.Info($"Scheduler: adding target end time trigger, run until: {Utils.FormatDateTimeFull(planTarget.EndTime)}");
            Add(new SchedulerTargetEndTimeTrigger(planTarget.EndTime));
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

        private void AddParentTriggers() {
            if (parentContainer.Triggers.Count == 0) {
                return;
            }

            // Clone the parent's triggers to this container so they can operate 'normally'
            IList<ISequenceTrigger> localTriggers;
            lock (parentContainer.lockObj) {
                localTriggers = parentContainer.Triggers.ToArray();
            }

            foreach (var trigger in localTriggers) {
                if (trigger.Status == SequenceEntityStatus.DISABLED) { continue; }
                Add((ISequenceTrigger)trigger.Clone());
            }
        }

        private void AddInstructions(SchedulerPlan plan) {

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

                if (instruction is Plan.PlanTakeExposure) {
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

            //isPlateSolve = false;
            //Notification.ShowInformation("REMINDER: center is disabled for slews");

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
            Add(slewCenter);
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
            Add(switchFilter);
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

            Add(setReadoutMode);
        }

        private void AddTakeExposure(IPlanExposure planExposure) {
            Logger.Info($"Scheduler: adding take exposure: {planExposure.FilterName}");

            PlanTakeExposure takeExposure = new PlanTakeExposure(parentContainer,
                        profileService,
                        cameraMediator,
                        imagingMediator,
                        imageSaveMediator,
                        imageHistoryVM,
                        ImageSaveWatcher,
                        planExposure.DatabaseId);

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

            Add(takeExposure);
        }

        private void AddWait(DateTime waitForTime, IPlanTarget planTarget) {
            Add(new PlanWaitInstruction(guiderMediator, telescopeMediator, waitForTime));
        }

        private int GetExposureCount() {
            parentContainer.TotalExposureCount++;
            return parentContainer.TotalExposureCount;
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

        public override object Clone() {
            throw new NotImplementedException();
        }

        // IDeepSkyObjectContainer behavior, defer to parent
        public InputTarget Target { get => parentContainer.Target; set { } }
        public NighttimeData NighttimeData => parentContainer.NighttimeData;
    }
}
