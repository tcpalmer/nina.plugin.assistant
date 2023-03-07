using Assistant.NINAPlugin.Controls.AcquiredImages;
using Assistant.NINAPlugin.Controls.AssistantManager;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Plugin;
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

        public static string PLUGIN_HOME = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "SchedulerPlugin");

        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;
        private IApplicationMediator applicationMediator;
        private IFramingAssistantVM framingAssistantVM;
        private IDeepSkyObjectSearchVM deepSkyObjectSearchVM;
        private IPlanetariumFactory planetariumFactory;

        [ImportingConstructor]
        public AssistantPlugin(IProfileService profileService,
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

            InitPluginHome();
        }

        private void InitPluginHome() {
            if (!Directory.Exists(PLUGIN_HOME)) {
                Directory.CreateDirectory(PLUGIN_HOME);
            }

            // TODO: backup database at the start of each NINA run, only keep 10 copies
        }

        private AssistantManagerVM assistantManagerVM;
        public AssistantManagerVM AssistantManagerVM {
            get => assistantManagerVM;
            set {
                assistantManagerVM = value;
                RaisePropertyChanged(nameof(AssistantManagerVM));
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
            Logger.Warning($"process exited");
        }

        public override Task Teardown() {
            profileService.ProfileChanged -= ProfileService_ProfileChanged;
            return base.Teardown();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            AssistantManagerVM = new AssistantManagerVM(profileService, applicationMediator, framingAssistantVM, deepSkyObjectSearchVM, planetariumFactory);
            AcquiredImagesManagerViewVM = new AcquiredImagesManagerViewVM(profileService);

            RaisePropertyChanged(nameof(AssistantManagerVM));
            RaisePropertyChanged(nameof(AcquiredImagesManagerViewVM));

            if (profileService.ActiveProfile != null) {
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged += ProfileService_ProfileChanged;
            }
        }
    }

}