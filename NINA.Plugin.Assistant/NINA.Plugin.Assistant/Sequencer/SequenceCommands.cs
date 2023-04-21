using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    class SequenceCommands {

        public static void StopGuiding(IGuiderMediator guiderMediator, CancellationToken token) {
            if (guiderMediator.GetInfo().Connected) {
                TSLogger.Info("stopping guiding");
                var stoppedGuiding = Task.Run(async () => {
                    await guiderMediator.StopGuiding(token);
                });
            }
            else {
                TSLogger.Warning("no guider connected, skipping StopGuiding");
            }
        }

        public static void StartGuiding(IGuiderMediator guiderMediator, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (guiderMediator.GetInfo().Connected) {
                TSLogger.Info("starting guiding");
                var stoppedGuiding = Task.Run(async () => {
                    await guiderMediator.StartGuiding(false, progress, token);
                });
            }
            else {
                TSLogger.Warning("no guider connected, skipping StartGuiding");
            }
        }

        public static void SetTelescopeTracking(ITelescopeMediator telescopeMediator, TrackingMode trackingMode, CancellationToken token) {
            if (telescopeMediator.GetInfo().Connected) {
                TSLogger.Info($"set mount tracking to {trackingMode}");
                telescopeMediator.SetTrackingMode(trackingMode);
            }
            else {
                TSLogger.Warning("no mount connected, skipping SetTelescopeTracking");
            }
        }

        public static async Task ParkTelescope(ITelescopeMediator telescopeMediator, IGuiderMediator guiderMediator, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().Connected) {
                TSLogger.Info($"parking telescope");
                StopGuiding(guiderMediator, token);
                await guiderMediator.StopGuiding(token);
                if (!await telescopeMediator.ParkTelescope(progress, token)) {
                    throw new SequenceEntityFailedException();
                }
            }
            else {
                TSLogger.Warning("no mount connected, skipping ParkTelescope");
            }
        }

        public static async Task UnparkTelescope(ITelescopeMediator telescopeMediator, IProgress<ApplicationStatus> progress, CancellationToken token) {
            if (telescopeMediator.GetInfo().Connected) {
                TSLogger.Info($"unparking telescope");
                bool success = await telescopeMediator.UnparkTelescope(progress, token);
                if (!success) {
                    throw new SequenceEntityFailedException();
                }
            }
            else {
                TSLogger.Warning("no mount connected, skipping UnparkTelescope");
            }
        }

        public static async Task ParkDome(IDomeMediator domeMediator, CancellationToken token) {
            if (domeMediator.GetInfo().Connected) {
                TSLogger.Info($"parking dome");
                if (!await domeMediator.Park(token)) {
                    throw new SequenceEntityFailedException();
                }
            }
            else {
                TSLogger.Warning("no dome connected, skipping ParkDome");
            }
        }

        private SequenceCommands() { }
    }
}
