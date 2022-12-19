using System.ComponentModel.Composition;
using System.Windows;

namespace Assistant.NINAPlugin.Sequencer {
    /// <summary>
    /// Interaction logic for AssistantInstructionTemplate.xaml
    /// </summary>
    [Export(typeof(ResourceDictionary))]
    public partial class AssistantInstructionTemplate : ResourceDictionary {
        public AssistantInstructionTemplate() {
            InitializeComponent();
        }
    }
}
