using Assistant.NINAPlugin.Plan;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Utility.DateTimeProvider;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Assistant")]
    [ExportMetadata("Description", "Run the Sequencer Assistant")]
    [ExportMetadata("Icon", "Assistant.AssistantSVG")]
    [ExportMetadata("Category", "Sequencer Assistant")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AssistantInstruction : SequentialContainer, IValidatable {

        /*
         * Check out some methods on the parent:
         * - override GetEstimatedDuration: estimate of how long this will take
         */

        /*
         * Lifecycle:
         * - construct/clone: when added to a sequence
         * - initialize: when the sequence is started (not when execution starts)
         * - execute: when the instruction is started
         * - teardown: when the instruction has completed or canceled
         * 
         * So initialize is where we can call the Assistant to get the plan.
         * A cancel has to be handled, e.g. remove any instructions added to the sequence under the hood and clear the plan
         */

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

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

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
                IWindowServiceFactory windowServiceFactory
            ) {

            Logger.Trace("Assistant ctor");
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

            // TODO: this can better be set via the ... on the instruction (see Smart Exposure)
            // Interestingly, DSO Container doesn't have ... but this instruction does.

            // TODO: also need to pay attention to Attempts - can also be set via ...
            this.ErrorBehavior = InstructionErrorBehavior.AbortOnError;
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
                cloneMe.windowServiceFactory
            ) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AssistantInstruction(this) { };
        }

        public override void Initialize() {
            Logger.Trace("Assistant initialize");
        }

        public override void Teardown() {
            Logger.Trace("Assistant teardown");
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(AssistantInstruction)}";
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("Assistant execute");

            while (true) {
                AssistantPlan plan = new Planner().GetPlan(DateTime.Now, profileService);

                if (plan == null) {
                    Logger.Info("Assistant: planner returned empty plan, done");
                    break;
                }

                PlanTarget planTarget = plan.PlanTarget;
                Logger.Info($"Assistant: starting execution of plan target: {planTarget.Name}");

                // If interval for this target has passed, we're done (really shouldn't happen)
                if (DateTime.Now > plan.TimeInterval.EndTime) {
                    Logger.Warning($"Assistant: time interval for the target has passed, end time: {planTarget.TimeInterval.EndTime}");
                    return Task.CompletedTask;
                }

                // Wait for the target start time
                WaitForStart(planTarget, progress, token);

                // TODO: needs to be accessible for binding from xaml
                AssistantStatusMonitor monitor = new AssistantStatusMonitor(planTarget);

                // Create a container for this target, add the instructions, and execute
                try {
                    AssistantTargetContainer targetContainer = GetTargetContainer(planTarget, monitor);
                    Logger.Debug("Assistant: executing target container instructions");
                    targetContainer.Execute(progress, token).Wait();
                    Logger.Debug("Assistant: done executing target container instructions");
                }
                catch (Exception ex) {
                    Logger.Error($"Assistant: exception\n{ex.ToString()}");
                }
            }

            return Task.CompletedTask;
        }

        private void WaitForStart(PlanTarget planTarget, IProgress<ApplicationStatus> progress, CancellationToken token) {
            DateTime now = DateTime.Now;
            if (planTarget.TimeInterval.StartTime > now) {
                TimeSpan duration = planTarget.TimeInterval.StartTime - now;
                Logger.Debug($"Assistant: waiting for target start time: {planTarget.TimeInterval.StartTime}");
                CoreUtil.Wait(duration, token, progress).Wait(token);
                Logger.Debug("Assistant: done waiting for target start time");
            }
        }

        private AssistantTargetContainer GetTargetContainer(PlanTarget planTarget, AssistantStatusMonitor monitor) {
            AssistantTargetContainer targetContainer = new AssistantTargetContainer(profileService, dateTimeProviders, telescopeMediator,
                rotatorMediator, guiderMediator, cameraMediator, imagingMediator, imageSaveMediator,
                imageHistoryVM, filterWheelMediator, domeMediator, domeFollower,
                plateSolverFactory, windowServiceFactory, planTarget, monitor);
            targetContainer.AttachNewParent(this);
            return targetContainer;
        }

        public override bool Validate() {
            var i = new ObservableCollection<string>();

            // TODO: see RoboCopyStart for howto
            // - could fire the Assistant and if nothing comes back -> 'nothing to image'
            // - could ensure connections: telescope, rotator (if), camera, filter wheel (if), guider (if dither), etc

            Issues = i;
            return i.Count == 0;
        }

    }
}
