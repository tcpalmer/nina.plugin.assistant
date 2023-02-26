using NINA.Core.Utility;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public partial class DocumentationLink : UserControl {

        private static readonly string ROOT_URL = "https://tcpalmer.github.io/docs/NINA/Assistant";

        public DocumentationLink() {
            InitializeComponent();
            DataContext = this;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            try {
                Process.Start(URL);
            }
            catch (Exception ex) {
                Logger.Error($"failed to open HTTP link {URL}: {ex.Message}");
            }

            e.Handled = true;
        }

        public string URL {
            get { return ROOT_URL + (string)GetValue(URIProperty); }
            set { SetValue(URIProperty, value); }
        }

        public string LinkText {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        public static readonly DependencyProperty URIProperty =
            DependencyProperty.Register("URL", typeof(string), typeof(DocumentationLink), new PropertyMetadata(null));

        public static readonly DependencyProperty LinkTextProperty =
            DependencyProperty.Register("LinkText", typeof(string), typeof(DocumentationLink), new PropertyMetadata(null));
    }
}
