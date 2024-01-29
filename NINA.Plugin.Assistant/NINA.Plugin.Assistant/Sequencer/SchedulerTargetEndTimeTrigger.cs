using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class SchedulerTargetEndTimeTrigger : SequenceTrigger {
        private DateTime EndTime;

        public SchedulerTargetEndTimeTrigger(DateTime endTime) {
            Name = nameof(SchedulerTargetEndTimeTrigger);
            Category = PlanTargetContainer.INSTRUCTION_CATEGORY;
            this.EndTime = endTime;
        }

        public override object Clone() {
            throw new NotImplementedException();
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Info("target stop time exceeded, interrupting target container");
            this.Parent.Interrupt().Wait(token);
            return Task.CompletedTask;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            double nextDuration = nextItem?.GetEstimatedDuration().TotalSeconds ?? 0;
            bool shouldTrigger = DateTime.Now.AddSeconds(nextDuration) > EndTime;
            if (shouldTrigger) {
                TSLogger.Info($"will trigger scheduler target plan stop: now plus next duration {nextDuration} > endTime {Utils.FormatDateTimeFull(EndTime)}");
            }

            return shouldTrigger;
        }
    }
}