using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Utility;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using System.ComponentModel.Composition;

namespace Assistant.NINAPlugin.Sequencer {

    /* disabling for now
    [ExportMetadata("Name", "Target Scheduler Test Condition")]
    [ExportMetadata("Description", "Test Condition for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    */
    public class SchedulerTestCondition : LoopCondition {

        [ImportingConstructor]
        public SchedulerTestCondition() : base() { }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            TSLogger.Debug($"TEST CONDITION: Check p={GetParentType()}");
            return base.Check(previousItem, nextItem);
        }

        private SchedulerTestCondition(SchedulerTestCondition cloneMe) : this() {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new SchedulerTestCondition(this) {
                Iterations = Iterations
            };
        }

        private string GetParentType() {
            return Parent != null ? Parent.GetType().ToString() : "";
        }

    }
}
