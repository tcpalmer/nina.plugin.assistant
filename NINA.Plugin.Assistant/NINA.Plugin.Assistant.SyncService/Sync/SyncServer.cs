using Grpc.Core;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NmeaParser.Gnss.Ntrip;
using Scheduler.SyncService;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Assistant.NINAPlugin.Sync {

    public enum ServerState {
        Ready,
        Waiting,
        WaitComplete,
        PlanWait,
        ExposureReady,
        EndSyncContainers
    }

    public class SyncServer : SchedulerSync.SchedulerSyncBase {

        private object lockobj = new object();

        private static readonly Lazy<SyncServer> lazy = new Lazy<SyncServer>(() => new SyncServer());
        public static SyncServer Instance { get => lazy.Value; }

        private Dictionary<string, SyncClientInstance> registeredClients;
        public ServerState State { get; set; }
        private ExposureResponse activeExposureResponse;
        private CancellationTokenSource staleClientPurgeCts;

        private ConcurrentQueue<SyncClientExposureResults> clientExposureQueue = new ConcurrentQueue<SyncClientExposureResults>();

        public SyncServer() {
            registeredClients = new Dictionary<string, SyncClientInstance>();
            State = ServerState.Ready;
            StartStaleClientPurge();
        }

        public override Task<StatusResponse> Register(RegistrationRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server received client registration request {request.Guid} {request.Pid} {request.Timestamp.ToDateTime().ToLocalTime()}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    TSLogger.Warning($"SYNC warning: client {request.Guid} is already registered");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is already registered" });
                }
                else {
                    TSLogger.Info($"SYNC registering client: {request.Guid}");
                    registeredClients[request.Guid] = new SyncClientInstance(request);
                    LogRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
            }
        }

        public override Task<StatusResponse> Unregister(ClientIdRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server received client unregister request {request.Guid}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    TSLogger.Info($"SYNC unregistering client: {request.Guid}");
                    registeredClients.Remove(request.Guid);
                    LogRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
                else {
                    TSLogger.Warning($"SYNC client does not exist: {request.Guid}");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is not registered" });
                }
            }
        }

        public override Task<SyncWaitResponse> SyncWait(ClientIdRequest request, ServerCallContext context) {

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    TSLogger.Info($"SYNC client syncwait: {request.Guid}");
                    bool willContinue = State != ServerState.WaitComplete;
                    registeredClients[request.Guid].SetState(willContinue ? ClientState.Waiting : ClientState.Ready);

                    LogRegisteredClients();
                    return Task.FromResult(new SyncWaitResponse { Success = true, Continue = willContinue });

                }
                else {
                    TSLogger.Warning($"SYNC client does not exist: {request.Guid}");
                    return Task.FromResult(new SyncWaitResponse { Success = false, Continue = false });
                }
            }
        }

        public override Task<ExposureResponse> RequestExposure(ClientIdRequest request, ServerCallContext context) {
            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    TSLogger.Info($"SYNC server received client exposure request {request.Guid}");

                    if (State == ServerState.EndSyncContainers) {
                        TSLogger.Info($"SYNC server ending sync container {request.Guid}");
                        return Task.FromResult(new ExposureResponse { Success = true, Terminate = true });
                    }

                    ClientState newState = (State == ServerState.ExposureReady) ? ClientState.Exposing : ClientState.Exposureready;
                    registeredClients[request.Guid].SetState(newState);
                    ExposureResponse response = (State == ServerState.ExposureReady) ?
                        activeExposureResponse : new ExposureResponse { Success = true, ExposureReady = false, Terminate = false };

                    LogRegisteredClients();
                    return Task.FromResult(response);
                }
                else {
                    TSLogger.Warning($"SYNC client does not exist: {request.Guid}");
                    return Task.FromResult(new ExposureResponse { Success = false, Terminate = true });
                }
            }
        }

        public override Task<StatusResponse> Keepalive(ClientIdRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC keepalive {request.Guid} {request.ClientState}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    registeredClients[request.Guid].SetLastAliveDate(request);
                    LogRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
                else {
                    TSLogger.Warning($"keepalive: client not registered {request.Guid}");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is not registered" });
                }
            }
        }

        public async Task SyncExposure(InputTarget target, CancellationToken token, int exposurePlanDatabaseId, int syncExposureTimeout) {
            string exposureId = Guid.NewGuid().ToString();
            activeExposureResponse = new ExposureResponse {
                Success = true,
                ExposureReady = true,
                Terminate = false,
                ExposureId = exposureId,
                TargetName = target.DeepSkyObject.NameAsAscii,
                TargetRa = target.InputCoordinates.Coordinates.RAString,
                TargetDec = target.InputCoordinates.Coordinates.DecString,
                TargetPositionAngle = target.PositionAngle,
                ExposurePlanDatabaseId = exposurePlanDatabaseId
            };

            State = ServerState.ExposureReady;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromSeconds(syncExposureTimeout);

            TSLogger.Info($"SYNC server informing clients of available exposure ({exposureId}), timeout is {timeout.TotalSeconds}s");

            while (NotTimedOut(stopwatch, timeout)) {
                if (AllClientsInState(ClientState.Exposing)) {
                    TSLogger.Info("SYNC server all clients now have the exposure, continuing");
                    State = ServerState.Ready;
                    activeExposureResponse = new ExposureResponse { Success = true, ExposureReady = false, Terminate = false };
                    return;
                }

                await Task.Delay(SyncManager.SERVER_AWAIT_EXPOSURE_POLL_PERIOD, token);
            }

            TSLogger.Warning("SYNC server timed out waiting for one or more clients to accept exposure, continuing");
        }

        public override Task<StatusResponse> SubmitExposure(SubmitExposureRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server received exposure ({request.ExposureId}) from client ({request.Guid})");
            clientExposureQueue.Enqueue(new SyncClientExposureResults(request));
            return Task.FromResult(new StatusResponse { Success = true, Message = "" });
        }

        public SyncClientExposureResults? DequeueSyncClientExposure() {
            SyncClientExposureResults? result;
            bool success = clientExposureQueue.TryDequeue(out result);
            return success ? result : null;
        }

        public bool AllClientsInState(ClientState state) {
            foreach (SyncClientInstance client in registeredClients.Values) {
                if (client.ClientState != state) {
                    return false;
                }
            }

            return true;
        }

        private void LogRegisteredClients() {
            if (registeredClients.Count == 0) {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (SyncClientInstance client in registeredClients.Values) {
                sb.AppendLine($"    {client.Guid} state={client.ClientState} lastAlive={client.LastAliveDate}");
            }

            TSLogger.Debug($"SYNC CLIENTS:\n{sb}");
        }

        private bool NotTimedOut(Stopwatch stopwatch, TimeSpan timeout) {
            if (stopwatch.Elapsed > timeout) {
                return false;
            }

            return true;
        }

        public void Shutdown() {
            try {
                staleClientPurgeCts?.Cancel();
            }
            catch (Exception ex) {
                TSLogger.Error("exception stopping stale client purge thread", ex);
            }
        }

        private Task StartStaleClientPurge() {
            TSLogger.Info($"SYNC starting stale client purge thread");

            return Task.Run(async () => {
                using (staleClientPurgeCts = new CancellationTokenSource()) {
                    var token = staleClientPurgeCts.Token;
                    while (!token.IsCancellationRequested) {
                        try {
                            await Task.Delay(SyncManager.SERVER_STALE_CLIENT_PURGE_CHECK_PERIOD, token);

                            List<String> purgeList = new List<String>();
                            foreach (SyncClientInstance client in registeredClients.Values) {
                                TimeSpan timeSpan = DateTime.Now - client.LastAliveDate;
                                if (timeSpan.TotalSeconds > SyncManager.SERVER_STALE_CLIENT_PURGE_TIMEOUT) {
                                    purgeList.Add(client.Guid);
                                }
                            }

                            for (int i = 0; i < purgeList.Count; i++) {
                                TSLogger.Warning($"SYNC detected stale client, purging {purgeList[i]}");
                                registeredClients.Remove(purgeList[i]);
                            }
                        }
                        catch (OperationCanceledException) {
                            TSLogger.Info($"SYNC stopping stale client purge thread");
                        }
                        catch (Exception ex) {
                            TSLogger.Error($"SYNC an error occurred during stale client purge", ex);
                        }
                    }
                }
            });
        }
    }

    public class SyncClientExposureResults {

        public readonly string ClientId;
        public readonly string ExposureId;
        public readonly int ProjectDatabaseId;
        public readonly int TargetDatabaseId;
        public readonly DateTime AcquiredDate;
        public readonly string FilterName;
        // TODO: ImageMetadata

        public SyncClientExposureResults(SubmitExposureRequest request) {
            ClientId = request.Guid;
            ExposureId = request.ExposureId;
            ProjectDatabaseId = request.ProjectDatabaseId;
            TargetDatabaseId = request.TargetDatabaseId;
            AcquiredDate = request.AcquiredDate.ToDateTime().ToLocalTime();
            FilterName = request.FilterName;

            // TODO: ImageMetadata

        }
    }
}
