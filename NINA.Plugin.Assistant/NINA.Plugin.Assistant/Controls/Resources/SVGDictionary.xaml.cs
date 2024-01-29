using System.ComponentModel.Composition;
using System.Windows;

namespace Assistant.NINAPlugin.Controls.Resources {

    [Export(typeof(ResourceDictionary))]
    public partial class SVGDictionary : ResourceDictionary {

        public SVGDictionary() {
            InitializeComponent();
        }
    }
}