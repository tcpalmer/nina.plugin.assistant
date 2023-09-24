using GrpcDotNetNamedPipes;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using Scheduler.SyncService;
using System.Diagnostics;

namespace Assistant.NINAPlugin.Sync {

    public class SyncClient : SchedulerSync.SchedulerSyncClient {

        private static object lockObj = new object();

        private static readonly Lazy<SyncClient> lazy = new Lazy<SyncClient>(() => new SyncClient());
        public static SyncClient Instance { get => lazy.Value; }

        private string Id = Guid.NewGuid().ToString();
        private ClientState ClientState = ClientState.Starting;
        private bool keepaliveRunning = false;
        private CancellationTokenSource keepaliveCts;

        private SyncClient() : base(new NamedPipeChannel(".", SyncManager.PIPE_NAME, new NamedPipeChannelOptions() { ConnectionTimeout = 300000 })) {
        }

        public StatusResponse Register() {
            RegistrationRequest request = new RegistrationRequest {
                Guid = Id,
                Pid = Environment.ProcessId,
                ProfileId = "profileId",
                Timestamp = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
            };

            try {
                StatusResponse response = base.Register(request, null, deadline: DateTime.UtcNow.AddSeconds(5));
                if (response.Success) {
                    ClientState = ClientState.Ready;
                    StartKeepalive();
                }

                return response;
            }
            catch (Exception ex) {
                TSLogger.Error($"SYNC exception registering client with server: {ex.Message} {ex}");
                return new StatusResponse { Success = false, Message = ex.Message };
            }
        }

        public void Unregister() {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState.Ending
            };

            try {
                StopKeepalive();
                StatusResponse response = base.Unregister(request, null, deadline: DateTime.UtcNow.AddSeconds(5));
            }
            catch (Exception ex) {
                TSLogger.Info($"SYNC exception unregistering client with server: {ex.Message} {ex}");
            }
        }

        public async Task<StatusResponse> Keepalive(CancellationToken ct) {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState
            };

            StatusResponse response = await base.KeepaliveAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: ct);
            return response;
        }

        public async Task StartSyncWait(CancellationToken ct, TimeSpan timeout) {
            try {
                ClientState = ClientState.Waiting;
                ClientIdRequest request = new ClientIdRequest {
                    Guid = Id,
                    ClientState = ClientState
                };

                TSLogger.Info($"SYNC client syncwait starting, timeout is {timeout.TotalSeconds}s");
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (true) {
                    SyncWaitResponse response = await base.SyncWaitAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: ct);
                    if (!response.Continue) {
                        TSLogger.Info("SYNC client syncwait completed");
                        break;
                    }
                    else {
                        await Task.Delay(SyncManager.CLIENT_WAIT_POLL_PERIOD, ct);
                    }

                    if (stopwatch.Elapsed > timeout) {
                        TSLogger.Warning($"SYNC client timed out after {timeout.TotalSeconds} seconds");
                        break;
                    }
                }
            }
            catch (Exception) { throw; }
            finally { ClientState = ClientState.Ready; }
        }

        private Task StartKeepalive() {
            if (!keepaliveRunning) {

                lock (lockObj) {
                    if (!keepaliveRunning) {
                        keepaliveRunning = true;
                        TSLogger.Info($"SYNC starting keepalive for {Id}");

                        return Task.Run(async () => {
                            using (keepaliveCts = new CancellationTokenSource()) {
                                var token = keepaliveCts.Token;
                                while (!token.IsCancellationRequested) {
                                    try {
                                        await Task.Delay(SyncManager.CLIENT_KEEPALIVE_PERIOD, token);
                                        StatusResponse response = await Keepalive(token);
                                        if (!response.Success) {
                                            TSLogger.Error($"SYNC error in keepalive for {Id}: {response.Message}");
                                        }
                                    }
                                    catch (OperationCanceledException) {
                                        TSLogger.Info($"SYNC stopping keepalive for {Id}");
                                    }
                                    catch (Exception ex) {
                                        TSLogger.Error($"SYNC an error occurred during keepalive for {Id}", ex);
                                    }
                                }
                            }
                        });
                    }
                    else {
                        keepaliveRunning = false;
                        return Task.CompletedTask;
                    }
                }
            }
            else {
                return Task.CompletedTask;
            }
        }

        private void StopKeepalive() {
            if (keepaliveRunning) {
                lock (lockObj) {
                    if (keepaliveRunning) {
                        TSLogger.Info($"SYNC stopping sync client keepalive for {Id}");
                        try {
                            keepaliveCts?.Cancel();
                        }
                        catch (Exception) { }
                        keepaliveRunning = false;
                    }
                }
            }
        }
    }
}
