using Grpc.Core;
using GrpcDotNetNamedPipes;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using Scheduler.SyncService;
using System;
using System.Diagnostics;
using System.Threading;

namespace Assistant.NINAPlugin.Sync {

    public class SyncClient : SchedulerSync.SchedulerSyncClient {

        private static object lockObj = new object();

        private static readonly Lazy<SyncClient> lazy = new Lazy<SyncClient>(() => new SyncClient());
        public static SyncClient Instance { get => lazy.Value; }

        public readonly string Id = Guid.NewGuid().ToString();
        public string ProfileId { get; private set; }
        private ClientState ClientState = ClientState.Starting;
        private bool keepaliveRunning = false;
        private CancellationTokenSource keepaliveCts;

        private SyncClient() : base(new NamedPipeChannel(".", SyncManager.PIPE_NAME, new NamedPipeChannelOptions() { ConnectionTimeout = 300000 })) {
        }

        public StatusResponse Register(string profileId) {
            ProfileId = profileId;
            RegistrationRequest request = new RegistrationRequest {
                Guid = Id,
                Pid = Environment.ProcessId,
                ProfileId = ProfileId,
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

        public async void Unregister() {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState.Ending
            };

            try {
                StopKeepalive();
                var task = Task.Run(() => base.UnregisterAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(2)));
                await Task.WhenAny(task, Task.Delay(2000));
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

        public async Task<SyncedExposure?> StartRequestExposure(CancellationToken ct) {
            ClientState = ClientState.Exposureready;
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState
            };

            TSLogger.Info($"SYNC client sync container starting polling for exposures");

            while (true) {
                try {
                    ExposureResponse response = await base.RequestExposureAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: ct);

                    if (response.Terminate) {
                        TSLogger.Info("SYNC client sync container completed");
                        ClientState = ClientState.Ready;
                        return null;
                    }

                    if (response.ExposureReady) {
                        TSLogger.Info($"SYNC client sync container received exposure request ({response.ExposureId})");
                        ClientState = ClientState.Exposing;
                        return new SyncedExposure(response.ExposureId, response.TargetName, response.TargetRa, response.TargetDec, response.TargetPositionAngle, response.ExposurePlanDatabaseId);
                    }

                    await Task.Delay(SyncManager.CLIENT_EXPOSURE_READY_POLL_PERIOD, ct);
                }
                catch (Exception e) {
                    if (e is TaskCanceledException || (e is RpcException && e.Message.Contains("Cancelled"))) {
                        TSLogger.Info("SYNC client sync container canceled, ending");
                        ClientState = ClientState.Ready;
                        return null;
                    }

                    TSLogger.Error("SYNC client exception in request exposure", e);
                    await Task.Delay(2000, ct); // at least slow down exceptions repeating
                }
            }
        }

        public async Task SubmitExposure(SubmitExposureRequest request) {
            try {
                TSLogger.Info($"SYNC client submitting exposure to server ({request.Guid})");
                StatusResponse response = await base.SubmitExposureAsync(request);
                if (!response.Success) {
                    TSLogger.Error($"SYNC client problem submitting exposure: {response.Message}");
                }
            }
            catch (Exception e) {
                TSLogger.Error("SYNC client exception submitting exposure", e);
            }
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
                        catch (Exception ex) {
                            TSLogger.Error("exception stopping sync client keepalive", ex);
                        }

                        keepaliveRunning = false;
                    }
                }
            }
        }
    }

    public class SyncedExposure {

        public string ExposureId { get; private set; }
        public string TargetName { get; private set; }
        public string TargetRA { get; private set; }
        public string TargetDec { get; private set; }
        public double TargetPositionAngle { get; private set; }
        public int ExposurePlanDatabaseId { get; private set; }

        public SyncedExposure(string exposureId, string targetName, string targetRA, string targetDec, double targetPositionAngle, int exposurePlanDatabaseId) {
            ExposureId = exposureId;
            TargetName = targetName;
            TargetRA = targetRA;
            TargetDec = targetDec;
            TargetPositionAngle = targetPositionAngle;
            ExposurePlanDatabaseId = exposurePlanDatabaseId;
        }
    }
}
