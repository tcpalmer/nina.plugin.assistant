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

        [JsonProperty] public InstructionContainer SyncBeforeWaitContainer { get; set; }
        [JsonProperty] public InstructionContainer SyncAfterWaitContainer { get; set; }
        [JsonProperty] public InstructionContainer SyncBeforeTargetContainer { get; set; }
        [JsonProperty] public InstructionContainer SyncAfterTargetContainer { get; set; }
        [JsonProperty] public InstructionContainer SyncAfterAllTargetsContainer { get; set; }

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

            SyncBeforeWaitContainer = new InstructionContainer(EventContainerType.BeforeWait, Parent);
            SyncAfterWaitContainer = new InstructionContainer(EventContainerType.AfterWait, Parent);
            SyncBeforeTargetContainer = new InstructionContainer(EventContainerType.BeforeTarget, Parent);
            SyncAfterTargetContainer = new InstructionContainer(EventContainerType.AfterTarget, Parent);
            SyncAfterAllTargetsContainer = new InstructionContainer(EventContainerType.AfterEachTarget, Parent);
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

            clone.SyncBeforeWaitContainer = (InstructionContainer)SyncBeforeWaitContainer.Clone();
            clone.SyncAfterWaitContainer = (InstructionContainer)SyncAfterWaitContainer.Clone();
            clone.SyncBeforeTargetContainer = (InstructionContainer)SyncBeforeTargetContainer.Clone();
            clone.SyncAfterTargetContainer = (InstructionContainer)SyncAfterTargetContainer.Clone();
            clone.SyncAfterAllTargetsContainer = (InstructionContainer)SyncAfterAllTargetsContainer.Clone();

            clone.SyncBeforeWaitContainer.AttachNewParent(clone);
            clone.SyncAfterWaitContainer.AttachNewParent(clone);
            clone.SyncBeforeTargetContainer.AttachNewParent(clone);
            clone.SyncAfterTargetContainer.AttachNewParent(clone);
            clone.SyncAfterAllTargetsContainer.AttachNewParent(clone);
        }

        public override object Clone() {
            return new TargetSchedulerSyncContainer(this);
        }

        public override void Initialize() {
            TSLogger.Debug("TargetSchedulerSyncContainer: Initialize");
            if (SyncManager.Instance.RunningClient) {
                SyncClient.Instance.SetClientState(ClientState.Ready);

                SyncBeforeWaitContainer.Initialize(profileService);
                SyncAfterWaitContainer.Initialize(profileService);
                SyncBeforeTargetContainer.Initialize(profileService);
                SyncAfterTargetContainer.Initialize(profileService);
                SyncAfterAllTargetsContainer.Initialize(profileService);
            }
        }

        public override void AfterParentChanged() {
            base.AfterParentChanged();

            if (Parent == null) {
                SequenceBlockTeardown();
            } else {
                SyncBeforeWaitContainer.AttachNewParent(Parent);
                SyncAfterWaitContainer.AttachNewParent(Parent);
                SyncBeforeTargetContainer.AttachNewParent(Parent);
                SyncAfterTargetContainer.AttachNewParent(Parent);
                SyncAfterAllTargetsContainer.AttachNewParent(Parent);

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

                SyncBeforeWaitContainer.ResetProgress();
                SyncAfterWaitContainer.ResetProgress();
                SyncBeforeTargetContainer.ResetProgress();
                SyncAfterTargetContainer.ResetProgress();
                SyncAfterAllTargetsContainer.ResetProgress();
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

            bool beforeWaitValid = SyncBeforeWaitContainer.Validate();
            bool afterWaitValid = SyncAfterWaitContainer.Validate();
            bool beforeNewTargetValid = SyncBeforeTargetContainer.Validate();
            bool afterNewTargetValid = SyncAfterTargetContainer.Validate();
            bool afterEachTargetValid = SyncAfterAllTargetsContainer.Validate();

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
                case EventContainerType.BeforeWait: { targetContainer = SyncBeforeWaitContainer; break; }
                case EventContainerType.AfterWait: { targetContainer = SyncAfterWaitContainer; break; }
                case EventContainerType.BeforeTarget: { targetContainer = SyncBeforeTargetContainer; break; }
                case EventContainerType.AfterTarget: { targetContainer = SyncAfterTargetContainer; break; }
                case EventContainerType.AfterEachTarget: { targetContainer = SyncAfterAllTargetsContainer; break; }
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