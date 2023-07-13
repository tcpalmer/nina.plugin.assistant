using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class SafetyWatchDog {

        private bool SafetyCheckState; // true: check for becomes safe, false: check for becomes unsafe
        private InstructionContainer InstructionContainer;
        private ISequenceContainer parent;
        private SchedulerProgressVM SchedulerProgress;
        private ISafetyMonitorMediator safetyMonitor;
        private IProgress<ApplicationStatus> progress;
        private CancellationToken token;

        private bool? CurrentStatus = null;

        private readonly TimeSpan DELAY = TimeSpan.FromSeconds(5);

        public SafetyWatchDog(ISequenceContainer parent, SchedulerProgressVM schedulerProgress, bool safetyCheckState, InstructionContainer instructionContainer, ISafetyMonitorMediator safetyMonitor, IProgress<ApplicationStatus> progress, CancellationToken token) {
            this.parent = parent;
            SchedulerProgress = schedulerProgress;
            SafetyCheckState = safetyCheckState;
            InstructionContainer = instructionContainer;
            this.safetyMonitor = safetyMonitor;
            this.progress = progress;
            this.token = token;

            // TODO: set parent on instructionContainer
        }

        /* ACTIONS
         * 
         * When Becomes Safe
         *   - Execute instructions (be sure to reset when done)
         *   - Jump to start of TS
         * 
         * When Becomes Unsafe
         *   - Interrupt/Stop TS execution
         *   - Execute instructions (be sure to reset when done)
         *   - Implicit TS Condition: break out if no more targets
         * 
         *   Think that's all.  It will continue to check but do nothing else?  But what is shown in main TS UI to indicate not safe (some spinner)?
         */

        // TODO: These have to be established for both WAIT and TARGET plans
        // TODO: how to detect ONLY the 'becomes' state switch?
        // TODO: HOW TO TRIGGER/CANCEL OUTER??

        private ConditionWatchdog watchdog;

        public void Start() {
            CurrentStatus = GetSafetyStatus();
            // TODO: if we can't get status here, we might want to throw an exception and abort ... for safety

            watchdog = new ConditionWatchdog(CheckSafety, DELAY);
            watchdog.Start();
            TSLogger.Info($"started Safety Watchdog, start state: IsSafe={CurrentStatus}");
        }

        public void Cancel() {
            if (watchdog != null) {
                watchdog.Cancel();
                TSLogger.Info("stopped Safety Watchdog");
            }
        }

        private async Task CheckSafety() {
            await Task.Run(() => {
                bool? LatestStatus = GetSafetyStatus();

                if (LatestStatus.HasValue) {
                    if (LatestStatus != CurrentStatus) {
                        TSLogger.Info($"safety status changed to: IsSafe={LatestStatus}");
                    }
                    else {
                        // TODO: this is likely too chatty at every 5s
                        TSLogger.Info($"safety status no change: IsSafe={LatestStatus}");
                    }
                }
                else {
                    TSLogger.Warning("failed to get current safety status");
                }

                CurrentStatus = LatestStatus;
            });
        }

        private void RunInstructions() {
            SchedulerProgress.Add(InstructionContainer.Name);

            try {
                InstructionContainer.Execute(progress, token).Wait();
            }
            catch (Exception ex) {
                SchedulerProgress.End();
                TSLogger.Error($"exception executing {InstructionContainer.Name} instruction container: {ex}");

                if (ex is SequenceEntityFailedException) {
                    throw;
                }

                throw new SequenceEntityFailedException($"exception executing {InstructionContainer.Name} instruction container: {ex.Message}", ex);
            }

        }

        private bool? GetSafetyStatus() {
            try {
                var info = safetyMonitor.GetInfo();
                if (info.Connected) {
                    return info.IsSafe;
                }

                Notification.ShowError("Failed to get safety monitor status: not connected");
                TSLogger.Error("failed to get safety monitor status: not connected");
                return null;
            }
            catch (Exception ex) {
                Notification.ShowError("Failed to get safety monitor status");
                TSLogger.Error($"failed to get safety monitor status: {ex}");
                return null;
            }
        }
    }
}
