using ModernMessageBoxLib;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ConfirmationMessageBox {

        private ModernMessageBox messageBox;

        public ConfirmationMessageBox(string message = "REPLACE ME", string acceptText = "OK", string cancelText = "Cancel") {

            messageBox = new ModernMessageBox(message, "Are you sure?", ModernMessageboxIcons.Warning, acceptText, cancelText) {
                Button1Key = Key.Enter,
                Button2Key = Key.Escape,
                // TODO: this styling isn't working ...
                FontSize = 10,
                FontWeight = FontWeights.Normal,
                FontFamily = new FontFamily("Segoe UI")
            };
        }

        public bool Show() {
            messageBox.ShowDialog();
            return messageBox.Result == ModernMessageboxResult.Button1;
        }
    }

}
