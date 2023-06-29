using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ExposureTemplateProfileViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private ProfileMeta profile;
        private TreeDataItem parentItem;
        private List<ExposureTemplate> exposureTemplates;

        public ProfileMeta Profile {
            get => profile;
            set {
                profile = value;
                RaisePropertyChanged(nameof(Profile));
            }
        }

        public List<ExposureTemplate> ExposureTemplates {
            get => exposureTemplates;
            set {
                exposureTemplates = value;
                RaisePropertyChanged(nameof(ExposureTemplates));
            }
        }

        public bool PasteEnabled {
            get => Clipboard.HasType(TreeDataType.ExposureTemplate);
        }

        public ExposureTemplateProfileViewVM(AssistantManagerVM managerVM, IProfileService profileService, TreeDataItem profileItem) : base(profileService) {
            this.managerVM = managerVM;
            Profile = (ProfileMeta)profileItem.Data;
            parentItem = profileItem;
            ExposureTemplates = InitExposureTemplates(profileItem);

            AddExposureTemplateCommand = new RelayCommand(AddExposureTemplate);
            PasteExposureTemplateCommand = new RelayCommand(PasteExposureTemplate);
            ViewExposureTemplateCommand = new RelayCommand(ViewExposureTemplate);
            CopyExposureTemplateCommand = new RelayCommand(CopyExposureTemplate);
        }

        private List<ExposureTemplate> InitExposureTemplates(TreeDataItem profileItem) {
            List<ExposureTemplate> exposureTemplates = new List<ExposureTemplate>();
            foreach (TreeDataItem item in profileItem.Items) {
                exposureTemplates.Add((ExposureTemplate)item.Data);
            }

            return exposureTemplates;
        }

        public ICommand AddExposureTemplateCommand { get; private set; }
        public ICommand PasteExposureTemplateCommand { get; private set; }
        public ICommand ViewExposureTemplateCommand { get; private set; }
        public ICommand CopyExposureTemplateCommand { get; private set; }

        private void AddExposureTemplate(object obj) {
            managerVM.AddNewExposureTemplate(parentItem);
        }

        private void PasteExposureTemplate(object obj) {
            managerVM.PasteExposureTemplate(parentItem);
        }

        private void ViewExposureTemplate(object obj) {
            ExposureTemplate exposureTemplate = obj as ExposureTemplate;
            if (exposureTemplate != null) {
                TreeDataItem item = Find(exposureTemplate);
                if (item != null) {
                    managerVM.NavigateTo(item);
                }
            }
        }

        private void CopyExposureTemplate(object obj) {
            ExposureTemplate exposureTemplate = obj as ExposureTemplate;
            if (exposureTemplate != null) {
                TreeDataItem item = Find(exposureTemplate);
                if (item != null) {
                    Clipboard.SetItem(item);
                    RaisePropertyChanged(nameof(PasteEnabled));
                }
            }
        }

        private TreeDataItem Find(ExposureTemplate exposureTemplate) {
            foreach (TreeDataItem item in parentItem.Items) {
                if (((ExposureTemplate)item.Data).Id == exposureTemplate.Id) {
                    return item;
                }
            }

            return null;
        }
    }
}
