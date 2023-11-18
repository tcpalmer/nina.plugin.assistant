using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using Scheduler.SyncService;
using System;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Assistant.NINAPlugin.Sequencer {
    [ExportMetadata("Name", "Target Scheduler Sync Container")]
    [ExportMetadata("Description", "Target Scheduler synchronized imaging for multiple NINA instances")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerSyncContainer : SequentialContainer {

        private readonly IProfileService profileService;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IRotatorMediator rotatorMediator;
        private readonly ICameraMediator cameraMediator;
        private readonly IImagingMediator imagingMediator;
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM imageHistoryVM;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IGuiderMediator guiderMediator;
        private readonly IPlateSolverFactory plateSolverFactory;
        private readonly IWindowServiceFactory windowServiceFactory;

        private ISyncImageSaveWatcher syncImageSaveWatcher;

        [ImportingConstructor]
        public TargetSchedulerSyncContainer(
            IProfileService profileService,
            ITelescopeMediator telescopeMediator,
            IRotatorMediator rotatorMediator,
            ICameraMediator cameraMediator,
            IImagingMediator imagingMediator,
            IImageSaveMediator imageSaveMediator,
            IImageHistoryVM imageHistoryVM,
            IFilterWheelMediator filterWheelMediator,
            IGuiderMediator guiderMediator,
            IPlateSolverFactory plateSolverFactory,
            IWindowServiceFactory windowServiceFactory) : base() {

            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            this.rotatorMediator = rotatorMediator;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.guiderMediator = guiderMediator;
            this.plateSolverFactory = plateSolverFactory;
            this.windowServiceFactory = windowServiceFactory;
        }

        public TargetSchedulerSyncContainer(TargetSchedulerSyncContainer cloneMe) : this(
            cloneMe.profileService,
            cloneMe.telescopeMediator,
            cloneMe.rotatorMediator,
            cloneMe.cameraMediator,
            cloneMe.imagingMediator,
            cloneMe.imageSaveMediator,
            cloneMe.imageHistoryVM,
            cloneMe.filterWheelMediator,
            cloneMe.guiderMediator,
            cloneMe.plateSolverFactory,
            cloneMe.windowServiceFactory) {
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

            //if (!Common.USE_EMULATOR) {
            syncImageSaveWatcher = new SyncImageSaveWatcher(profileService.ActiveProfile, imageSaveMediator);
            syncImageSaveWatcher.Start();
            //}

            while (true) {
                progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: requesting action from sync server" });
                try {
                    DisplayText = "Requesting action from sync server";
                    SyncedAction syncedAction = await SyncClient.Instance.StartRequestAction(token);

                    if (syncedAction == null) {
                        TSLogger.Info("TargetSchedulerSyncContainer complete, ending instruction");
                        DisplayText = "Completed";
                        progress?.Report(new ApplicationStatus() { Status = "" });
                        break;
                    }

                    if (syncedAction is SyncedExposure) {
                        SyncedExposure syncedExposure = syncedAction as SyncedExposure;
                        TSLogger.Info($"SYNC client received exposure: {syncedExposure.ExposureId} for {syncedExposure.TargetName}");
                        TakeSyncedExposure(syncedExposure, progress, token).Wait();
                    }

                    if (syncedAction is SyncedSolveRotate) {
                        SyncedSolveRotate syncedSolveRotate = syncedAction as SyncedSolveRotate;
                        DisplayText = $"Rotating to {syncedSolveRotate.TargetPositionAngle} and solving";
                        TSLogger.Info($"SYNC client received solve/rotate: {syncedSolveRotate.SolveRotateId} for {syncedSolveRotate.TargetName}");
                        await DoSyncedSolveRotate(syncedSolveRotate, progress, token);
                    }
                }
                catch (Exception ex) {
                    if (Utils.IsCancelException(ex)) {
                        TSLogger.Warning("TargetSchedulerSyncContainer instruction was canceled");
                        syncImageSaveWatcher.Stop();
                        return;
                    }
                    else {
                        TSLogger.Error($"TargetSchedulerSyncContainer exception (will continue): {ex}");
                    }
                }
            }

            DisplayText = "";
            syncImageSaveWatcher.Stop();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

        private string displayText;
        public string DisplayText {
            get => displayText;
            set {
                displayText = value;
                RaisePropertyChanged(nameof(DisplayText));
            }
        }

        public void UpdateDisplayTextAction(string text) { DisplayText = text; }

        private async Task TakeSyncedExposure(SyncedExposure syncedExposure, IProgress<ApplicationStatus> progress, CancellationToken token) {
            SyncTakeExposureContainer container = new SyncTakeExposureContainer(profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, filterWheelMediator, syncImageSaveWatcher, syncedExposure, UpdateDisplayTextAction);
            Application.Current.Dispatcher.Invoke(delegate {
                Items.Clear();
                Add(container);
            });

            await base.Execute(progress, token);
        }

        private async Task DoSyncedSolveRotate(SyncedSolveRotate syncedSolveRotate, IProgress<ApplicationStatus> progress, CancellationToken token) {

            if (!rotatorMediator.GetInfo().Connected) {
                TSLogger.Warning($"SYNC client received solve/rotate but no rotator is connected: skipping and continuing, id={syncedSolveRotate.SolveRotateId}");
                await Task.Delay(2500, token);
            }
            else {
                TSLogger.Info($"SYNC client starting solve/rotate, id={syncedSolveRotate.SolveRotateId}");
                SolveAndRotate solveAndRotate = new SolveAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, plateSolverFactory, windowServiceFactory);
                solveAndRotate.PositionAngle = syncedSolveRotate.TargetPositionAngle;
                await solveAndRotate.Execute(progress, token);
                TSLogger.Info($"SYNC client completed solve/rotate, id={syncedSolveRotate.SolveRotateId}");
            }

            await SyncClient.Instance.CompleteSolveRotate(syncedSolveRotate.SolveRotateId);
        }
    }
}
