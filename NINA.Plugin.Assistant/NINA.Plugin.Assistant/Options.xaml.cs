using System.ComponentModel.Composition;
using System.Windows;

namespace Assistant.NINAPlugin {

    [Export(typeof(ResourceDictionary))]
    public partial class Options : ResourceDictionary {

        public Options() {
            InitializeComponent();
        }
    }
}