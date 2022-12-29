using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class AssistantTargetEndTimeTrigger : SequenceTrigger {

        DateTime EndTime;

        public AssistantTargetEndTimeTrigger(DateTime endTime) {
            Name = nameof(AssistantTargetEndTimeTrigger);
            Category = "Assistant";
            this.EndTime = endTime;
        }

        public override object Clone() {
            throw new NotImplementedException();
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Info("AssistantTargetEndTimeTrigger: target stop time exceeded, interrupting target container");
            this.Parent.Interrupt().Wait(token);
            return Task.CompletedTask;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            Logger.Trace("AssistantTargetEndTimeTrigger: trigger check");
            double nextDuration = nextItem?.GetEstimatedDuration().TotalSeconds ?? 0;
            return DateTime.Now.AddSeconds(nextDuration) > EndTime;
        }
    }

}
