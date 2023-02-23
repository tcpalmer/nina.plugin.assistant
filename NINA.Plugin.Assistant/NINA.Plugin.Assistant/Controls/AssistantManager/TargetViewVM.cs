using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TargetViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private IProfile profile;

        public TargetViewVM(AssistantManagerVM managerVM,
            IProfileService profileService,
            IApplicationMediator applicationMediator,
            IFramingAssistantVM framingAssistantVM,
            IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
            IPlanetariumFactory planetariumFactory,
            Target target)
            : base(profileService) {

            this.managerVM = managerVM;
            TargetProxy = new TargetProxy(target);

            profile = managerVM.GetProfile(target.Project.ProfileId);

            InitializeCombos();
            InitializeExposurePlans(TargetProxy.Proxy);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            CopyCommand = new RelayCommand(Copy);
            DeleteCommand = new RelayCommand(Delete);

            ShowTargetImportViewCommand = new RelayCommand(ShowTargetImportViewCmd);

            AddExposurePlanCommand = new RelayCommand(AddExposurePlan);
            DeleteExposurePlanCommand = new RelayCommand(DeleteExposurePlan);

            SendCoordinatesToFramingAssistantCommand = new AsyncCommand<bool>(async () => {
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                return await framingAssistantVM.SetCoordinates(TargetDSO);
            });

            TargetImportVM = new TargetImportVM(deepSkyObjectSearchVM, planetariumFactory);
            TargetImportVM.PropertyChanged += ImportTarget_PropertyChanged;
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

        private TargetProxy targetProxy;
        public TargetProxy TargetProxy {
            get => targetProxy;
            set {
                targetProxy = value;
                RaisePropertyChanged(nameof(TargetProxy));
            }
        }

        private void TargetProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(TargetProxy.Proxy)) {
                ItemEdited = true;
            }
            else {
                RaisePropertyChanged(nameof(TargetProxy));
            }
        }

        public DeepSkyObject TargetDSO {
            get {
                Target target = TargetProxy.Target;
                DeepSkyObject dso = new DeepSkyObject(string.Empty, target.Coordinates, profileService.ActiveProfile.ApplicationSettings.SkyAtlasImageRepository, profileService.ActiveProfile.AstrometrySettings.Horizon);
                dso.Name = target.Name;
                return dso;
            }
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

        private void InitializeExposurePlans(Target target) {
            List<ExposurePlan> exposurePlans = new List<ExposurePlan>();

            target.ExposurePlans.ForEach((plan) => {
                plan.PropertyChanged -= TargetProxy_PropertyChanged;
                plan.PropertyChanged += TargetProxy_PropertyChanged;
                exposurePlans.Add(plan);
            });

            ExposurePlans = exposurePlans;
        }

        private List<ExposurePlan> exposurePlans = new List<ExposurePlan>();
        public List<ExposurePlan> ExposurePlans {
            get => exposurePlans;
            set {
                exposurePlans = value;
                RaisePropertyChanged(nameof(ExposurePlans));
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

        private bool showTargetImportView = false;
        public bool ShowTargetImportView {
            get => showTargetImportView;
            set {
                showTargetImportView = value;
                RaisePropertyChanged(nameof(ShowTargetImportView));
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

        private TargetImportVM targetImportVM;
        public TargetImportVM TargetImportVM { get => targetImportVM; set => targetImportVM = value; }

        private void ImportTarget_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (TargetImportVM.Target.Name != null) {
                TargetProxy.Proxy.Name = TargetImportVM.Target.Name;
            }

            TargetProxy.Proxy.Coordinates = TargetImportVM.Target.Coordinates;
            RaisePropertyChanged(nameof(TargetProxy.Proxy));
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        public ICommand SendCoordinatesToFramingAssistantCommand { get; private set; }
        public ICommand ShowTargetImportViewCommand { get; private set; }

        public ICommand AddExposurePlanCommand { get; private set; }
        public ICommand DeleteExposurePlanCommand { get; private set; }

        private void Edit(object obj) {
            TargetProxy.PropertyChanged += TargetProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowEditView = true;
            ItemEdited = false;
        }

        private void ShowTargetImportViewCmd(object obj) {
            ShowTargetImportView = !ShowTargetImportView;
        }

        private void Save(object obj) {
            TargetProxy.Proxy.ExposurePlans = ExposurePlans;
            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();
            InitializeExposurePlans(TargetProxy.Proxy);
            TargetProxy.PropertyChanged -= TargetProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            ShowTargetImportView = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            TargetProxy.OnCancel();
            TargetProxy.PropertyChanged -= TargetProxy_PropertyChanged;
            InitializeExposurePlans(TargetProxy.Proxy);
            ShowEditView = false;
            ItemEdited = false;
            ShowTargetImportView = false;
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

        private void AddExposurePlan(object obj) {
            string filterName = GetFilterNamesForProfile().FirstOrDefault();
            if (filterName == null) {
                filterName = "unknown";
            }

            Target proxy = TargetProxy.Proxy;
            ExposurePlan exposurePlan = new ExposurePlan(profile.Id.ToString(), filterName);
            exposurePlan.TargetId = proxy.Id;
            exposurePlan.Gain = -1;
            exposurePlan.Offset = -1;
            exposurePlan.ReadoutMode = -1;

            proxy.ExposurePlans.Add(exposurePlan);
            InitializeExposurePlans(proxy);
        }

        private void DeleteExposurePlan(object obj) {
            ExposurePlan item = obj as ExposurePlan;
            ExposurePlan exposurePlan = TargetProxy.Original.ExposurePlans.Where(p => p.Id == item.Id).FirstOrDefault();
            if (exposurePlan != null) {
                string message = $"Delete exposure plan for '{exposurePlan.FilterName}' filter?  This cannot be undone.";
                ConfirmationMessageBox messageBox = new ConfirmationMessageBox(message, "Delete");
                if (messageBox.Show()) {
                    Target updatedTarget = managerVM.DeleteExposurePlan(TargetProxy.Original, exposurePlan);
                    if (updatedTarget != null) {
                        TargetProxy = new TargetProxy(updatedTarget);
                        InitializeExposurePlans(TargetProxy.Proxy);
                    }
                }
            }
            else {
                Logger.Error($"Assistant: failed to find original exposure plan: {item.Id}");
            }
        }

    }
}
