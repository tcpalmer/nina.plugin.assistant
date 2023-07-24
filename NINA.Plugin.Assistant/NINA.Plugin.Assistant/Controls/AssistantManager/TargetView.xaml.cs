using System.Windows.Controls;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public partial class TargetView : UserControl {
        public TargetView() {
            InitializeComponent();
        }

        /// <summary>
        /// This is needed so that we can update the ExposureTemplate object on an edited ExposurePlan.  Without this,
        /// you only update the ExposureTemplateId but the ExposureTemplate (which is used for the Name) remains the
        /// same on the proxy and so doesn't update in the UI after the ExposureTemplate choice is changed but before
        /// save.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExposureTemplateId_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox != null) {
                var exposureTemplateChoices = (this.DataContext as TargetViewVM).ExposureTemplateChoices;
                var proxyPlans = (this.DataContext as TargetViewVM).TargetProxy.Target.ExposurePlans;
                int currentExposurePlanRow = this.ExposurePlansDataGrid.SelectedIndex;

                int newExposureTemplateId = exposureTemplateChoices[comboBox.SelectedIndex].Key;
                int oldExposureTemplateId = proxyPlans[currentExposurePlanRow].ExposureTemplateId;

                if (newExposureTemplateId != oldExposureTemplateId) {
                    var templates = (this.DataContext as TargetViewVM).exposureTemplates;
                    proxyPlans[currentExposurePlanRow].ExposureTemplate = templates[comboBox.SelectedIndex];
                }
            }
        }
    }
}
