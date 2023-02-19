using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TargetViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private TargetProxy targetProxy;

        public TargetProxy TargetProxy {
            get => targetProxy;
            set {
                targetProxy = value;
                RaisePropertyChanged(nameof(TargetProxy));
            }
        }

        public TargetViewVM(AssistantManagerVM managerVM, IProfileService profileService, Target target) : base(profileService) {
            this.managerVM = managerVM;
            TargetProxy = new TargetProxy(target);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            CopyCommand = new RelayCommand(Copy);
            DeleteCommand = new RelayCommand(Delete);
        }

        private void TargetProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(TargetProxy.Proxy)) {
                TargetChanged = true;
            }
            else {
                RaisePropertyChanged(nameof(TargetProxy));
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

        private bool targetChanged = false;
        public bool TargetChanged {
            get => targetChanged;
            set {
                targetChanged = value;
                RaisePropertyChanged(nameof(TargetChanged));
            }
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        private void Edit(object obj) {
            TargetProxy.PropertyChanged += TargetProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            TargetChanged = false;
        }

        private void Save(object obj) {
            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();
            TargetProxy.PropertyChanged -= TargetProxy_PropertyChanged;
            ShowEditView = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            TargetProxy.OnCancel();
            TargetProxy.PropertyChanged -= TargetProxy_PropertyChanged;
            ShowEditView = false;
            managerVM.SetEditMode(false);
        }

        private void Copy(object obj) {
            managerVM.CopyItem();
        }

        private void Delete(object obj) {
            string message = $"Delete target '{TargetProxy.Target.Name}'?  This cannot be undone.";
            ConfirmationMessageBox messageBox = new ConfirmationMessageBox(message, "Delete");
            if (messageBox.Show()) {
                managerVM.DeleteTarget(TargetProxy.Proxy);
            }
        }

    }
}
