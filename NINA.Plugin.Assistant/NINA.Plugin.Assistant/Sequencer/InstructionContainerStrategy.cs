using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// This is only used so that we can recognize calls to TS Condition that originate from operation of TS.  See
    /// TargetSchedulerCondition which skips its Check if called from some TS action.
    /// </summary>
    internal class InstructionContainerStrategy : IExecutionStrategy {
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