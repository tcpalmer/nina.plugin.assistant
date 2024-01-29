using Newtonsoft.Json;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "SchedulerInstructionContainer")]
    [ExportMetadata("Description", "")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class InstructionContainer : SequenceContainer, ISequenceContainer, IValidatable {
        private Object lockObj = new Object();

        [ImportingConstructor]
        public InstructionContainer() : base(new InstructionContainerStrategy()) { }

        public InstructionContainer(string name, ISequenceContainer parent) : base(new InstructionContainerStrategy()) {
            Name = name;
            AttachNewParent(Parent);
        }

        public override void Initialize() {
            foreach (ISequenceItem item in Items) {
                item.Initialize();
            }

            base.Initialize();
        }

        public override object Clone() {
            InstructionContainer ic = new InstructionContainer(Name, Parent);
            ic.Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem));
            foreach (var item in ic.Items) {
                item.AttachNewParent(ic);
            }

            AttachNewParent(Parent);
            return ic;
        }

        public new void MoveUp(ISequenceItem item) {
            lock (lockObj) {
                var index = Items.IndexOf(item);
                if (index == 0) {
                    return;
                } else {
                    base.MoveUp(item);
                }
            }
        }
    }
}