using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProjectViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private IFramingAssistantVM framingAssistantVM;
        private ProjectProxy projectProxy;

        public ProjectProxy ProjectProxy {
            get => projectProxy;
            set {
                projectProxy = value;
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        public ProjectViewVM(AssistantManagerVM managerVM, IFramingAssistantVM framingAssistantVM, IProfileService profileService, Project project) : base(profileService) {
            this.managerVM = managerVM;
            this.framingAssistantVM = framingAssistantVM;
            ProjectProxy = new ProjectProxy(project);
            ProjectActive = ProjectProxy.Project.ActiveNowWithActiveTargets;

            InitializeRuleWeights(ProjectProxy.Proxy);
            InitializeCombos();

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            CopyCommand = new RelayCommand(Copy);
            DeleteCommand = new RelayCommand(Delete);
            AddTargetCommand = new RelayCommand(AddTarget);
            PasteTargetCommand = new RelayCommand(PasteTarget);
            ImportMosaicPanelsCommand = new RelayCommand(ImportMosaicPanels);
        }

        private void InitializeRuleWeights(Project project) {
            List<RuleWeight> ruleWeights = new List<RuleWeight>();

            project.RuleWeights.ForEach((rw) => {
                rw.PropertyChanged -= ProjectProxy_PropertyChanged;
                rw.PropertyChanged += ProjectProxy_PropertyChanged;
                ruleWeights.Add(rw);
            });

            RuleWeights = ruleWeights;
        }

        private void ProjectProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(ProjectProxy.Proxy)) {
                ItemEdited = true;
            }
            else {
                ProjectActive = ProjectProxy.Project.ActiveNowWithActiveTargets;
                RaisePropertyChanged(nameof(ProjectProxy));
            }
        }

        private bool projectActive;
        public bool ProjectActive {
            get {
                return projectActive;
            }
            set {
                projectActive = value;
                RaisePropertyChanged(nameof(ProjectActive));
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

        private bool itemEdited = false;
        public bool ItemEdited {
            get => itemEdited;
            set {
                itemEdited = value;
                RaisePropertyChanged(nameof(ItemEdited));
            }
        }

        public bool PasteEnabled {
            get => Clipboard.HasType(TreeDataType.Target);
        }

        public bool MosaicPanelsAvailable {
            get => FramingAssistantPanelsDefined() > 1;
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand AddTargetCommand { get; private set; }
        public ICommand PasteTargetCommand { get; private set; }
        public ICommand ImportMosaicPanelsCommand { get; private set; }

        private void Edit(object obj) {
            ProjectProxy.PropertyChanged += ProjectProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ItemEdited = false;
        }

        private void Save(object obj) {

            // Prevent save if minimum time setting is such that it would never allow a meridian window to work properly
            if (ProjectProxy.Proxy.MeridianWindow > 0 && ProjectProxy.Proxy.MinimumTime > (ProjectProxy.Proxy.MeridianWindow * 2)) {
                string message = $"Minimum Time must be less than twice the Meridian Window or the project will never be selected for imaging.";
                MyMessageBox.Show(message, "Oops");
                return;
            }

            ProjectProxy.Proxy.RuleWeights = RuleWeights;
            managerVM.SaveProject(ProjectProxy.Proxy);
            ProjectProxy.OnSave();
            InitializeRuleWeights(ProjectProxy.Proxy);
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            ProjectProxy.OnCancel();
            ProjectProxy.PropertyChanged -= ProjectProxy_PropertyChanged;
            InitializeRuleWeights(ProjectProxy.Proxy);
            ShowEditView = false;
            ItemEdited = false;
            managerVM.SetEditMode(false);
        }

        private void Copy(object obj) {
            managerVM.CopyItem();
        }

        private void Delete(object obj) {
            string message = $"Delete project '{ProjectProxy.Project.Name}' and any associated targets?  This cannot be undone.";
            if (MyMessageBox.Show(message, "Delete Project?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                managerVM.DeleteProject(ProjectProxy.Proxy);
            }
        }

        private void AddTarget(object obj) {
            managerVM.AddNewTarget(ProjectProxy.Proxy);
        }

        private void PasteTarget(object obj) {
            managerVM.PasteTarget(ProjectProxy.Proxy);
            ProjectActive = ProjectProxy.Project.ActiveNowWithActiveTargets;
        }

        private void ImportMosaicPanels(object obj) {
            int panels = FramingAssistantPanelsDefined();
            if (panels == 1) {
                MyMessageBox.Show("The Framing Assistant only defines one panel at the moment.", "Oops");
                return;
            }

            string message = $"Add {panels} mosaic panels as new targets to project '{ProjectProxy.Project.Name}'?";
            if (MyMessageBox.Show(message, "Add Targets?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                List<Target> targets = new List<Target>();
                foreach (FramingRectangle rect in framingAssistantVM.CameraRectangles) {
                    TSLogger.Debug($"Add mosaic panel as target: {rect.Name} {rect.Coordinates.RAString} {rect.Coordinates.DecString} rot={rect.DSOPositionAngle}");
                    Target target = new Target();
                    target.Name = rect.Name;
                    target.ra = rect.Coordinates.RA;
                    target.dec = rect.Coordinates.Dec;
                    target.Rotation = rect.DSOPositionAngle;
                    targets.Add(target);
                }

                managerVM.AddTargets(ProjectProxy.Project, targets);
            }
        }

        private int FramingAssistantPanelsDefined() {
            return framingAssistantVM.VerticalPanels * framingAssistantVM.HorizontalPanels;
        }
    }
}
