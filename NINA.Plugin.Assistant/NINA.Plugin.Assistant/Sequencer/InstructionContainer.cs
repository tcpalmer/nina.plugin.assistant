using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "SchedulerInstructionContainer")]
    [ExportMetadata("Description", "")]
    [ExportMetadata("Icon", "Pen_NoFill_SVG")]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class InstructionContainer : SequenceContainer, ISequenceContainer, IValidatable {
        private IProfileService profileService;
        private Object lockObj = new Object();

        private int syncActionTimeout;
        private int syncEventContainerTimeout;

        [JsonProperty]
        public EventContainerType EventContainerType { get; set; }

        [ImportingConstructor]
        public InstructionContainer() : base(new InstructionContainerStrategy()) { }

        public InstructionContainer(EventContainerType containerType, ISequenceContainer parent) : base(new InstructionContainerStrategy()) {
            EventContainerType = containerType;
            Name = containerType.ToString();
            AttachNewParent(Parent);
        }

        [OnDeserialized]
        public void OnDeserializedMethod(StreamingContext context) {
            EventContainerType = EventContainerHelper.Convert(Name);
        }

        public void Initialize(IProfileService profileService) {
            this.profileService = profileService;
            Initialize();
        }

        public override void Initialize() {
            foreach (ISequenceItem item in Items) {
                item.Initialize();
            }

            base.Initialize();
            SetSyncTimeouts();
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            var result = Task.CompletedTask;

            if (!SyncManager.Instance.IsRunning) {
                result = Items.Count > 0 ? base.Execute(progress, token) : Task.CompletedTask;
                return;
            }

            if (IsSyncServer()) {
                // Inform clients they can execute
                string eventContainerId = Guid.NewGuid().ToString();
                TSLogger.Info($"SYNC server waiting for clients to start {EventContainerType}");
                progress?.Report(new ApplicationStatus() { Status = $"Target Scheduler: waiting for sync clients to run {EventContainerType}" });
                await SyncServer.Instance.SyncEventContainer(eventContainerId, EventContainerType, syncActionTimeout, token);
                progress?.Report(new ApplicationStatus() { Status = "" });

                // Server can proceed with event container
                if (Items.Count > 0) {
                    await base.Execute(progress, token);
                }

                // Wait for clients to complete the event container
                TSLogger.Info($"SYNC server waiting for clients to complete {EventContainerType}");
                progress?.Report(new ApplicationStatus() { Status = $"Target Scheduler: waiting for sync clients to complete {EventContainerType}" });
                await SyncServer.Instance.WaitForClientEventContainerCompletion(EventContainerType, eventContainerId, syncEventContainerTimeout, token);
                progress?.Report(new ApplicationStatus() { Status = "" });
            }

            if (IsSyncClient()) {
                if (Items.Count > 0) {
                    await base.Execute(progress, token);
                }
            }

            return;
        }

        public override object Clone() {
            InstructionContainer ic = new InstructionContainer(EventContainerType, Parent);
            ic.Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem));
            foreach (var item in ic.Items) {
                item.AttachNewParent(ic);
            }

            AttachNewParent(Parent);
            return ic;
        }

        public new void MoveUp(ISequenceItem item) {
            lock (lockObj) {
                var index = Items.IndexOf(item);
                if (index == 0) {
                    return;
                } else {
                    base.MoveUp(item);
                }
            }
        }

        private void SetSyncTimeouts() {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                ProfilePreference profilePreference = context.GetProfilePreference(profileService.ActiveProfile.Id.ToString());
                syncActionTimeout = profilePreference != null ? profilePreference.SyncActionTimeout : SyncManager.DEFAULT_SYNC_ACTION_TIMEOUT;
                syncEventContainerTimeout = profilePreference != null ? profilePreference.SyncEventContainerTimeout : SyncManager.DEFAULT_SYNC_ACTION_TIMEOUT;
            }
        }

        private bool IsSyncServer() {
            return SyncManager.Instance.RunningServer;
        }

        private bool IsSyncClient() {
            return SyncManager.Instance.RunningClient;
        }
    }
}