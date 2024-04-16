using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using NINA.Astrometry;
using NINA.Core.Enum;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TargetViewVM : BaseVM {
        private AssistantManagerVM managerVM;
        private Project project;
        private IProfile profile;
        private string profileId;
        private ExposureCompletionHelper exposureCompletionHelper;
        public List<ExposureTemplate> exposureTemplates;

        public TargetViewVM(AssistantManagerVM managerVM,
            IProfileService profileService,
            IApplicationMediator applicationMediator,
            IFramingAssistantVM framingAssistantVM,
            IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
            IPlanetariumFactory planetariumFactory,
            Target target,
            Project project) : base(profileService) {
            this.managerVM = managerVM;
            this.project = project;
            exposureCompletionHelper = GetExposureCompletionHelper(project);

            profileId = project.ProfileId;
            TargetProxy = new TargetProxy(target);
            TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);

            profile = managerVM.GetProfile(profileId);
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
            DeleteExposurePlansCommand = new RelayCommand(DeleteAllExposurePlans);
            DeleteExposurePlanCommand = new RelayCommand(DeleteExposurePlan);

            OverrideExposureOrderCommand = new RelayCommand(DisplayOverrideExposureOrder);
            CancelOverrideExposureOrderCommand = new RelayCommand(CancelOverrideExposureOrder);

            if (target.OverrideExposureOrder != null) {
                OverrideExposureOrder = new OverrideExposureOrder(target.OverrideExposureOrder, target.ExposurePlans);
            }

            OverrideExposureOrderVM = new OverrideExposureOrderViewVM(this, profileService);

            SendCoordinatesToFramingAssistantCommand = new AsyncCommand<bool>(async () => {
                applicationMediator.ChangeTab(ApplicationTab.FRAMINGASSISTANT);
                // Note that IFramingAssistantVM doesn't expose any properties to set the rotation, although they are on the impl
                return await framingAssistantVM.SetCoordinates(TargetDSO);
            });

            TargetImportVM = new TargetImportVM(deepSkyObjectSearchVM, framingAssistantVM, planetariumFactory);
            TargetImportVM.PropertyChanged += ImportTarget_PropertyChanged;
        }

        private ExposureCompletionHelper GetExposureCompletionHelper(Project project) {
            ProfilePreference profilePreference = managerVM.Database.GetContext().GetProfilePreference(project.ProfileId, true);
            return new ExposureCompletionHelper(project.EnableGrader, profilePreference.ExposureThrottle);
        }

        private bool ActiveWithActiveExposurePlans(Target target) {
            return target.Project.ActiveNow && target.Enabled && target.ExposurePlans.Count > 0 && exposureCompletionHelper.PercentComplete(target) < 100;
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
            } else {
                TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);
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
                dso.RotationPositionAngle = target.Rotation;
                return dso;
            }
        }

        private void InitializeExposurePlans(Target target) {
            List<ExposurePlan> exposurePlans = new List<ExposurePlan>();

            target.ExposurePlans.ForEach((plan) => {
                plan.PercentComplete = exposureCompletionHelper.PercentComplete(plan);
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
                DefaultExposureOrder = GetDefaultExposureOrder();
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
                RaisePropertyChanged(nameof(ExposurePlansDeleteEnabled));
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

        public bool ExposurePlansDeleteEnabled {
            get => !ShowEditView && TargetProxy.Original.ExposurePlans?.Count > 0;
        }

        private TargetImportVM targetImportVM;
        public TargetImportVM TargetImportVM { get => targetImportVM; set => targetImportVM = value; }

        private void ImportTarget_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!ShowEditView) {
                return;
            }

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
        public ICommand DeleteExposurePlansCommand { get; private set; }
        public ICommand DeleteExposurePlanCommand { get; private set; }

        public ICommand OverrideExposureOrderCommand { get; private set; }
        public ICommand CancelOverrideExposureOrderCommand { get; private set; }

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

            // If exposure plans have been added or removed, we have to clear any override exposure order
            if (TargetProxy.Proxy.ExposurePlans.Count != TargetProxy.Original.ExposurePlans.Count) {
                OverrideExposureOrder = null;
                TargetProxy.Proxy.OverrideExposureOrder = null;
            }

            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();
            InitializeExposurePlans(TargetProxy.Proxy);
            TargetProxy.PropertyChanged -= TargetProxy_PropertyChanged;
            ShowEditView = false;
            ItemEdited = false;
            ShowTargetImportView = false;

            if (TargetProxy.Original.OverrideExposureOrder != null) {
                OverrideExposureOrder = new OverrideExposureOrder(TargetProxy.Original.OverrideExposureOrder, ExposurePlans);
                OverrideExposureOrderDisplay = GetOverrideExposureOrder();
            } else {
                DefaultExposureOrder = GetDefaultExposureOrder();
            }

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
            bool deleteAcquiredImagesWithTarget = managerVM.GetProfilePreference(profileId).EnableDeleteAcquiredImagesWithTarget;
            string message = deleteAcquiredImagesWithTarget
                ? $"Delete target '{TargetProxy.Target.Name}' and all associated acquired image records?  This cannot be undone."
                : $"Delete target '{TargetProxy.Target.Name}'?  This cannot be undone.";
            if (MyMessageBox.Show(message, "Delete Target?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                managerVM.DeleteTarget(TargetProxy.Proxy, deleteAcquiredImagesWithTarget);
            }
        }

        private void Refresh(object obj) {
            Target target = managerVM.ReloadTarget(TargetProxy.Proxy);
            if (target != null) {
                TargetProxy = new TargetProxy(target);
                TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);
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

                string overrideExposureOrder = OverrideExposureOrder == null ? null : OverrideExposureOrder.Serialize();
                ExposurePlansClipboard.SetItem(exposurePlans, overrideExposureOrder);
                RaisePropertyChanged(nameof(ExposurePlansPasteEnabled));
            } else {
                ExposurePlansClipboard.Clear();
                RaisePropertyChanged(nameof(ExposurePlansPasteEnabled));
            }
        }

        private void PasteExposurePlans(object obj) {
            ExposurePlansSpec source = ExposurePlansClipboard.GetItem();
            List<ExposurePlan> srcExposurePlans = source.ExposurePlans;

            if (srcExposurePlans?.Count == 0) {
                return;
            }

            // If existing target already has one or more exposure plans, we won't paste an override order
            string srcOverrideExposureOrder = new string(source.OverrideExposureOrder);
            if (ExposurePlans.Count > 0 && srcOverrideExposureOrder != null) {
                srcOverrideExposureOrder = null;
            }

            ExposureTemplate exposureTemplate = null;
            if (srcExposurePlans[0].ExposureTemplate.ProfileId != profileId) {
                MyMessageBox.Show("The copied Exposure Plans reference Exposure Templates from a different profile.  They will be defaulted to the default (first) Exposure Template for this profile.");
                exposureTemplate = GetDefaultExposureTemplate();
                if (exposureTemplate == null) {
                    return;
                }

                srcOverrideExposureOrder = null;
            }

            foreach (ExposurePlan copy in srcExposurePlans) {
                ExposurePlan ep = copy.GetPasteCopy(profileId);
                ep.TargetId = TargetProxy.Original.Id;

                if (exposureTemplate != null) {
                    ep.ExposureTemplateId = exposureTemplate.Id;
                    ep.ExposureTemplate = exposureTemplate;
                }

                ExposurePlans.Add(ep);
            }

            TargetProxy.Proxy.ExposurePlans = ExposurePlans;
            TargetProxy.Proxy.OverrideExposureOrder = srcOverrideExposureOrder;

            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();
            InitializeExposurePlans(TargetProxy.Proxy);

            // If the copy had an override exposure order, we need to remap it for the new exposure plan records
            if (srcOverrideExposureOrder != null) {
                TargetProxy.Proxy.OverrideExposureOrder = OverrideExposureOrder.Remap(srcOverrideExposureOrder, srcExposurePlans, TargetProxy.Proxy.ExposurePlans);
                managerVM.SaveTarget(TargetProxy.Proxy);
                TargetProxy.OnSave();
                OverrideExposureOrder = new OverrideExposureOrder(TargetProxy.Proxy.OverrideExposureOrder, TargetProxy.Proxy.ExposurePlans);
            } else {
                OverrideExposureOrder = null;
            }

            RaisePropertyChanged(nameof(ExposurePlans));
            RaisePropertyChanged(nameof(ExposurePlansCopyEnabled));
            RaisePropertyChanged(nameof(ExposurePlansDeleteEnabled));

            TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);
        }

        private void DeleteAllExposurePlans(object obj) {
            if (TargetProxy.Original.ExposurePlans?.Count > 0) {
                string message = "Delete all exposure plans for this target?  This cannot be undone.";
                if (MyMessageBox.Show(message, "Delete all Exposure Plans?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    // Have to clear any override exposure order on deleted exposure plans
                    TargetProxy.Original.OverrideExposureOrder = null;

                    Target updatedTarget = managerVM.DeleteAllExposurePlans(TargetProxy.Original);
                    if (updatedTarget != null) {
                        TargetProxy = new TargetProxy(updatedTarget);
                        InitializeExposurePlans(TargetProxy.Proxy);
                        RaisePropertyChanged(nameof(ExposurePlansCopyEnabled));
                        RaisePropertyChanged(nameof(ExposurePlansDeleteEnabled));
                        TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);

                        OverrideExposureOrder = null;
                        DefaultExposureOrder = GetDefaultExposureOrder();
                    }
                }
            }
        }

        private void DeleteExposurePlan(object obj) {
            ExposurePlan item = obj as ExposurePlan;
            ExposurePlan exposurePlan = TargetProxy.Original.ExposurePlans.Where(ep => ep.Id == item.Id).FirstOrDefault();
            if (exposurePlan != null) {
                string message = $"Delete exposure plan using template '{exposurePlan.ExposureTemplate?.Name}'?  This cannot be undone.";
                if (MyMessageBox.Show(message, "Delete Exposure Plan?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    // Have to clear any override exposure order on deleted exposure plan
                    TargetProxy.Original.OverrideExposureOrder = null;

                    Target updatedTarget = managerVM.DeleteExposurePlan(TargetProxy.Original, exposurePlan);
                    if (updatedTarget != null) {
                        TargetProxy = new TargetProxy(updatedTarget);
                        InitializeExposurePlans(TargetProxy.Proxy);
                        RaisePropertyChanged(nameof(ExposurePlansDeleteEnabled));
                        TargetActive = ActiveWithActiveExposurePlans(TargetProxy.Target);

                        OverrideExposureOrder = null;
                        DefaultExposureOrder = GetDefaultExposureOrder();
                    }
                }
            } else {
                TSLogger.Error($"failed to find original exposure plan: {item.Id}");
            }
        }

        private string defaultExposureOrder;

        public string DefaultExposureOrder {
            get {
                return defaultExposureOrder;
            }
            set {
                defaultExposureOrder = value;
                RaisePropertyChanged(nameof(DefaultExposureOrder));
            }
        }

        private string overrideExposureOrderDisplay;

        public string OverrideExposureOrderDisplay {
            get {
                return overrideExposureOrderDisplay;
            }
            set {
                overrideExposureOrderDisplay = value;
                RaisePropertyChanged(nameof(OverrideExposureOrderDisplay));
            }
        }

        private bool showOverrideExposureOrderPopup = false;

        public bool ShowOverrideExposureOrderPopup {
            get => showOverrideExposureOrderPopup;
            set {
                showOverrideExposureOrderPopup = value;
                RaisePropertyChanged(nameof(ShowOverrideExposureOrderPopup));
            }
        }

        private OverrideExposureOrderViewVM overrideExposureOrderVM;

        public OverrideExposureOrderViewVM OverrideExposureOrderVM {
            get => overrideExposureOrderVM;
            set {
                overrideExposureOrderVM = value;
                RaisePropertyChanged(nameof(OverrideExposureOrderVM));
            }
        }

        private OverrideExposureOrder overrideExposureOrder;

        public OverrideExposureOrder OverrideExposureOrder {
            get => overrideExposureOrder;
            set {
                overrideExposureOrder = value;
                OverrideExposureOrderDisplay = GetOverrideExposureOrder();
                RaisePropertyChanged(nameof(OverrideExposureOrder));
                RaisePropertyChanged(nameof(HaveOverrideExposureOrder));
            }
        }

        public bool HaveOverrideExposureOrder { get => OverrideExposureOrder != null; private set { } }

        private string GetDefaultExposureOrder() {
            StringBuilder sb = new StringBuilder();
            List<string> exposureInstructions = new List<string>();

            int filterSwitchFrequency = project.FilterSwitchFrequency;
            int ditherEvery = project.DitherEvery;

            ExposurePlans.ForEach((plan) => {
                if (filterSwitchFrequency == 0) {
                    sb.Append(plan.ExposureTemplate.Name).Append("..., ");
                } else {
                    for (int i = 0; i < filterSwitchFrequency; i++) {
                        sb.Append(plan.ExposureTemplate.Name).Append(", ");
                        exposureInstructions.Add(plan.ExposureTemplate.Name);
                    }
                }
            });

            if (filterSwitchFrequency == 0 || ditherEvery == 0) {
                return sb.ToString().TrimEnd().TrimEnd(new Char[] { ',' });
            }

            List<string> dithered = new DitherInjector(exposureInstructions, ditherEvery).ExposureOrderInject();
            StringBuilder sb2 = new StringBuilder();
            foreach (string item in dithered) {
                sb2.Append(item).Append(", ");
            }

            return sb2.ToString().TrimEnd().TrimEnd(new Char[] { ',' });
        }

        private string GetOverrideExposureOrder() {
            if (OverrideExposureOrder == null || OverrideExposureOrder.OverrideItems.Count == 0) {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var item in OverrideExposureOrder.OverrideItems) {
                sb.Append(item.Name).Append(", ");
            }

            return sb.ToString().TrimEnd().TrimEnd(new Char[] { ',' });
        }

        private void DisplayOverrideExposureOrder(object obj) {
            if (OverrideExposureOrder == null) {
                OverrideExposureOrderVM.OverrideExposureOrder = new OverrideExposureOrder(TargetProxy.Proxy.ExposurePlans);
            } else {
                // Clone it for popup
                OverrideExposureOrderVM.OverrideExposureOrder = new OverrideExposureOrder(OverrideExposureOrder.Serialize(), TargetProxy.Proxy.ExposurePlans);
            }

            ShowOverrideExposureOrderPopup = true;
        }

        private void CancelOverrideExposureOrder(object obj) {
            string message = $"Clear override exposure order?  This cannot be undone.";
            if (MyMessageBox.Show(message, "Clear?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                TargetProxy.Proxy.OverrideExposureOrder = null;
                managerVM.SaveTarget(TargetProxy.Proxy);
                TargetProxy.OnSave();

                OverrideExposureOrder = null;
            }
        }

        public void SaveOverrideExposureOrder(OverrideExposureOrder overrideExposureOrder) {
            TargetProxy.Proxy.OverrideExposureOrder = overrideExposureOrder.Serialize();
            managerVM.SaveTarget(TargetProxy.Proxy);
            TargetProxy.OnSave();

            OverrideExposureOrder = overrideExposureOrder;
        }
    }
}