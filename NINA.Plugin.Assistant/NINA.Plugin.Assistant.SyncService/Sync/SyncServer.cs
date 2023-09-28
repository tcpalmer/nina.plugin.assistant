using Grpc.Core;
using NINA.Astrometry;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using Scheduler.SyncService;
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

        public SyncServer() {
            registeredClients = new Dictionary<string, SyncClientInstance>();
            State = ServerState.Ready;
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
            activeExposureResponse = new ExposureResponse {
                Success = true,
                ExposureReady = true,
                Terminate = false,
                TargetName = target.DeepSkyObject.NameAsAscii,
                TargetRa = target.InputCoordinates.Coordinates.RAString,
                TargetDec = target.InputCoordinates.Coordinates.DecString,
                TargetPositionAngle = target.PositionAngle,
                ExposurePlanDatabaseId = exposurePlanDatabaseId
            };

            State = ServerState.ExposureReady;
            Stopwatch stopwatch = Stopwatch.StartNew();
            TimeSpan timeout = TimeSpan.FromSeconds(syncExposureTimeout);

            TSLogger.Info($"SYNC server informing clients of available exposure, timeout is {timeout.TotalSeconds}s");

            while (NotTimedOut(stopwatch, timeout)) {
                bool allExposing = true;
                foreach (SyncClientInstance client in registeredClients.Values) {
                    if (client.ClientState != ClientState.Exposing) {
                        allExposing = false;
                        break;
                    }
                }

                if (allExposing) {
                    TSLogger.Info("SYNC server all clients now have the exposure, continuing");
                    State = ServerState.Ready;
                    activeExposureResponse = new ExposureResponse { Success = true, ExposureReady = false, Terminate = false };
                    return;
                }

                await Task.Delay(SyncManager.SERVER_AWAIT_EXPOSURE_POLL_PERIOD, token);
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
                TSLogger.Warning($"SYNC server exposure hand offs timed out after {timeout.TotalSeconds} seconds");
                return false;
            }

            return true;
        }
    }
}
