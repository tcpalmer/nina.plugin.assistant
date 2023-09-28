using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.Sequencer.SequenceItem.Guider;
using NINA.Sequencer.SequenceItem.Imaging;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.Sequencer.SequenceItem.Telescope;
using NINA.Sequencer.Trigger;
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
        private SchedulerProgressVM schedulerProgress;

        private IImageSaveWatcher ImageSaveWatcher;

        private bool synchronizationEnabled;
        private int syncExposureTimeout;

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
                bool synchronizationEnabled,
                IPlanTarget previousPlanTarget,
                SchedulerPlan plan,
                SchedulerProgressVM schedulerProgress) : base(new PlanTargetContainerStrategy()) {
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

            this.synchronizationEnabled = synchronizationEnabled;
            this.schedulerProgress = schedulerProgress;
            this.previousPlanTarget = previousPlanTarget;
            this.plan = plan;
            this.activeProfile = profileService.ActiveProfile;

            PlanTargetContainerStrategy containerStrategy = Strategy as PlanTargetContainerStrategy;
            containerStrategy.SetContext(parentContainer, plan, schedulerProgress);
            AttachNewParent(parentContainer);

            if (synchronizationEnabled) {
                syncExposureTimeout = GetSyncExposureTimeout();
            }

            if (!plan.IsEmulator)
                ImageSaveWatcher = new ImageSaveWatcher(activeProfile, imageSaveMediator, plan.PlanTarget);
            else
                ImageSaveWatcher = new ImageSaveWatcherEmulator();

            // These have no impact on the container itself but are used to assign to each added instruction
            Attempts = 1;
            ErrorBehavior = InstructionErrorBehavior.ContinueOnError;
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Debug("executing target container");

            try {
                AddEndTimeTrigger(plan.PlanTarget);
                AddParentTriggers();
                AddInstructions(plan);
                EnsureUnparked(progress, token);

                ImageSaveWatcher.Start();
                base.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                throw;
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
                TSLogger.Debug("done executing target container");
            }

            return Task.CompletedTask;
        }

        public override Task Interrupt() {
            TSLogger.Warning("PlanTargetContainer: interrupt");
            return base.Interrupt();
        }

        private void AddEndTimeTrigger(IPlanTarget planTarget) {
            TSLogger.Info($"adding target end time trigger, run until: {Utils.FormatDateTimeFull(planTarget.EndTime)}");
            Add(new SchedulerTargetEndTimeTrigger(planTarget.EndTime));
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

            foreach (IPlanInstruction instruction in plan.PlanInstructions) {

                if (instruction is PlanMessage) {
                    TSLogger.Debug($"exp plan msg: {((PlanMessage)instruction).msg}");
                    continue;
                }

                if (instruction is PlanSlew) {
                    AddSlew((PlanSlew)instruction, plan.PlanTarget);
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
                    AddTakeExposure(plan.PlanTarget, instruction.planExposure);
                    continue;
                }

                if (instruction is PlanDither) {
                    AddDither();
                    continue;
                }

                if (instruction is PlanWait) {
                    AddWait(((PlanWait)instruction).waitForTime, plan.PlanTarget);
                    continue;
                }

                if (instruction is PlanBeforeTargetContainer) {
                    AddBeforeTargetInstructions();
                    continue;
                }

                TSLogger.Error($"unknown instruction type: {instruction.GetType().FullName}");
                throw new Exception($"unknown instruction type: {instruction.GetType().FullName}");
            }
        }

        private void AddSlew(PlanSlew instruction, IPlanTarget planTarget) {

            bool isPlateSolve = instruction.center;
            InputCoordinates slewCoordinates = new InputCoordinates(planTarget.Coordinates);
            SequenceItem slewCenter;

            string with = isPlateSolve ? "with" : "without";
            TSLogger.Info($"slew ({with} center): {Utils.FormatCoordinates(planTarget.Coordinates)}");

            if (isPlateSolve) {
                if (rotatorMediator.GetInfo().Connected) {
                    slewCenter = new CenterAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                    slewCenter.Name = nameof(CenterAndRotate);
                    (slewCenter as Center).Coordinates = slewCoordinates;
                    (slewCenter as CenterAndRotate).PositionAngle = planTarget.Rotation;
                }
                else {
                    slewCenter = new Center(profileService, telescopeMediator, imagingMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory);
                    slewCenter.Name = nameof(Center);
                    (slewCenter as Center).Coordinates = slewCoordinates;
                }
            }
            else {
                slewCenter = new SlewScopeToRaDec(telescopeMediator, guiderMediator);
                slewCenter.Name = nameof(SlewScopeToRaDec);
                (slewCenter as SlewScopeToRaDec).Coordinates = slewCoordinates;
            }

            SetItemDefaults(slewCenter, null);
            Add(slewCenter);
        }

        private void AddBeforeTargetInstructions() {
            int? numInstructions = parentContainer.BeforeTargetContainer.Items?.Count;
            if (numInstructions != null && numInstructions > 0) {
                TSLogger.Info($"adding BeforeNewTarget container with {numInstructions} instruction(s)");
                parentContainer.BeforeTargetContainer.ResetAll();
                Add(parentContainer.BeforeTargetContainer);
            }
        }

        private void AddSwitchFilter(IPlanExposure planExposure) {
            TSLogger.Info($"adding switch filter: {planExposure.FilterName}");

            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            SetItemDefaults(switchFilter, nameof(SwitchFilter));

            switchFilter.Filter = Utils.LookupFilter(profileService, planExposure.FilterName);
            Add(switchFilter);
        }

        private void AddSetReadoutMode(IPlanExposure planExposure) {
            int? readoutMode = planExposure.ReadoutMode;
            readoutMode = (readoutMode == null || readoutMode < 0) ? 0 : readoutMode;

            TSLogger.Info($"adding set readout mode: {readoutMode}");
            SetReadoutMode setReadoutMode = new SetReadoutMode(cameraMediator);
            SetItemDefaults(setReadoutMode, nameof(SetReadoutMode));

            setReadoutMode.Mode = (short)readoutMode;

            Add(setReadoutMode);
        }

        private void AddTakeExposure(IPlanTarget planTarget, IPlanExposure planExposure) {
            TSLogger.Info($"adding take exposure: {planExposure.FilterName} {planExposure.ExposureLength}s");

            PlanTakeExposure takeExposure = new PlanTakeExposure(
                        parentContainer,
                        synchronizationEnabled,
                        syncExposureTimeout,
                        profileService,
                        cameraMediator,
                        imagingMediator,
                        imageSaveMediator,
                        imageHistoryVM,
                        ImageSaveWatcher,
                        planExposure.DatabaseId);
            SetItemDefaults(takeExposure, nameof(TakeExposure));

            takeExposure.ExposureCount = GetExposureCount();
            takeExposure.ExposureTime = planExposure.ExposureLength;
            takeExposure.Gain = GetGain(planExposure.Gain);
            takeExposure.Offset = GetOffset(planExposure.Offset);
            takeExposure.Binning = planExposure.BinningMode;
            takeExposure.ROI = planTarget.ROI;

            Add(takeExposure);
        }

        private void AddDither() {
            TSLogger.Info("adding dither");
            Dither dither = new Dither(guiderMediator, profileService);
            Add(dither);
        }

        private void AddWait(DateTime waitForTime, IPlanTarget planTarget) {
            Add(new PlanWaitInstruction(guiderMediator, telescopeMediator, waitForTime));
        }

        private void EnsureUnparked(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().AtPark) {
                TSLogger.Info("telescope is parked before potential target slew: unparking");
                try {
                    telescopeMediator.UnparkTelescope(progress, token).Wait();
                }
                catch (Exception ex) {
                    TSLogger.Error($"failed to unpark telescope: {ex.Message}");
                    throw new SequenceEntityFailedException("Failed to unpark telescope");
                }
            }
        }

        private int GetSyncExposureTimeout() {
            ProfilePreference profilePreference;
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                profilePreference = context.GetProfilePreference(profileService.ActiveProfile.Id.ToString());
                if (profilePreference == null) {
                    return SyncManager.DEFAULT_SYNC_EXPOSURE_TIMEOUT;
                }
            }

            return profilePreference.SyncExposureTimeout;
        }

        private void SetItemDefaults(ISequenceItem item, string name) {
            if (name != null) {
                item.Name = name;
            }

            item.Category = INSTRUCTION_CATEGORY;
            item.Description = "";
            item.ErrorBehavior = this.ErrorBehavior;
            item.Attempts = this.Attempts;
        }

        private int GetExposureCount() {
            parentContainer.TotalExposureCount++;
            return parentContainer.TotalExposureCount;
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
