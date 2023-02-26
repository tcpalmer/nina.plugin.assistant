namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class DocumentationLinkVM {

        private static readonly string ROOT_URL = "https://tcpalmer.github.io/docs/NINA/Assistant/";

        public DocumentationLinkVM(string uri, string linkText) {
            URL = ROOT_URL + uri;
            LinkText = linkText;
        }

        public string URL { get; private set; }
        public string LinkText { get; private set; }
    }
}
