using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "SchedulerInstructionContainer")]
    [ExportMetadata("Description", "")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class InstructionContainer : SequentialContainer, ISequenceContainer, IValidatable {

        private Object lockObj = new Object();

        [ImportingConstructor]
        public InstructionContainer() : base() { }

        public InstructionContainer(string name, ISequenceContainer parent) : base() {
            Name = name;
            AttachNewParent(Parent);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            TSLogger.Info($"begin executing '{Name}' event instructions");
            Task t = base.Execute(progress, token);
            TSLogger.Info($"done executing '{Name}' event instructions, resetting progress for next execution");
            base.ResetAll();
            return t;
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
                }
                else {
                    base.MoveUp(item);
                }
            }
        }
    }
}
