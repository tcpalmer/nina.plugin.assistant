using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Utility;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProjectViewVM : BaseINPC {

        private ProjectProxy projectProxy;

        public ProjectProxy ProjectProxy {
            get => projectProxy;
            set {
                projectProxy = value;
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        public ProjectViewVM(Project project) {
            ProjectProxy = new ProjectProxy(project);

            InitializeCombos();
            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
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

        private bool showProjectEditView = false;
        public bool ShowProjectEditView {
            get => showProjectEditView;
            set {
                showProjectEditView = value;
                RaisePropertyChanged(nameof(ShowProjectEditView));
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

        public ICommand EditCommand { get; private set; }
        private void Edit(object obj) {
            ProjectProxy.PropertyChanged += ProjectProxy_PropertyChanged;
            ShowProjectEditView = true;
            ProjectChanged = false;
        }

        public ICommand SaveCommand { get; private set; }
        private void Save(object obj) {
            Logger.Info("SAVE ...");
            // TODO: execute the save, ProjectProxy.Proxy should be the edited version
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            ShowProjectEditView = false;
        }

        public ICommand CancelCommand { get; private set; }
        private void Cancel(object obj) {
            Logger.Info("CANCEL ...");
            ProjectProxy.RestoreOnEditCancel();
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            ShowProjectEditView = false;
        }

    }
}
