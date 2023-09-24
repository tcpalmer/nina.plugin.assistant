using Grpc.Core;
using NINA.Plugin.Assistant.Shared.Utility;
using Scheduler.SyncService;

namespace Assistant.NINAPlugin.Sync {

    public enum ServerState {
        Ready,
        Waiting,
        WaitComplete
    }

    public class SyncServer : SchedulerSync.SchedulerSyncBase {

        private object lockobj = new object();

        private static readonly Lazy<SyncServer> lazy = new Lazy<SyncServer>(() => new SyncServer());
        public static SyncServer Instance { get => lazy.Value; }

        private Dictionary<string, SyncClientInstance> registeredClients;
        public ServerState State { get; set; }

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

        public bool AllClientsReady() {
            foreach (SyncClientInstance client in registeredClients.Values) {
                if (client.ClientState != ClientState.Ready) {
                    return false;
                }
            }

            return true;
        }

        public bool AllClientsWaiting() {
            foreach (SyncClientInstance client in registeredClients.Values) {
                if (client.ClientState != ClientState.Waiting) {
                    return false;
                }
            }

            return true;
        }

        private void LogRegisteredClients() {
            TSLogger.Debug("SYNC CLIENTS: ");
            foreach (SyncClientInstance client in registeredClients.Values) {
                TSLogger.Debug($"    {client}");
            }
        }
    }
}
