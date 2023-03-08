using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ExposureTemplateViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private IProfile profile;

        public ExposureTemplateViewVM(AssistantManagerVM managerVM, IProfileService profileService, ExposureTemplate exposureTemplate) : base(profileService) {
            this.managerVM = managerVM;
            this.profile = managerVM.GetProfile(exposureTemplate.ProfileId);

            ExposureTemplateProxy = new ExposureTemplateProxy(exposureTemplate);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            InitializeCombos();
        }

        private void InitializeCombos() {
            FilterNameChoices = GetFilterNamesForProfile();

            BinningModeChoices = new List<BinningMode> {
                    new BinningMode(1,1),
                    new BinningMode(2,2),
                    new BinningMode(3,3),
                    new BinningMode(4,4),
            };
        }

        private List<string> GetFilterNamesForProfile() {
            var filterNames = new List<string>();

            foreach (FilterInfo filterInfo in profile?.FilterWheelSettings?.FilterWheelFilters) {
                filterNames.Add(filterInfo.Name);
            }

            return filterNames;
        }

        private List<string> _filterNameChoices;
        public List<string> FilterNameChoices {
            get => _filterNameChoices;
            set {
                _filterNameChoices = value;
                RaisePropertyChanged(nameof(FilterNameChoices));
            }
        }

        private List<BinningMode> _binningModeChoices;
        public List<BinningMode> BinningModeChoices {
            get => _binningModeChoices;
            set {
                _binningModeChoices = value;
                RaisePropertyChanged(nameof(BinningModeChoices));
            }
        }

        private ExposureTemplateProxy exposureTemplateProxy;
        public ExposureTemplateProxy ExposureTemplateProxy {
            get => exposureTemplateProxy;
            set {
                exposureTemplateProxy = value;
                RaisePropertyChanged(nameof(ExposureTemplateProxy));
            }
        }

        private void ExposureTemplateProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(TargetProxy.Proxy)) {
                ItemEdited = true;
            }
            else {
                RaisePropertyChanged(nameof(ExposureTemplateProxy));
            }
        }

        private bool showEditView = false;
        public bool ShowEditView {
            get => showEditView;
            set {
                showEditView = value;
                RaisePropertyChanged(nameof(ShowEditView));
            }
        }

        private bool itemEdited = false;
        public bool ItemEdited {
            get => itemEdited;
            set {
                itemEdited = value;
                RaisePropertyChanged(nameof(ItemEdited));
            }
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void Edit(object obj) {
            ExposureTemplateProxy.PropertyChanged += ExposureTemplateProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ItemEdited = false;
        }

        private void Save(object obj) {
            managerVM.SaveExposureTemplate(ExposureTemplateProxy.Proxy);
            ExposureTemplateProxy.OnSave();
            ExposureTemplateProxy.PropertyChanged -= ExposureTemplateProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            ExposureTemplateProxy.OnCancel();
            ExposureTemplateProxy.PropertyChanged -= ExposureTemplateProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

    }
}
