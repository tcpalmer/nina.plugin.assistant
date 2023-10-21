using Grpc.Core;
using GrpcDotNetNamedPipes;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using Scheduler.SyncService;
using System;
using System.Diagnostics;
using System.Threading;
using static System.Windows.Forms.AxHost;

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

        public void SetClientState(ClientState state) {
            lock (lockObj) {
                TSLogger.Info($"SYNC client setting state {ClientState} -> {state}");
                ClientState = state;
            }
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
                    SetClientState(ClientState.Ready);
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
            SetClientState(ClientState.Ending);
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState
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

        public async Task<StatusResponse> Keepalive(CancellationToken token) {
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState
            };

            StatusResponse response = await base.KeepaliveAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: token);
            return response;
        }

        public async Task StartSyncWait(CancellationToken token, TimeSpan timeout) {
            try {
                SetClientState(ClientState.Waiting);
                ClientIdRequest request = new ClientIdRequest {
                    Guid = Id,
                    ClientState = ClientState
                };

                TSLogger.Info($"SYNC client sync wait starting, timeout is {timeout.TotalSeconds}s");
                Stopwatch stopwatch = Stopwatch.StartNew();

                while (true) {
                    SyncWaitResponse response = await base.SyncWaitAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: token);
                    if (!response.Continue) {
                        TSLogger.Info("SYNC client sync wait completed");
                        break;
                    }
                    else {
                        await Task.Delay(SyncManager.CLIENT_WAIT_POLL_PERIOD, token);
                    }

                    if (stopwatch.Elapsed > timeout) {
                        TSLogger.Warning($"SYNC client timed out after {timeout.TotalSeconds} seconds");
                        break;
                    }
                }
            }
            catch (Exception) { throw; }
            finally { SetClientState(ClientState.Ready); }
        }

        public async Task<SyncedExposure?> StartRequestExposure(CancellationToken token) {
            SetClientState(ClientState.Exposureready);
            ClientIdRequest request = new ClientIdRequest {
                Guid = Id,
                ClientState = ClientState
            };

            TSLogger.Info($"SYNC client starting polling for exposures");

            while (true) {
                try {
                    ExposureResponse response = await base.RequestExposureAsync(request, null, deadline: DateTime.UtcNow.AddSeconds(5), cancellationToken: token);

                    if (response.Terminate) {
                        TSLogger.Info("SYNC client completed");
                        SetClientState(ClientState.Ready);
                        return null;
                    }

                    if (response.ExposureReady) {
                        await AcceptExposure(response.ExposureId);
                        SetClientState(ClientState.Exposing);
                        return new SyncedExposure(response.ExposureId, response.TargetName, response.TargetRa, response.TargetDec, response.TargetPositionAngle, response.TargetDatabaseId, response.ExposurePlanDatabaseId);
                    }

                    await Task.Delay(SyncManager.CLIENT_EXPOSURE_READY_POLL_PERIOD, token);
                }
                catch (Exception e) {
                    if (e is TaskCanceledException || (e is RpcException && e.Message.Contains("Cancelled"))) {
                        TSLogger.Info("SYNC client sync container cancelled, ending");
                        SetClientState(ClientState.Ready);
                        return null;
                    }

                    TSLogger.Error("SYNC client exception in request exposure", e);
                    await Task.Delay(2000, token); // at least slow down exceptions repeating
                }
            }
        }

        public async Task AcceptExposure(string exposureId) {
            ExposureRequest request = new ExposureRequest {
                Guid = Id,
                ExposureId = exposureId
            };

            try {
                TSLogger.Info($"SYNC client accepting exposure ({request.ExposureId})");
                StatusResponse response = await base.AcceptExposureAsync(request);
                if (!response.Success) {
                    TSLogger.Error($"SYNC client problem accepting exposure: {response.Message}");
                }
            }
            catch (Exception e) {
                TSLogger.Error("SYNC client exception accepting exposure", e);
            }
        }

        public async Task SubmitCompletedExposure(string exposureId) {
            ExposureRequest request = new ExposureRequest {
                Guid = Id,
                ExposureId = exposureId
            };

            try {
                TSLogger.Info($"SYNC client submitting completed exposure to server ({request.ExposureId})");
                StatusResponse response = await base.CompleteExposureAsync(request);
                if (!response.Success) {
                    TSLogger.Error($"SYNC client problem submitting completed exposure: {response.Message}");
                }
            }
            catch (Exception e) {
                TSLogger.Error("SYNC client exception submitting completed exposure", e);
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
        public int TargetDatabaseId { get; private set; }
        public int ExposurePlanDatabaseId { get; private set; }

        public SyncedExposure(string exposureId, string targetName, string targetRA, string targetDec, double targetPositionAngle, int targetDatabaseId, int exposurePlanDatabaseId) {
            ExposureId = exposureId;
            TargetName = targetName;
            TargetRA = targetRA;
            TargetDec = targetDec;
            TargetPositionAngle = targetPositionAngle;
            TargetDatabaseId = targetDatabaseId;
            ExposurePlanDatabaseId = exposurePlanDatabaseId;
        }
    }
}
