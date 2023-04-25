using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProfilePreferencesViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private ProfilePreference profilePreference;

        public string ProfileName { get; private set; }

        private ProfilePreferenceProxy profilePreferenceProxy;

        public ProfilePreferenceProxy ProfilePreferenceProxy {
            get => profilePreferenceProxy;
            set {
                profilePreferenceProxy = value;
                RaisePropertyChanged(nameof(ProfilePreferenceProxy));
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

        public ProfilePreferencesViewVM(AssistantManagerVM managerVM, IProfileService profileService, ProfilePreference profilePreference, string profileName) : base(profileService) {
            this.managerVM = managerVM;
            this.profilePreference = profilePreference;

            ProfileName = profileName;
            ProfilePreferenceProxy = new ProfilePreferenceProxy(profilePreference);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void ProfilePreferenceProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(ProfilePreferenceProxy.Proxy)) {
                ItemEdited = true;
            }
            else {
                RaisePropertyChanged(nameof(ProfilePreferenceProxy));
            }
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void Edit(object obj) {
            ProfilePreferenceProxy.PropertyChanged += ProfilePreferenceProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ItemEdited = false;
        }

        private void Save(object obj) {
            managerVM.SaveProfilePreference(ProfilePreferenceProxy.ProfilePreference);
            ProfilePreferenceProxy.OnSave();
            ProfilePreferenceProxy.PropertyChanged -= ProfilePreferenceProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            ProfilePreferenceProxy.OnCancel();
            ProfilePreferenceProxy.PropertyChanged -= ProfilePreferenceProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

    }
}
