using Grpc.Core;
using NINA.Astrometry;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using Scheduler.SyncService;
using System.Collections.Concurrent;
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

        private object lockObj = new object();

        private static readonly Lazy<SyncServer> lazy = new Lazy<SyncServer>(() => new SyncServer());
        public static SyncServer Instance { get => lazy.Value; }

        private Dictionary<string, SyncClientInstance> registeredClients;
        private ConcurrentDictionary<string, string> clientActiveExposures;

        public ServerState State { get; set; }
        private ExposureResponse activeExposureResponse;
        private CancellationTokenSource staleClientPurgeCts;

        public SyncServer() {
            registeredClients = new Dictionary<string, SyncClientInstance>();
            clientActiveExposures = new ConcurrentDictionary<string, string>(Environment.ProcessorCount * 2, 31);
            SetServerState(ServerState.Ready);
            StartStaleClientPurge();
        }

        private void SetServerState(ServerState state) {
            lock (lockObj) {
                TSLogger.Info($"SYNC server setting state {State} -> {state}");
                State = state;
            }
        }

        public override Task<StatusResponse> Register(RegistrationRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server received client registration request {request.Guid} {request.Pid} {request.Timestamp.ToDateTime().ToLocalTime()}");

            lock (lockObj) {
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

            lock (lockObj) {
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

            lock (lockObj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    TSLogger.Info($"SYNC client sync wait: {request.Guid}");
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
            lock (lockObj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    if (State == ServerState.EndSyncContainers) {
                        TSLogger.Info($"SYNC server ending sync container {request.Guid}");
                        return Task.FromResult(new ExposureResponse { Success = true, Terminate = true });
                    }

                    // Don't think we want to do this here/now - instead let client call accept
                    //ClientState newState = (State == ServerState.ExposureReady) ? ClientState.Exposing : ClientState.Exposureready;
                    //registeredClients[request.Guid].SetState(newState);

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

            lock (lockObj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    registeredClients[request.Guid].SetState(request.ClientState);
                    registeredClients[request.Guid].SetLastAliveDate(request);
                    //LogRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
                else {
                    TSLogger.Warning($"keepalive: client not registered {request.Guid}");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is not registered" });
                }
            }
        }

        public async Task SyncExposure(string exposureId, InputTarget target, int targetDatabaseId, int exposurePlanDatabaseId, int syncExposureTimeout, CancellationToken token) {
            activeExposureResponse = new ExposureResponse {
                Success = true,
                ExposureReady = true,
                Terminate = false,
                ExposureId = exposureId,
                TargetName = target.DeepSkyObject.NameAsAscii,
                TargetRa = target.InputCoordinates.Coordinates.RAString,
                TargetDec = target.InputCoordinates.Coordinates.DecString,
                TargetPositionAngle = target.PositionAngle,
                TargetDatabaseId = targetDatabaseId,
                ExposurePlanDatabaseId = exposurePlanDatabaseId
            };

            SetServerState(ServerState.ExposureReady);
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromSeconds(syncExposureTimeout);

            TSLogger.Info($"SYNC server informing clients of available exposure ({exposureId}), timeout {timeout.TotalSeconds}s");

            while (NotTimedOut(stopwatch, timeout)) {
                if (AllClientsInState(ClientState.Exposing)) {
                    TSLogger.Info("SYNC server all clients now have the exposure, continuing");
                    SetServerState(ServerState.Ready);
                    activeExposureResponse = new ExposureResponse { Success = true, ExposureReady = false, Terminate = false };
                    SetClientActiveExposureList(exposureId);
                    return;
                }

                await Task.Delay(SyncManager.SERVER_AWAIT_EXPOSURE_POLL_PERIOD, token);
            }

            TSLogger.Warning($"SYNC server timed out waiting for one or more clients to accept exposure ({exposureId}), continuing");
        }

        public override Task<StatusResponse> AcceptExposure(ExposureRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server accepted exposure ({request.ExposureId}) from client ({request.Guid})");
            registeredClients[request.Guid].SetState(ClientState.Exposing);
            return Task.FromResult(new StatusResponse { Success = true, Message = "" });
        }

        public override Task<StatusResponse> CompleteExposure(ExposureRequest request, ServerCallContext context) {
            TSLogger.Info($"SYNC server received completed exposure ({request.ExposureId}) from client ({request.Guid})");

            if (RemoveClientFromActiveExposureList(request.Guid, request.ExposureId)) {
                registeredClients[request.Guid].SetState(ClientState.Exposureready);
                return Task.FromResult(new StatusResponse { Success = true, Message = "" });
            }
            else {
                TSLogger.Warning($"SYNC server client not found in active exposure list: {request.Guid}");
                return Task.FromResult(new StatusResponse { Success = false, Message = "client not found in active exposure list" });
            }
        }

        public bool AllClientsInState(ClientState state) {
            foreach (SyncClientInstance client in registeredClients.Values) {
                if (client.ClientState != state) {
                    return false;
                }
            }

            return true;
        }

        public async Task WaitForClientExposureCompletion(string exposureId, CancellationToken token) {

            if (clientActiveExposures.Count == 0) {
                TSLogger.Warning("SYNC server not waiting on any clients for completed exposures");
                return;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromSeconds(SyncManager.SERVER_AWAIT_EXPOSURE_COMPLETE_TIMEOUT);

            TSLogger.Info($"SYNC server waiting for all clients to complete exposure: {exposureId}, timeout {SyncManager.SERVER_AWAIT_EXPOSURE_COMPLETE_TIMEOUT}s");

            while (NotTimedOut(stopwatch, timeout)) {
                if (clientActiveExposures.Count == 0) {
                    TSLogger.Info($"SYNC server all clients have completed exposure: {exposureId}");
                    return;
                }

                await Task.Delay(SyncManager.SERVER_AWAIT_EXPOSURE_COMPLETE_POLL_PERIOD, token);
            }

            // If we timed out waiting for client exposures, then we need clear the list and let the server continue
            TSLogger.Warning($"SYNC server timed out waiting for all clients to complete exposure: {exposureId}, clearing wait list and continuing.  Remaining was:");
            LogClientActiveExposureList();
            clientActiveExposures.Clear();
        }

        private void SetClientActiveExposureList(string exposureId) {
            lock (lockObj) {
                clientActiveExposures.Clear();
                foreach (SyncClientInstance client in registeredClients.Values) {
                    if (client.ClientState == ClientState.Exposing) {
                        clientActiveExposures.TryAdd(client.Guid, exposureId);
                    }
                }

                LogClientActiveExposureList();
            }
        }

        private bool RemoveClientFromActiveExposureList(string clientId, string exposureId) {
            lock (lockObj) {
                if (clientActiveExposures.ContainsKey(clientId)) {
                    string eid;
                    bool success = clientActiveExposures.TryRemove(clientId, out eid);
                    if (success && eid != exposureId) {
                        TSLogger.Warning($"SYNC server unexpected exposureId for client ({clientId}: found {eid} but expected {exposureId})");
                    }

                    LogClientActiveExposureList();
                    return true;
                }

                return false;
            }
        }

        private void LogClientActiveExposureList() {
            StringBuilder sb = new StringBuilder();
            foreach (var item in clientActiveExposures) {
                sb.Append($"{item.Key}:{item.Value} ");
            }

            TSLogger.Debug($"SYNC server active client exposure list: {sb}");
        }

        private void LogRegisteredClients() {
            if (registeredClients.Count == 0) {
                return;
            }

            if (registeredClients.Count == 1) {
                SyncClientInstance client = registeredClients.First().Value;
                TSLogger.Debug($"SYNC CLIENT: {client.Guid} state={client.ClientState} lastAlive={client.LastAliveDate}");
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

                            lock (lockObj) {
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

        public SyncClientExposureResults(ExposureRequest request) {
            ClientId = request.Guid;
            ExposureId = request.ExposureId;
        }
    }
}
