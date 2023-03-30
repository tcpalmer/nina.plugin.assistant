using Assistant.NINAPlugin.Plan;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// Modified from NINA.Sequencer.Container.ExecutionStrategy.SequentialStrategy.  This strategy performs two key functions:
    /// - It manages the status monitor to keep track of progress for the parent TargetSchedulerContainer.
    /// - It runs ancestor triggers and conditions as needed on the parent of TargetSchedulerContainer, skipping
    ///   TargetSchedulerContainer itself.
    /// </summary>
    public class PlanTargetContainerStrategy : IExecutionStrategy {

        public object Clone() {
            return new PlanTargetContainerStrategy();
        }

        private ISequenceContainer parentContainer;
        private SchedulerPlan schedulerPlan;
        private SchedulerStatusMonitor monitor;
        private Queue<InstructionMonitor> instructionMonitorQueue;

        public void SetContext(TargetSchedulerContainer parentContainer, SchedulerPlan schedulerPlan, SchedulerStatusMonitor monitor) {
            this.parentContainer = parentContainer;
            this.schedulerPlan = schedulerPlan;
            this.monitor = monitor;

            instructionMonitorQueue = new Queue<InstructionMonitor>();
            foreach (IPlanInstruction instruction in schedulerPlan.PlanInstructions) {
                Logger.Debug($"PLAN INSTRUCTION: {instruction.GetType()}");

                if (instruction is PlanMessage) {
                    continue;
                }

                if (instruction is PlanSlew) {
                    instructionMonitorQueue.Enqueue(new InstructionMonitor(schedulerPlan.PlanTarget.PlanId, "Slew"));
                    continue;
                }

                if (instruction is PlanSwitchFilter) {
                    instructionMonitorQueue.Enqueue(new InstructionMonitor(instruction.planExposure.PlanId, "SwitchFilter"));
                    continue;
                }

                if (instruction is PlanSetReadoutMode) {
                    instructionMonitorQueue.Enqueue(new InstructionMonitor(instruction.planExposure.PlanId, "SetReadoutMode"));
                    continue;
                }

                if (instruction is Plan.PlanTakeExposure) {
                    instructionMonitorQueue.Enqueue(new InstructionMonitor(instruction.planExposure.PlanId, "TakeExposure"));
                    continue;
                }

                if (instruction is PlanWait) {
                    instructionMonitorQueue.Enqueue(new InstructionMonitor(schedulerPlan.PlanTarget.PlanId, "Wait"));
                    continue;
                }

                throw new Exception($"Unknown instruction type in PlanTargetContainerStrategy: {instruction.GetType()}");
            }
        }

        public async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("PlanTargetContainerStrategy: Execute");

            ISequenceItem previous = null;
            ISequenceItem next = null;
            bool canContinue = true;
            var root = ItemUtility.GetRootContainer(context);

            context.Iterations = 0;
            InitializeBlock(context);

            try {
                while (((next, canContinue) = GetNextItem(context, previous)).next != null && canContinue) {
                    StartBlock(context);

                    (next, canContinue) = GetNextItem(context, previous);
                    while (next != null && canContinue) {
                        token.ThrowIfCancellationRequested();
                        await RunTriggers(context, previous, next, progress, token);

                        InstructionMonitor instructionMonitor = instructionMonitorQueue.Dequeue();
                        monitor.ItemStart(instructionMonitor.Id, instructionMonitor.Name);
                        await next.Run(progress, token);
                        monitor.ItemFinish(instructionMonitor.Id, instructionMonitor.Name);

                        previous = next;

                        (next, canContinue) = GetNextItem(context, previous);
                        await RunTriggersAfter(context, previous, next, progress, token);
                    }

                    FinishBlock(context);

                    if (CanContinue(context, previous, next)) {
                        foreach (var item in context.GetItemsSnapshot()) {
                            if (item is ISequenceContainer) {
                                (item as ISequenceContainer).ResetAll();
                            }
                            else {
                                item.ResetProgress();
                            }
                        }
                    }
                }

                //Mark rest of items as skipped
                foreach (var item in context.GetItemsSnapshot().Where(x => x.Status == SequenceEntityStatus.CREATED)) {
                    item.Skip();
                }
            }
            finally {
                TeardownBlock(context);
            }
        }

        public void RunTriggers() {
            Logger.Debug("In My Strategy: RunTriggers");
        }

        private void TeardownBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockTeardown();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockTeardown();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockTeardown();
                }
            }
        }

        private void InitializeBlock(ISequenceContainer context) {
            foreach (var item in context.GetItemsSnapshot()) {
                item.SequenceBlockInitialize();
            }

            var conditionable = context as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockInitialize();
                }
            }
            var triggerable = context as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockInitialize();
                }
            }
        }

        private (ISequenceItem, bool) GetNextItem(ISequenceContainer context, ISequenceItem previous) {
            var items = context.GetItemsSnapshot();
            var next = items.FirstOrDefault(x => x.Status == SequenceEntityStatus.CREATED);

            var canContinue = false;
            if (next != null) {
                canContinue = CanContinue(context, previous, next);
            }

            return (next, canContinue);
        }

        private async Task RunTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {

            // Run the triggers attached here
            Logger.Debug($"PlanTargetContainerStrategy: RunTriggers, next is {nextItem?.GetType()}");
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            // Run the triggers on ancestor containers above our parent
            if (parentContainer.Parent != null) {
                await AncestorsRunTriggers(parentContainer.Parent, previousItem, nextItem, progress, token);
            }
        }

        private async Task AncestorsRunTriggers(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug($"PlanTargetContainerStrategy: AncestorsRunTriggers, next is {nextItem?.GetType()}");
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggers(previousItem, nextItem, progress, token);
            }

            if (container.Parent != null) {
                await AncestorsRunTriggers(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private async Task RunTriggersAfter(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {

            // Run the triggers attached here
            Logger.Debug($"PlanTargetContainerStrategy: RunTriggersAfter, previous is {previousItem?.GetType()}");
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggersAfter(previousItem, nextItem, progress, token);
            }

            // Run the triggers on ancestor containers above our parent
            if (parentContainer.Parent != null) {
                await AncestorsRunTriggersAfter(parentContainer.Parent, previousItem, nextItem, progress, token);
            }
        }

        private async Task AncestorsRunTriggersAfter(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug($"PlanTargetContainerStrategy: AncestorsRunTriggersAfter, previous is {previousItem?.GetType()}");
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                await triggerable.RunTriggersAfter(previousItem, nextItem, progress, token);
            }

            if (container.Parent != null) {
                await AncestorsRunTriggersAfter(container.Parent, previousItem, nextItem, progress, token);
            }
        }

        private void StartBlock(ISequenceContainer container) {
            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockStarted();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockStarted();
                }
            }
            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockStarted();
                }
            }
        }

        private void FinishBlock(ISequenceContainer container) {
            container.Iterations++;

            foreach (var item in container.GetItemsSnapshot()) {
                item.SequenceBlockFinished();
            }

            var conditionable = container as IConditionable;
            if (conditionable != null) {
                foreach (var condition in conditionable.GetConditionsSnapshot()) {
                    condition.SequenceBlockFinished();
                }
            }

            var triggerable = container as ITriggerable;
            if (triggerable != null) {
                foreach (var trigger in triggerable.GetTriggersSnapshot()) {
                    trigger.SequenceBlockFinished();
                }
            }
        }

        private bool CanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {

            // Check conditions here (shouldn't be any on PlanTargetContainer)
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot()?.Where(x => x.Status != SequenceEntityStatus.DISABLED).ToList();
            if (conditions != null && conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            }
            else {
                canContinue = container.Iterations < 1;
            }

            // Check conditions on ancestor containers above our parent
            if (parentContainer.Parent != null) {
                canContinue = canContinue && AncestorsCanContinue(parentContainer.Parent, previousItem, nextItem);
            }

            return canContinue;
        }

        private bool AncestorsCanContinue(ISequenceContainer container, ISequenceItem previousItem, ISequenceItem nextItem) {
            var conditionable = container as IConditionable;
            var canContinue = false;
            var conditions = conditionable?.GetConditionsSnapshot()?.Where(x => x.Status != SequenceEntityStatus.DISABLED).ToList();
            if (conditions != null && conditions.Count > 0) {
                canContinue = conditionable.CheckConditions(previousItem, nextItem);
            }
            else {
                canContinue = container.Iterations < 1;
            }

            if (container.Parent != null) {
                canContinue = canContinue && AncestorsCanContinue(container.Parent, previousItem, nextItem);
            }

            return canContinue;
        }
    }

    class InstructionMonitor {

        public string Id { get; private set; }
        public string Name { get; private set; }

        public InstructionMonitor(string id, string name) {
            Id = id;
            Name = name;
        }
    }
}
