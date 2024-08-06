using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Platesolving;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using Scheduler.SyncService;
using System;
using System.Collections.Generic;
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

        [JsonProperty] public InstructionContainer BeforeWaitContainer { get; set; }
        [JsonProperty] public InstructionContainer AfterWaitContainer { get; set; }
        [JsonProperty] public InstructionContainer BeforeNewTargetContainer { get; set; }
        [JsonProperty] public InstructionContainer AfterNewTargetContainer { get; set; }
        [JsonProperty] public InstructionContainer AfterEachTargetContainer { get; set; }

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

            BeforeWaitContainer = new InstructionContainer(profileService, EventContainerType.BeforeWait, Parent);
            AfterWaitContainer = new InstructionContainer(profileService, EventContainerType.AfterWait, Parent);
            BeforeNewTargetContainer = new InstructionContainer(profileService, EventContainerType.BeforeNewTarget, Parent);
            AfterNewTargetContainer = new InstructionContainer(profileService, EventContainerType.AfterNewTarget, Parent);
            AfterEachTargetContainer = new InstructionContainer(profileService, EventContainerType.AfterEachTarget, Parent);
        }

        public TargetSchedulerSyncContainer(TargetSchedulerSyncContainer clone) : this(
            clone.profileService,
            clone.telescopeMediator,
            clone.rotatorMediator,
            clone.cameraMediator,
            clone.imagingMediator,
            clone.imageSaveMediator,
            clone.imageHistoryVM,
            clone.filterWheelMediator,
            clone.guiderMediator,
            clone.plateSolverFactory,
            clone.windowServiceFactory) {
            CopyMetaData(clone);

            clone.BeforeWaitContainer = (InstructionContainer)BeforeWaitContainer.Clone();
            clone.AfterWaitContainer = (InstructionContainer)AfterWaitContainer.Clone();
            clone.BeforeNewTargetContainer = (InstructionContainer)BeforeNewTargetContainer.Clone();
            clone.AfterNewTargetContainer = (InstructionContainer)AfterNewTargetContainer.Clone();
            clone.AfterEachTargetContainer = (InstructionContainer)AfterEachTargetContainer.Clone();

            clone.BeforeWaitContainer.AttachNewParent(clone);
            clone.AfterWaitContainer.AttachNewParent(clone);
            clone.BeforeNewTargetContainer.AttachNewParent(clone);
            clone.AfterNewTargetContainer.AttachNewParent(clone);
            clone.AfterEachTargetContainer.AttachNewParent(clone);
        }

        public override object Clone() {
            return new TargetSchedulerSyncContainer(this);
        }

        public override void Initialize() {
            TSLogger.Debug("TargetSchedulerSyncContainer: Initialize");
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);

                BeforeWaitContainer.Initialize();
                AfterWaitContainer.Initialize();
                BeforeNewTargetContainer.Initialize();
                AfterNewTargetContainer.Initialize();
                AfterEachTargetContainer.Initialize();
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                BeforeWaitContainer.AttachNewParent(Parent);
                AfterWaitContainer.AttachNewParent(Parent);
                BeforeNewTargetContainer.AttachNewParent(Parent);
                AfterNewTargetContainer.AttachNewParent(Parent);
                AfterEachTargetContainer.AttachNewParent(Parent);

                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
        }

        public override void ResetProgress() {
            TSLogger.Debug("TargetSchedulerSyncContainer: ResetProgress");
            // TODO: do we really want to do this here??  Better in SequenceBlockFinished?
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);

                BeforeWaitContainer.ResetProgress();
                AfterWaitContainer.ResetProgress();
                BeforeNewTargetContainer.ResetProgress();
                AfterNewTargetContainer.ResetProgress();
                AfterEachTargetContainer.ResetProgress();
            }
        }

        public override void SequenceBlockInitialize() {
            TSLogger.Debug("TargetSchedulerSyncContainer: SequenceBlockInitialize");
        }

        public override void SequenceBlockStarted() {
            TSLogger.Debug("TargetSchedulerSyncContainer: SequenceBlockStarted");
        }

        public override void SequenceBlockFinished() {
            TSLogger.Debug("TargetSchedulerSyncContainer: SequenceBlockFinished");
        }

        public override void SequenceBlockTeardown() {
            TSLogger.Debug("TargetSchedulerSyncContainer: SequenceBlockTeardown");
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

                    if (syncedAction is SyncedEventContainer) {
                        SyncedEventContainer syncedEventContainer = syncedAction as SyncedEventContainer;
                        DisplayText = $"Executing event container {syncedEventContainer.EventContainerType}";
                        TSLogger.Info($"SYNC client received event container: {syncedEventContainer.EventContainerType}");
                        await DoEventContainer(syncedEventContainer, progress, token);
                    }
                } catch (Exception ex) {
                    if (Utils.IsCancelException(ex)) {
                        TSLogger.Warning("TargetSchedulerSyncContainer was canceled or interrupted, execution is incomplete");
                        syncImageSaveWatcher.Stop();
                        Status = SequenceEntityStatus.CREATED;
                        token.ThrowIfCancellationRequested();
                        return;
                    } else {
                        TSLogger.Error($"TargetSchedulerSyncContainer exception (will continue): {ex}");
                    }
                }
            }

            DisplayText = "";
            syncImageSaveWatcher.Stop();
        }

        public override bool Validate() {
            var issues = new List<string>();

            bool beforeWaitValid = BeforeWaitContainer.Validate();
            bool afterWaitValid = AfterWaitContainer.Validate();
            bool beforeNewTargetValid = BeforeNewTargetContainer.Validate();
            bool afterNewTargetValid = AfterNewTargetContainer.Validate();
            bool afterEachTargetValid = AfterEachTargetContainer.Validate();

            if (!beforeWaitValid || !afterWaitValid || !beforeNewTargetValid || !afterNewTargetValid || !afterEachTargetValid) {
                issues.Add("One or more custom containers is not valid");
            }

            Issues = issues;
            return issues.Count == 0;
        }

        [OnSerializing]
        public void OnSerializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
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

        public void UpdateDisplayTextAction(string text) {
            DisplayText = text;
        }

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
            } else {
                TSLogger.Info($"SYNC client starting solve/rotate, id={syncedSolveRotate.SolveRotateId}");
                SolveAndRotate solveAndRotate = new SolveAndRotate(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, plateSolverFactory, windowServiceFactory);
                solveAndRotate.PositionAngle = syncedSolveRotate.TargetPositionAngle;
                await solveAndRotate.Execute(progress, token);
                TSLogger.Info($"SYNC client completed solve/rotate, id={syncedSolveRotate.SolveRotateId}");
            }

            await SyncClient.Instance.CompleteSolveRotate(syncedSolveRotate.SolveRotateId);
        }

        private async Task DoEventContainer(SyncedEventContainer syncedEventContainer, IProgress<ApplicationStatus> progress, CancellationToken token) {
            InstructionContainer targetContainer = null;
            switch (syncedEventContainer.EventContainerType) {
                case EventContainerType.BeforeWait: { targetContainer = BeforeWaitContainer; break; }
                case EventContainerType.AfterWait: { targetContainer = AfterWaitContainer; break; }
                case EventContainerType.BeforeNewTarget: { targetContainer = BeforeNewTargetContainer; break; }
                case EventContainerType.AfterNewTarget: { targetContainer = AfterNewTargetContainer; break; }
                case EventContainerType.AfterEachTarget: { targetContainer = AfterEachTargetContainer; break; }
            }

            await ExecuteEventContainer(targetContainer, progress, token);
            await SyncClient.Instance.CompleteEventContainer(syncedEventContainer.EventContainerId, syncedEventContainer.EventContainerType);
        }

        private async Task ExecuteEventContainer(InstructionContainer container, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (container.Items?.Count == 0) {
                TSLogger.Info($"SYNC client event container is empty: {container.Name}");
            }

            TSLogger.Info($"SYNC client starting event container: {container.Name}");

            try {
                await container.Execute(progress, token);
            } catch (Exception ex) {
                TSLogger.Error($"exception executing {container.Name} instruction container: {ex}");

                if (ex is SequenceEntityFailedException) {
                    throw;
                }

                throw new SequenceEntityFailedException($"exception executing {container.Name} instruction container: {ex.Message}", ex);
            } finally {
                TSLogger.Info($"SYNC client completed event container: {container.Name}, resetting progress for next execution");
                container.ResetAll();
            }
        }
    }
}