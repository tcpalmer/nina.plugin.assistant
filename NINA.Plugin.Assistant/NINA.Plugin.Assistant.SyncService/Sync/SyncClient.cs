using GrpcDotNetNamedPipes;
using Scheduler.SyncService;

namespace Assistant.NINAPlugin.Sync {

    public class SyncClient : SchedulerSync.SchedulerSyncClient {

        private static object lockObj = new object();

        private static readonly Lazy<SyncClient> lazy = new Lazy<SyncClient>(() => new SyncClient());
        public static SyncClient Instance { get => lazy.Value; }

        private string Id = Guid.NewGuid().ToString();
        private bool keepaliveRunning = false;
        private CancellationTokenSource keepaliveCts;

        private SyncClient() : base(new NamedPipeChannel(".", SyncServer.PIPE_NAME, new NamedPipeChannelOptions() { ConnectionTimeout = 300000 })) {
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
                    StartKeepalive();
                }

                return response;
            }
            catch (Exception ex) {
                Console.WriteLine($"exception registering client with server: {ex.Message} {ex}");
                return new StatusResponse { Success = false, Message = ex.Message };
            }
        }

        public void Unregister() {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState.Ending
            };

            try {
                StatusResponse response = base.Unregister(request, null, deadline: DateTime.UtcNow.AddSeconds(5));
            }
            catch (Exception ex) {
                Console.WriteLine($"exception unregistering client with server: {ex.Message} {ex}");
            }
            finally {
                StopKeepalive();
            }
        }

        public async Task<StatusResponse> Keepalive(CancellationToken ct) {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState.Waiting // this state isn't really appropriate for keepalive since client could be doing anything
            };

            StatusResponse response = await base.KeepaliveAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: ct);
            return response;
        }

        private Task StartKeepalive() {
            if (!keepaliveRunning) {

                lock (lockObj) {
                    if (!keepaliveRunning) {
                        keepaliveRunning = true;
                        Console.WriteLine($"Starting keepalive for {Id}");

                        return Task.Run(async () => {
                            using (keepaliveCts = new CancellationTokenSource()) {
                                var token = keepaliveCts.Token;
                                while (!token.IsCancellationRequested) {
                                    try {
                                        await Task.Delay(1000, token);
                                        StatusResponse response = await Keepalive(token);
                                        if (!response.Success) {
                                            Console.WriteLine($"Error in keepalive for {Id}: {response.Message}");
                                        }
                                    }
                                    catch (OperationCanceledException) {
                                        Console.WriteLine($"Stopping keepalive for {Id}");
                                    }
                                    catch (Exception ex) {
                                        Console.WriteLine($"An error occurred during keepalive for {Id}", ex);
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
                        Console.WriteLine($"stopping sync client keepalive for {Id}");
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
