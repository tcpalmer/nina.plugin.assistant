using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class SchedulerTargetEndTimeTrigger : SequenceTrigger {

        DateTime EndTime;

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
            return DateTime.Now.AddSeconds(nextDuration) > EndTime;
        }
    }

}
