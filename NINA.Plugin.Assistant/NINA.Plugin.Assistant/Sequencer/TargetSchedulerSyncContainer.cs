using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Mediator;
using System;
using System.Collections.Generic;
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
        private readonly ICameraMediator cameraMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM imageHistoryVM;
        private readonly IFilterWheelMediator filterWheelMediator;

        [ImportingConstructor]
        public TargetSchedulerSyncContainer(
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IFilterWheelMediator filterWheelMediator) : base() {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
        }

        public TargetSchedulerSyncContainer(TargetSchedulerSyncContainer cloneMe) : this(
            cloneMe.profileService,
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

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (!SyncManager.Instance.IsRunning) {
                TSLogger.Info("TargetSchedulerSyncContainer execute but sync is not running");
                return;
            }

            if (SyncManager.Instance.IsServer) {
                TSLogger.Info("TargetSchedulerSyncContainer execute but instance is primary, not secondary as expected");
                return;
            }

            while (true) {
                progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: requesting exposure from primary" });
                try {
                    SyncedExposure syncedExposure = await SyncClient.Instance.StartRequestExposure(token);
                    if (syncedExposure == null) {
                        TSLogger.Info("TargetSchedulerSyncContainer complete, ending instruction");
                        progress?.Report(new ApplicationStatus() { Status = "" });
                        break;
                    }

                    await TakeSyncedExposure(syncedExposure, progress, token);
                }
                catch (Exception ex) {
                    TSLogger.Error("TargetSchedulerSyncContainer exception", ex);
                }
            }
        }

        private async Task TakeSyncedExposure(SyncedExposure syncedExposure, IProgress<ApplicationStatus> progress, CancellationToken token) {
            SyncTakeExposure takeExposure = new SyncTakeExposure(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, syncedExposure);
            await takeExposure.Execute(progress, token);
        }
    }
}
