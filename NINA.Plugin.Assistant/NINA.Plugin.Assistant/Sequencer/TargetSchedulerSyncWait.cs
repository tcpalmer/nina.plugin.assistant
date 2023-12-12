using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using Scheduler.SyncService;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Sync Wait")]
    [ExportMetadata("Description", "Target Scheduler synchronized waiting for multiple NINA instances")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerSyncWait : SequenceItem {

        private readonly IProfileService profileService;

        [ImportingConstructor]
        public TargetSchedulerSyncWait(IProfileService profileService) : base() {
            this.profileService = profileService;
        }

        public TargetSchedulerSyncWait(TargetSchedulerSyncWait cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TargetSchedulerSyncWait(this);
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            if (!AssistantPlugin.SyncEnabled(profileService)) {
                TSLogger.Info("TargetSchedulerSyncWait execute but sync is not enabled on profile");
                return;
            }

            if (!SyncManager.Instance.IsRunning) {
                TSLogger.Info("TargetSchedulerSyncWait execute but sync server not running");
                return;
            }

            TimeSpan timeout = TimeSpan.FromSeconds(GetSyncWaitTimeout());

            // SERVER
            if (SyncManager.Instance.IsServer) {
                try {
                    TSLogger.Info($"TargetSchedulerSyncWait: server waiting on all clients to report wait, timeout is {timeout.TotalSeconds}s");
                    progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for clients to enter sync wait" });

                    SyncServer.Instance.State = ServerState.Waiting;
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    while (!SyncServer.Instance.AllClientsInState(ClientState.Waiting) && NotTimedOut(stopwatch, timeout)) {
                        await Task.Delay(SyncManager.SERVER_WAIT_POLL_PERIOD, token);
                    }

                    TSLogger.Info("TargetSchedulerSyncWait: server detected all sync clients are waiting");
                    SyncServer.Instance.State = ServerState.WaitComplete;

                    // Allow time for updated client state to propagate
                    await Task.Delay(SyncManager.CLIENT_KEEPALIVE_PERIOD + 500, token);

                    while (!SyncServer.Instance.AllClientsInState(ClientState.Ready) && NotTimedOut(stopwatch, timeout)) {
                        await Task.Delay(SyncManager.SERVER_WAIT_POLL_PERIOD, token);
                    }

                    TSLogger.Info("TargetSchedulerSyncWait: server detected all sync clients are ready (or timed out waiting)");
                }
                catch (Exception e) {
                    if (e is TaskCanceledException) {
                        TSLogger.Warning("TargetSchedulerSyncWait: task was canceled");
                    }
                    else {
                        TSLogger.Error("TargetSchedulerSyncWait: server exception", e);
                    }
                }
                finally {
                    SyncServer.Instance.State = ServerState.Ready;
                    progress?.Report(new ApplicationStatus() { Status = "" });
                }
            }

            // CLIENT
            else {
                try {
                    TSLogger.Info("TargetSchedulerSyncWait: setting client to sync wait");
                    progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for server to sync" });
                    await SyncClient.Instance.StartSyncWait(token, timeout);

                    // Allow time for updated client state to propagate
                    await Task.Delay(SyncManager.CLIENT_KEEPALIVE_PERIOD + 500, token);
                }
                catch (Exception e) {
                    if (e is TaskCanceledException) {
                        TSLogger.Warning("TargetSchedulerSyncWait: task was canceled");
                    }
                    else {
                        TSLogger.Error("TargetSchedulerSyncWait: client exception", e);
                    }
                }
                finally {
                    progress?.Report(new ApplicationStatus() { Status = "" });
                }
            }
        }

        private int GetSyncWaitTimeout() {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                ProfilePreference profilePreference = context.GetProfilePreference(profileService.ActiveProfile.Id.ToString());
                return profilePreference != null ? profilePreference.SyncWaitTimeout : SyncManager.DEFAULT_SYNC_WAIT_TIMEOUT;
            }
        }

        private bool NotTimedOut(Stopwatch stopwatch, TimeSpan timeout) {
            if (stopwatch.Elapsed > timeout) {
                TSLogger.Warning($"TargetSchedulerSyncWait: server timed out after {timeout.TotalSeconds} seconds");
                return false;
            }

            return true;
        }
    }
}
