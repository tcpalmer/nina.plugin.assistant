using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {
    /*
    [ExportMetadata("Name", "Target Scheduler Test Trigger")]
    [ExportMetadata("Description", "Test Trigger")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    */

    public class TSTestTrigger : SequenceTrigger {

        //[ImportingConstructor]
        public TSTestTrigger() {
            Name = nameof(TSTestTrigger);
            Category = PlanTargetContainer.INSTRUCTION_CATEGORY;
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Info("TSTestTrigger TRIGGERED");
            return Task.CompletedTask;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            TSLogger.Info("TSTestTrigger ShouldTrigger?");
            if (nextItem == null) { return false; }
            if (!(nextItem is IExposureItem exposureItem)) { return false; }
            if (exposureItem.ImageType != "LIGHT") { return false; }

            TSLogger.Info("TSTestTrigger WILL TRIGGER");
            return true;
        }

        public override object Clone() {
            return new TSTestTrigger();
        }
    }
}