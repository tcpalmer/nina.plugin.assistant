using Assistant.NINAPlugin.Controls.AssistantManager;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin {

    [Export(typeof(IPluginManifest))]
    public class AssistantPlugin : PluginBase, INotifyPropertyChanged {

        public static string PLUGIN_HOME = Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "AssistantPlugin");

        private IPluginOptionsAccessor pluginSettings;
        private IProfileService profileService;

        [ImportingConstructor]
        public AssistantPlugin(IProfileService profileService) {
            if (Properties.Settings.Default.UpdateSettings) {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Properties.Settings.Default);
            }

            this.pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(this.Identifier));
            this.profileService = profileService;
            profileService.ProfileChanged += ProfileService_ProfileChanged;

            InitPluginHome();

            AssistantManagerVM = new AssistantManagerVM(profileService);
            //StartDatabaseManagementApp();
        }

        private void InitPluginHome() {
            if (!Directory.Exists(PLUGIN_HOME)) {
                Directory.CreateDirectory(PLUGIN_HOME);
            }

            // TODO: backup database at the start of each NINA run, only keep 10 copies
        }

        private void StartDatabaseManagementApp() {
            // TODO: clean up, make like RemoteCopy
            // TODO: can get the active profile ID and pass as an argument
            Process process = new Process();
            process.StartInfo.FileName = "C:\\Users\\Tom\\source\\repos\\Names\\bin\\Debug\\net6.0-windows\\Names.exe";
            process.StartInfo.Arguments = "";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(ProcessExited);
            bool isRunning = false;

            try {
                isRunning = process.Start();
            }
            catch (Exception) {
                Logger.Error($"failed to start process: APP (check command and args)");
                Notification.ShowError($"Failed to start APP background process, check command and args");
                //runningProcessId = INACTIVE_PID;
                isRunning = false;
            }

            if (isRunning) {
                Logger.Info($"started process: pid={process.Id}");
                Notification.ShowSuccess($"APP background process started");
                //runningProcessId = process.Id;
                //StartWatchTimer();
            }
            else {
                Logger.Error($"failed to start process");
            }
        }

        private AssistantManagerVM assistantManagerVM;
        public AssistantManagerVM AssistantManagerVM { get => assistantManagerVM; set => assistantManagerVM = value; }

        private void ProcessExited(object sender, System.EventArgs e) {
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

            // TODO: fix
            //RaisePropertyChanged(nameof(PROPERTY_THAT_IS_SAVED_IN_PROFILE));

            if (profileService.ActiveProfile != null) {
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged -= ProfileService_ProfileChanged;
                profileService.ActiveProfile.AstrometrySettings.PropertyChanged += ProfileService_ProfileChanged;
            }
        }
    }

}