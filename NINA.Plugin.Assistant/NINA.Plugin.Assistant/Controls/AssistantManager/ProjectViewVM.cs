using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProjectViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private ProjectProxy projectProxy;

        public ProjectProxy ProjectProxy {
            get => projectProxy;
            set {
                projectProxy = value;
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        public ProjectViewVM(AssistantManagerVM managerVM, IProfileService profileService, Project project) : base(profileService) {
            this.managerVM = managerVM;
            ProjectProxy = new ProjectProxy(project);

            InitializeRuleWeights(ProjectProxy.Proxy);
            InitializeCombos();

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            CopyCommand = new RelayCommand(Copy);
            DeleteCommand = new RelayCommand(Delete);
            AddTargetCommand = new RelayCommand(AddTarget);
            PasteTargetCommand = new RelayCommand(PasteTarget);
        }

        private void InitializeRuleWeights(Project project) {
            List<RuleWeight> ruleWeights = new List<RuleWeight>();

            project.RuleWeights.ForEach((rw) => {
                rw.PropertyChanged += ProjectProxy_PropertyChanged;
                ruleWeights.Add(rw);
            });

            RuleWeights = ruleWeights;
        }

        private void ProjectProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(ProjectProxy.Proxy)) {
                ProjectChanged = true;
            }
            else {
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        private void InitializeCombos() {

            MinimumTimeChoices = new List<string>();
            for (int i = 30; i <= 240; i += 30) {
                MinimumTimeChoices.Add(Utils.MtoHM(i));
            }

            MinimumAltitudeChoices = new List<string>();
            for (int i = 0; i <= 60; i += 5) {
                MinimumAltitudeChoices.Add(i + "°");
            }
        }

        private List<RuleWeight> ruleWeights = new List<RuleWeight>();
        public List<RuleWeight> RuleWeights {
            get => ruleWeights;
            set {
                ruleWeights = value;
                RaisePropertyChanged(nameof(RuleWeights));
            }
        }

        private List<string> _minimumTimeChoices;
        public List<string> MinimumTimeChoices {
            get => _minimumTimeChoices;
            set {
                _minimumTimeChoices = value;
                RaisePropertyChanged(nameof(MinimumTimeChoices));
            }
        }

        private List<string> _minimumAltitudeChoices;
        public List<string> MinimumAltitudeChoices {
            get {
                return _minimumAltitudeChoices;
            }
            set {
                _minimumAltitudeChoices = value;
                RaisePropertyChanged(nameof(MinimumAltitudeChoices));
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

        private bool projectChanged = false;
        public bool ProjectChanged {
            get => projectChanged;
            set {
                projectChanged = value;
                RaisePropertyChanged(nameof(ProjectChanged));
            }
        }

        public bool PasteEnabled {
            get => Clipboard.HasType(TreeDataType.Target);
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand AddTargetCommand { get; private set; }
        public ICommand PasteTargetCommand { get; private set; }

        private void Edit(object obj) {
            ProjectProxy.PropertyChanged += ProjectProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ProjectChanged = false;
        }

        private void Save(object obj) {
            ProjectProxy.Proxy.RuleWeights = RuleWeights;
            managerVM.SaveProject(ProjectProxy.Proxy);
            ProjectProxy.OnSave();
            InitializeRuleWeights(ProjectProxy.Proxy);
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            ShowEditView = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            ProjectProxy.OnCancel();
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            InitializeRuleWeights(ProjectProxy.Proxy);
            ShowEditView = false;
            managerVM.SetEditMode(false);
        }

        private void Copy(object obj) {
            managerVM.CopyItem();
        }

        private void Delete(object obj) {
            string message = $"Delete project '{ProjectProxy.Project.Name}' and any associated targets?  This cannot be undone.";
            ConfirmationMessageBox messageBox = new ConfirmationMessageBox(message, "Delete");
            if (messageBox.Show()) {
                managerVM.DeleteProject(ProjectProxy.Proxy);
            }
        }

        private void AddTarget(object obj) {
            managerVM.AddNewTarget(ProjectProxy.Proxy);
        }

        private void PasteTarget(object obj) {
            managerVM.PasteTarget(ProjectProxy.Proxy);
        }

    }
}
