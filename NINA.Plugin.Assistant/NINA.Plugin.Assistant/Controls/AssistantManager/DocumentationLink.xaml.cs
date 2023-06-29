using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public partial class DocumentationLink : UserControl {

        private static readonly string ROOT_URL = "https://tcpalmer.github.io/nina-scheduler/";

        public DocumentationLink() {
            InitializeComponent();
            DataContext = this;
        }

        private void OpenLink(object sender, RoutedEventArgs e) {
            try {
                Process.Start(new ProcessStartInfo(URL) { UseShellExecute = true });
            }
            catch (Exception ex) {
                TSLogger.Error($"failed to open HTTP link {URL}: {ex.Message}");
            }

            e.Handled = true;
        }

        public string URL {
            get { return ROOT_URL + (string)GetValue(URIProperty); }
            set { SetValue(URIProperty, value); }
        }

        public static readonly DependencyProperty URIProperty =
            DependencyProperty.Register("URL", typeof(string), typeof(DocumentationLink), new PropertyMetadata(null));
    }
}
