using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// This is only used so that we can recognize calls to TS Condition that originate from operation of TS.  See
    /// TargetSchedulerCondition which skips its Check if called from some TS action.
    /// </summary>
    class InstructionContainerStrategy : IExecutionStrategy {

        private SequentialStrategy sequentialStrategy;

        public InstructionContainerStrategy() {
            sequentialStrategy = new SequentialStrategy();
        }

        public object Clone() {
            return sequentialStrategy.Clone();
        }

        public Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            return sequentialStrategy.Execute(context, progress, token);
        }

    }
}
