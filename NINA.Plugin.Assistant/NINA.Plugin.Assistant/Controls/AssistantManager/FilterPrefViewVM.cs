using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class FilterPrefViewVM : BaseVM {

        private AssistantManagerVM managerVM;

        public FilterPrefViewVM(AssistantManagerVM managerVM, IProfileService profileService, FilterPreference filterPreference) : base(profileService) {
            this.managerVM = managerVM;
            FilterPreferenceProxy = new FilterPreferenceProxy(filterPreference);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private FilterPreferenceProxy filterPreferenceProxy;
        public FilterPreferenceProxy FilterPreferenceProxy {
            get => filterPreferenceProxy;
            set {
                filterPreferenceProxy = value;
                RaisePropertyChanged(nameof(FilterPreferenceProxy));
            }
        }

        private void FilterPreferenceProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(TargetProxy.Proxy)) {
                ItemEdited = true;
            }
            else {
                RaisePropertyChanged(nameof(FilterPreferenceProxy));
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
            FilterPreferenceProxy.PropertyChanged += FilterPreferenceProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ItemEdited = false;
        }

        private void Save(object obj) {
            managerVM.SaveFilterPreference(FilterPreferenceProxy.Proxy);
            FilterPreferenceProxy.OnSave();
            FilterPreferenceProxy.PropertyChanged -= FilterPreferenceProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            FilterPreferenceProxy.OnCancel();
            FilterPreferenceProxy.PropertyChanged -= FilterPreferenceProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

    }
}
