using Assistant.NINAPlugin.Controls.AcquiredImages;
using Assistant.NINAPlugin.Controls.AssistantManager;
using Assistant.NINAPlugin.Controls.PlanPreview;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Plugin;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class AssistantPlugin : PluginBase, INotifyPropertyChanged {

        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;
        private IApplicationMediator applicationMediator;
        private IFramingAssistantVM framingAssistantVM;
        private IDeepSkyObjectSearchVM deepSkyObjectSearchVM;
        private IPlanetariumFactory planetariumFactory;

        // Plugin specific image file patterns
        public static readonly ImagePattern FlatSessionIdImagePattern = new ImagePattern("$$TSSESSIONID$$", "Session identifier for working with TS lights and flats", "Target Scheduler");

        [ImportingConstructor]
        public AssistantPlugin(IProfileService profileService,
            IOptionsVM options,
            IApplicationMediator applicationMediator,
            IFramingAssistantVM framingAssistantVM,
            IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
            IPlanetariumFactory planetariumFactory) {

            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.deepSkyObjectSearchVM = deepSkyObjectSearchVM;
            this.planetariumFactory = planetariumFactory;

            profileService.ProfileChanged += ProfileService_ProfileChanged;

            options.AddImagePattern(FlatSessionIdImagePattern);
        }

        public override async Task Initialize() {
            InitPluginHome();

            if (SyncEnabled(profileService)) {
                SyncManager.Instance.Start(profileService);
            }

            TSLogger.Info("plugin initialized");
        }

        private void InitPluginHome() {
            if (!Directory.Exists(Common.PLUGIN_HOME)) {
                Directory.CreateDirectory(Common.PLUGIN_HOME);
            }

            SchedulerDatabaseInteraction.BackupDatabase();
        }

        public static bool SyncEnabled(IProfileService profileService) {
            ProfilePreference profilePreference = new SchedulerPlanLoader(profileService.ActiveProfile).GetProfilePreferences();
            return profilePreference.EnableSynchronization;
        }

        private AssistantManagerVM assistantManagerVM;
        public AssistantManagerVM AssistantManagerVM {
            get => assistantManagerVM;
            set {
                assistantManagerVM = value;
                RaisePropertyChanged(nameof(AssistantManagerVM));
            }
        }

        private PlanPreviewerViewVM planPreviewerViewVM;
        public PlanPreviewerViewVM PlanPreviewerViewVM {
            get => planPreviewerViewVM;
            set {
                planPreviewerViewVM = value;
                RaisePropertyChanged(nameof(PlanPreviewerViewVM));
            }
        }

        private AcquiredImagesManagerViewVM acquiredImagesManagerViewVM;
        public AcquiredImagesManagerViewVM AcquiredImagesManagerViewVM {
            get => acquiredImagesManagerViewVM;
            set {
                acquiredImagesManagerViewVM = value;
                RaisePropertyChanged(nameof(AcquiredImagesManagerViewVM));
            }
        }

        private bool assistantManagerIsExpanded = false;
        public bool AssistantManagerIsExpanded {
            get { return assistantManagerIsExpanded; }
            set {
                assistantManagerIsExpanded = value;
                if (value && AssistantManagerVM == null) {
                    AssistantManagerVM = new AssistantManagerVM(profileService, applicationMediator, framingAssistantVM, deepSkyObjectSearchVM, planetariumFactory);
                }
            }
        }

        private bool planPreviewIsExpanded = false;
        public bool PlanPreviewIsExpanded {
            get { return planPreviewIsExpanded; }
            set {
                planPreviewIsExpanded = value;
                if (value && PlanPreviewerViewVM == null) {
                    PlanPreviewerViewVM = new PlanPreviewerViewVM(profileService);
                }
            }
        }

        private bool acquiredImagesManagerIsExpanded = false;
        public bool AcquiredImagesManagerIsExpanded {
            get { return acquiredImagesManagerIsExpanded; }
            set {
                acquiredImagesManagerIsExpanded = value;
                if (value && AcquiredImagesManagerViewVM == null) {
                    AcquiredImagesManagerViewVM = new AcquiredImagesManagerViewVM(profileService);
                }
            }
        }

        private void ProcessExited(object sender, EventArgs e) {
            TSLogger.Warning($"process exited");
        }

        public override Task Teardown() {

            if (SyncManager.Instance.IsRunning) {
                SyncManager.Instance.Shutdown();
            }

            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            TSLogger.Info("closing log");
            TSLogger.CloseAndFlush();
            return base.Teardown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            AssistantManagerVM = new AssistantManagerVM(profileService, applicationMediator, framingAssistantVM, deepSkyObjectSearchVM, planetariumFactory);
            PlanPreviewerViewVM = new PlanPreviewerViewVM(profileService);
            AcquiredImagesManagerViewVM = new AcquiredImagesManagerViewVM(profileService);

            RaisePropertyChanged(nameof(AssistantManagerVM));
            RaisePropertyChanged(nameof(PlanPreviewerViewVM));
            RaisePropertyChanged(nameof(AcquiredImagesManagerViewVM));

            if (profileService.ActiveProfile != null) {
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged += ProfileService_ProfileChanged;

                if (SyncManager.Instance.IsRunning) {
                    SyncManager.Instance.Shutdown();
                    if (SyncEnabled(profileService)) {
                        SyncManager.Instance.Start(profileService);
                    }
                }
            }
        }
    }

}