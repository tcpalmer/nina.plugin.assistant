using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TargetViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private IProfile profile;
        private string profileId;
        public List<ExposureTemplate> exposureTemplates;

        public TargetViewVM(AssistantManagerVM managerVM,
            IProfileService profileService,
            IApplicationMediator applicationMediator,
            IFramingAssistantVM framingAssistantVM,
            IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
            IPlanetariumFactory planetariumFactory,
            Target target) : base(profileService) {

            this.managerVM = managerVM;
            profileId = target.Project.ProfileId;
            TargetProxy = new TargetProxy(target);
            TargetActive = TargetProxy.Target.ActiveWithActiveExposurePlans;

            profile = managerVM.GetProfile(target.Project.ProfileId);
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            InitializeExposurePlans(TargetProxy.Proxy);
            InitializeExposureTemplateList(profile);

            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            CopyCommand = new RelayCommand(Copy);
            DeleteCommand = new RelayCommand(Delete);
            RefreshCommand = new RelayCommand(Refresh);

            ShowTargetImportViewCommand = new RelayCommand(ShowTargetImportViewCmd);
            AddExposurePlanCommand = new RelayCommand(AddExposurePlan);
            CopyExposurePlansCommand = new RelayCommand(CopyExposurePlans);
            PasteExposurePlansCommand = new RelayCommand(PasteExposurePlans);
            DeleteExposurePlanCommand = new RelayCommand(DeleteExposurePlan);

            SendCoordinatesToFramingAssistantCommand = new AsyncCommand<bool>(async () => {
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                // Note that IFramingAssistantVM doesn't expose any properties to set the rotation, although they are on the impl
                return await framingAssistantVM.SetCoordinates(TargetDSO);
            });

            TargetImportVM = new TargetImportVM(deepSkyObjectSearchVM, framingAssistantVM, planetariumFactory);
            TargetImportVM.PropertyChanged += ImportTarget_PropertyChanged;
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
                TargetActive = TargetProxy.Target.ActiveWithActiveExposurePlans;
                RaisePropertyChanged(nameof(TargetProxy));
            }
        }

        private bool targetActive;
        public bool TargetActive {
            get {
                return targetActive;
            }
            set {
                targetActive = value;
                RaisePropertyChanged(nameof(TargetActive));
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

        private void ProfileService_ProfileChanged(object sender, System.EventArgs e) {
            InitializeExposureTemplateList(profile);
        }

        private void InitializeExposureTemplateList(IProfile profile) {
            exposureTemplates = managerVM.GetExposureTemplates(profile);
            ExposureTemplateChoices = new AsyncObservableCollection<KeyValuePair<int, string>>();
            exposureTemplates.ForEach(et => {
                ExposureTemplateChoices.Add(new KeyValuePair<int, string>(et.Id, et.Name));
            });

            RaisePropertyChanged(nameof(ExposureTemplateChoices));
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> exposureTemplateChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> ExposureTemplateChoices {
            get {
                return exposureTemplateChoices;
            }
            set {
                exposureTemplateChoices = value;
            }
        }

        private bool showEditView = false;
        public bool ShowEditView {
            get => showEditView;
            set {
                showEditView = value;
                RaisePropertyChanged(nameof(ShowEditView));
                RaisePropertyChanged(nameof(ExposurePlansCopyEnabled));
                RaisePropertyChanged(nameof(ExposurePlansPasteEnabled));
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

        public bool ExposurePlansCopyEnabled {
            get => !ShowEditView && TargetProxy.Original.ExposurePlans?.Count > 0;
        }

        public bool ExposurePlansPasteEnabled {
            get => !ShowEditView && ExposurePlansClipboard.HasCopyItem();
        }

        private TargetImportVM targetImportVM;
        public TargetImportVM TargetImportVM { get => targetImportVM; set => targetImportVM = value; }

        private void ImportTarget_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (TargetImportVM.Target.Name != null) {
                TargetProxy.Proxy.Name = TargetImportVM.Target.Name;
            }

            TargetProxy.Proxy.Coordinates = TargetImportVM.Target.Coordinates;
            TargetProxy.Proxy.Rotation = TargetImportVM.Target.Rotation;
            RaisePropertyChanged(nameof(TargetProxy.Proxy));
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand CopyCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public ICommand SendCoordinatesToFramingAssistantCommand { get; private set; }
        public ICommand ShowTargetImportViewCommand { get; private set; }

        public ICommand AddExposurePlanCommand { get; private set; }
        public ICommand CopyExposurePlansCommand { get; private set; }
        public ICommand PasteExposurePlansCommand { get; private set; }
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
            if (MyMessageBox.Show(message, "Delete Target?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                managerVM.DeleteTarget(TargetProxy.Proxy);
            }
        }

        private void Refresh(object obj) {
            Target target = managerVM.ReloadTarget(TargetProxy.Proxy);
            if (target != null) {
                TargetProxy = new TargetProxy(target);
                TargetActive = TargetProxy.Target.ActiveWithActiveExposurePlans;
                InitializeExposurePlans(TargetProxy.Proxy);
            }
        }

        private ExposureTemplate GetDefaultExposureTemplate() {
            ExposureTemplate exposureTemplate = managerVM.GetDefaultExposureTemplate(profile);
            if (exposureTemplate == null) {
                MyMessageBox.Show("Can't find a default Exposure Template.  You must create some Exposure Templates for this profile before creating an Exposure Plan.", "Oops");
                return null;
            }

            return exposureTemplate;
        }

        private void AddExposurePlan(object obj) {
            ExposureTemplate exposureTemplate = GetDefaultExposureTemplate();
            if (exposureTemplate == null) {
                return;
            }

            Target proxy = TargetProxy.Proxy;
            ExposurePlan exposurePlan = new ExposurePlan(profile.Id.ToString());
            exposurePlan.ExposureTemplate = exposureTemplate;
            exposurePlan.ExposureTemplateId = exposureTemplate.Id;
            exposurePlan.TargetId = proxy.Id;

            proxy.ExposurePlans.Add(exposurePlan);
            InitializeExposurePlans(proxy);
            ItemEdited = true;
        }

        private void CopyExposurePlans(object obj) {
            if (ExposurePlans?.Count > 0) {
                List<ExposurePlan> exposurePlans = new List<ExposurePlan>(ExposurePlans.Count);
                foreach (ExposurePlan item in ExposurePlans) {
                    exposurePlans.Add(item);
                }

                ExposurePlansClipboard.SetItem(exposurePlans);
                RaisePropertyChanged(nameof(ExposurePlansPasteEnabled));
            }
        }

        private void PasteExposurePlans(object obj) {
            List<ExposurePlan> copiedExposurePlans = ExposurePlansClipboard.GetItem();
            if (copiedExposurePlans?.Count == 0) {
                return;
            }

            ExposureTemplate exposureTemplate = null;
            if (copiedExposurePlans[0].ExposureTemplate.ProfileId != profileId) {
                MyMessageBox.Show("The copied Exposure Plans reference Exposure Templates from a different profile.  They will be defaulted to the default (first) Exposure Template for this profile.");
                exposureTemplate = GetDefaultExposureTemplate();
                if (exposureTemplate == null) {
                    return;
                }
            }

            foreach (ExposurePlan copy in copiedExposurePlans) {
                ExposurePlan ep = copy.GetPasteCopy(profileId);
                ep.TargetId = TargetProxy.Original.Id;

                if (exposureTemplate != null) {
                    ep.ExposureTemplateId = exposureTemplate.Id;
                    ep.ExposureTemplate = exposureTemplate;
                }

                ExposurePlans.Add(ep);
            }

            TargetProxy.Proxy.ExposurePlans = ExposurePlans;
            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();
            InitializeExposurePlans(TargetProxy.Proxy);
            RaisePropertyChanged(nameof(ExposurePlans));
            TargetActive = TargetProxy.Target.ActiveWithActiveExposurePlans;
        }

        private void DeleteExposurePlan(object obj) {
            ExposurePlan item = obj as ExposurePlan;
            ExposurePlan exposurePlan = TargetProxy.Original.ExposurePlans.Where(ep => ep.Id == item.Id).FirstOrDefault();
            if (exposurePlan != null) {
                string message = $"Delete exposure plan using template '{exposurePlan.ExposureTemplate?.Name}'?  This cannot be undone.";
                if (MyMessageBox.Show(message, "Delete Exposure Plan?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    Target updatedTarget = managerVM.DeleteExposurePlan(TargetProxy.Original, exposurePlan);
                    if (updatedTarget != null) {
                        TargetProxy = new TargetProxy(updatedTarget);
                        InitializeExposurePlans(TargetProxy.Proxy);
                    }
                }
            }
            else {
                TSLogger.Error($"failed to find original exposure plan: {item.Id}");
            }
        }

    }
}
