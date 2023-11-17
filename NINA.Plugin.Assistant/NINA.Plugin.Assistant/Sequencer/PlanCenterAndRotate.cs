using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Sync;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.PlateSolving.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem.Platesolving;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class PlanCenterAndRotate : CenterAndRotate {

        private IPlanTarget planTarget;
        private InputTarget target;
        private int syncActionTimeout;
        private int syncSolveRotateTimeout;

        public PlanCenterAndRotate(IPlanTarget planTarget,
                           InputTarget target,
                           int syncActionTimeout,
                           int syncSolveRotateTimeout,
                           IProfileService profileService,
                           ITelescopeMediator telescopeMediator,
                           IImagingMediator imagingMediator,
                           IRotatorMediator rotatorMediator,
                           IFilterWheelMediator filterWheelMediator,
                           IGuiderMediator guiderMediator,
                           IDomeMediator domeMediator,
                           IDomeFollower domeFollower,
                           IPlateSolverFactory plateSolverFactory,
                           IWindowServiceFactory windowServiceFactory) : base(profileService, telescopeMediator, imagingMediator, rotatorMediator, filterWheelMediator, guiderMediator, domeMediator, domeFollower, plateSolverFactory, windowServiceFactory) {

            this.planTarget = planTarget;
            this.target = target;
            this.syncActionTimeout = syncActionTimeout;
            this.syncSolveRotateTimeout = syncSolveRotateTimeout;
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {

            // Server has to perform slew/center/rotate first
            TSLogger.Info($"SYNC server performing server slew/center/rotate");
            await base.Execute(progress, token);

            // Then clients can begin ...
            string solveRotateId = Guid.NewGuid().ToString();
            TSLogger.Info($"SYNC server informing clients of need for solve/rotate, id={solveRotateId}");
            progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for sync clients to accept solve/rotate" });
            await SyncServer.Instance.SyncSolveRotate(solveRotateId, target, planTarget.DatabaseId, syncActionTimeout, token);
            progress?.Report(new ApplicationStatus() { Status = "" });

            // And wait for them to complete
            TSLogger.Info($"SYNC server waiting for clients to complete solve/rotate, id={solveRotateId}");
            progress?.Report(new ApplicationStatus() { Status = "Target Scheduler: waiting for sync clients to complete solve/rotate" });
            await SyncServer.Instance.WaitForClientSolveRotateCompletion(solveRotateId, syncSolveRotateTimeout, token);
            progress?.Report(new ApplicationStatus() { Status = "" });
        }
    }
}
