using Assistant.NINAPlugin.Sync;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Sync Wait")]
    [ExportMetadata("Description", "Target Scheduler synchronized waiting for multiple NINA instances")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerSyncWait : SequenceItem {

        [ImportingConstructor]
        public TargetSchedulerSyncWait() {
        }

        public TargetSchedulerSyncWait(TargetSchedulerSyncWait cloneMe) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TargetSchedulerSyncWait(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            if (SyncManager.Instance.IsRunning) {
                if (SyncManager.Instance.IsServer) {
                    TSLogger.Info("TargetSchedulerSyncWait execute, server waiting on all clients to report wait");
                    // TODO: ask server to check all on client states:
                    //   If all waiting => end execute
                    //   If not all waiting => sleep and try again
                }
                else {
                    TSLogger.Info("TargetSchedulerSyncWait execute, setting client to sync wait");
                    progress?.Report(new ApplicationStatus() { Status = "Waiting for leader to sync" });
                    SyncClient.Instance.StartSyncWait();
                    // TODO: this needs to loop too

                    // TODO: 


                    progress?.Report(new ApplicationStatus() { Status = "" });
                }
            }
            else {
                TSLogger.Info("TargetSchedulerSyncWait execute but sync server not running");
            }

            return Task.CompletedTask;
        }

        /*

New Instruction: Target Scheduler Synced Wait

This instruction is inserted into each sequence (perhaps more than once) prior to the TS Containers.
        The sequences used by primary and secondary instances must have ?matching? Target Scheduler Synced Wait instructions. When executed, it works as follows:

    If running on a secondary instance, it begins polling, telling the primary that it is in ?sync wait? state.
    If running on the primary instance, when it receives a ?sync wait? request from a secondary, it marks that instance as waiting.

On the server, if there are secondaries that have not yet reported to be ?sync wait?, it returns a ?continue waiting? response to the secondary. When all secondaries have reported ?sync wait?, the server then responds (on the next request) with a ?proceed? response. When the secondaries receive that, they end the Target Scheduler Sync Wait instruction and the secondary sequence proceeds with the next instruction. The server instance proceeds when all secondaries have checked in and been told to continue.

There is certainly the potential for deadlock here:

    If a secondary was shutdown, then it?s keepalive would stop which can be detected. The primary simply wouldn?t wait for a secondary with a stale keepalive time.
    If a secondary doesn?t have a matching Target Scheduler Synced Wait instruction, then the primary will never move past the Sync Wait on its side. Assuming all secondary keepalives are active, the primary will need an overall timeout on this operation so that it could keep going. This timeout would have to be lengthy (many minutes) so that a client could perform operations like autofocus.
    If the sequence for a secondary isn?t running, it can?t enter the ?sync wait? state. If we can detect if the sequence is stopped, we could change the secondary state to reflect that which would propagate to the server with the next keepalive.

Secondaries that didn?t participate in this wait may still recover but that requires testing.         */
    }
}
