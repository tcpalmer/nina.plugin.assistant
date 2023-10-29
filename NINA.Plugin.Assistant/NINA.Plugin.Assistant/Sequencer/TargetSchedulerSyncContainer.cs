using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using Scheduler.SyncService;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {
    [ExportMetadata("Name", "Target Scheduler Sync Container")]
    [ExportMetadata("Description", "Target Scheduler synchronized imaging for multiple NINA instances")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerSyncContainer : SequenceItem {

        private readonly IProfileService profileService;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM imageHistoryVM;
        private readonly IFilterWheelMediator filterWheelMediator;

        private ISyncImageSaveWatcher syncImageSaveWatcher;

        [ImportingConstructor]
        public TargetSchedulerSyncContainer(
            IProfileService profileService,
            IRotatorMediator rotatorMediator,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IFilterWheelMediator filterWheelMediator) : base() {
            this.profileService = profileService;
            this.rotatorMediator = rotatorMediator;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
        }

        public TargetSchedulerSyncContainer(TargetSchedulerSyncContainer cloneMe) : this(
            cloneMe.profileService,
            cloneMe.rotatorMediator,
            cloneMe.cameraMediator,
            cloneMe.imagingMediator,
            cloneMe.imageSaveMediator,
            cloneMe.imageHistoryVM,
            cloneMe.filterWheelMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TargetSchedulerSyncContainer(this);
        }

        public override void Initialize() {
            TSLogger.Debug("TargetSchedulerSyncContainer: Initialize");
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);
            }
        }

        public override void ResetProgress() {
            TSLogger.Debug("TargetSchedulerSyncContainer: ResetProgress");
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);
            }
        }

        public override void Teardown() {
            TSLogger.Debug("TargetSchedulerSyncContainer: Teardown");
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);
            }
            base.Teardown();
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (!SyncManager.Instance.IsRunning) {
                TSLogger.Info("TargetSchedulerSyncContainer execute but sync is not running");
                return;
            }

            if (SyncManager.Instance.IsServer) {
                Notification.ShowWarning("Target Scheduler Sync Container should only be used in a NINA client instance, but current instance is the server.");
                TSLogger.Info("TargetSchedulerSyncContainer execute but instance is server, not client as expected");
                return;
            }

            if (!Common.USE_EMULATOR) {
                syncImageSaveWatcher = new SyncImageSaveWatcher(profileService.ActiveProfile, imageSaveMediator);
                syncImageSaveWatcher.Start();
            }

            while (true) {
                progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: requesting exposure from server" });
                try {
                    SyncedExposure syncedExposure = await SyncClient.Instance.StartRequestExposure(token);
                    if (syncedExposure == null) {
                        TSLogger.Info("TargetSchedulerSyncContainer complete, ending instruction");
                        progress?.Report(new ApplicationStatus() { Status = "" });
                        break;
                    }

                    TSLogger.Info($"SYNC client received exposure: {syncedExposure.ExposureId}");
                    await TakeSyncedExposure(syncedExposure, progress, token);
                }
                catch (Exception ex) {
                    TSLogger.Error("TargetSchedulerSyncContainer exception", ex);
                }

                // Test something like an AF on the client
                //SyncClient.Instance.SetClientState(ClientState.Exposureready);
                //Utils.TestWait(130);
                ////
            }

            syncImageSaveWatcher.Stop();
        }

        private async Task TakeSyncedExposure(SyncedExposure syncedExposure, IProgress<ApplicationStatus> progress, CancellationToken token) {
            SyncTakeExposure takeExposure = new SyncTakeExposure(profileService, rotatorMediator, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, syncImageSaveWatcher, syncedExposure);
            await takeExposure.Execute(progress, token);
        }
    }
}
