using Grpc.Core;
using Scheduler.SyncService;

namespace Assistant.NINAPlugin.Sync {

    public class SyncServer : SchedulerSync.SchedulerSyncBase {

        public static readonly string PIPE_NAME = "TargetScheduler.Sync";
        private object lockobj = new object();

        private static readonly Lazy<SyncServer> lazy = new Lazy<SyncServer>(() => new SyncServer());
        public static SyncServer Instance { get => lazy.Value; }

        private Dictionary<string, SyncClientInstance> registeredClients;

        public SyncServer() {
            registeredClients = new Dictionary<string, SyncClientInstance>();
        }

        public override Task<StatusResponse> Register(RegistrationRequest request, ServerCallContext context) {
            //TSLogger.Info($"sync, server received client registration request {request.Guid} {request.Pid} {request.Timestamp.ToDateTime().ToLocalTime()}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    //TSLogger.Warning($"sync, warning: client {request.Guid} is already registered");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is already registered" });
                }
                else {
                    //TSLogger.Info($"sync, registering client: {request.Guid}");
                    registeredClients[request.Guid] = new SyncClientInstance(request);
                    DumpRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
            }
        }

        public override Task<StatusResponse> Unregister(ClientIdRequest request, ServerCallContext context) {
            //TSLogger.Info($"sync, server received client unregister request {request.Guid}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    //TSLogger.Info($"sync, unregistering client: {request.Guid}");
                    registeredClients.Remove(request.Guid);
                    DumpRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
                else {
                    //TSLogger.Warning($"sync, client does not exist: {request.Guid}");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is not registered" });
                }
            }
        }

        public override Task<StatusResponse> Keepalive(ClientIdRequest request, ServerCallContext context) {
            //TSLogger.Info($"sync, keepalive {request.Guid} {request.ClientState}");

            lock (lockobj) {
                if (registeredClients.ContainsKey(request.Guid)) {
                    registeredClients[request.Guid].SetState(request);
                    registeredClients[request.Guid].SetLastAliveDate(request);
                    DumpRegisteredClients();
                    return Task.FromResult(new StatusResponse { Success = true, Message = "" });
                }
                else {
                    //TSLogger.Warning($"keepalive: client not registered {request.Guid}");
                    return Task.FromResult(new StatusResponse { Success = false, Message = "client is not registered" });
                }
            }
        }

        private void DumpRegisteredClients() {
            //TSLogger.Debug("CLIENT: ");
            foreach (SyncClientInstance client in registeredClients.Values) {
                //TSLogger.Debug($"    {client}");
            }
        }
    }
}
