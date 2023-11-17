using Assistant.NINAPlugin.Util;
using NINA.Plugin.Assistant.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Plan {

    public class PlanStopTimeExpert {

        public PlanStopTimeExpert() { }

        /// <summary>
        /// Determine an appropriate end time for the target plan window.
        /// </summary>
        /// <param name="useSmartPlanWindow"></param>
        /// <param name="startTime"></param>
        /// <param name="selectedTarget"></param>
        /// <param name="projects"></param>
        /// <returns></returns>
        public DateTime GetStopTime(bool useSmartPlanWindow, DateTime startTime,
                                    IPlanTarget selectedTarget,
                                    List<IPlanProject> projects) {

            // If the selected target is using a meridian window, then use the window end time
            if (selectedTarget.Project.MeridianWindow > 0 && selectedTarget.MeridianWindow != null) {
                TSLogger.Debug($"stop time determined by end of meridian window: {Utils.FormatDateTimeFull(selectedTarget.MeridianWindow.EndTime)}");
                return selectedTarget.MeridianWindow.EndTime;
            }

            // If we're not using the smart method, then stop time is just startTime plus the project minimum time
            if (!useSmartPlanWindow) {
                DateTime minimumTime = startTime.AddMinutes(selectedTarget.Project.MinimumTime);
                TSLogger.Debug($"stop time determined simply by project minimum time: {Utils.FormatDateTimeFull(minimumTime)}");
                return minimumTime;
            }

            // Otherwise, examine potential future targets to see if we can extend the stop time of the
            // selected target with no impact ...
            DateTime? futureTargetStart = GetFutureTargetStartTime(startTime, selectedTarget, projects);
            if (futureTargetStart.HasValue) {
                return futureTargetStart.Value;
            }

            // Otherwise, just let it run until its end time
            return selectedTarget.EndTime;
        }

        private DateTime? GetFutureTargetStartTime(DateTime startTime, IPlanTarget selectedTarget, List<IPlanProject> projects) {
            List<StopEvent> futureEvents = GetFutureTargetEvents(selectedTarget, projects);
            DateTime minimumTime = startTime.AddMinutes(selectedTarget.Project.MinimumTime);

            foreach (StopEvent stopEvent in futureEvents) {

                if (stopEvent.dateTime < minimumTime) {

                    // No future target could start before the minimum time window of the selected target.  But
                    // some (e.g. those that could have been selected now but had lower scores) could have started
                    // before that time.  We need to consider them but only if they have a shot at being selected
                    // in the future (based on that target's minimum time and hard stop time).

                    if (minimumTime.AddMinutes(stopEvent.target.Project.MinimumTime) <= stopEvent.target.EndTime) {
                        TSLogger.Debug($"smart plan window: found concurrent potential target ({stopEvent.target.Name}), using minimum: {Utils.FormatDateTimeFull(minimumTime)}");
                        return minimumTime;
                    }

                    continue;
                }

                TSLogger.Debug($"smart plan window: found future potential target start time: {stopEvent.target.Name} at {Utils.FormatDateTimeFull(stopEvent.dateTime)}");
                return stopEvent.dateTime;
            }

            TSLogger.Debug($"smart plan window: no future potential targets, using selected end time: {selectedTarget.EndTime}");
            return null;
        }

        private List<StopEvent> GetFutureTargetEvents(IPlanTarget selectedTarget, List<IPlanProject> projects) {
            List<StopEvent> futureEvents = new List<StopEvent>();

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) {
                        switch (planTarget.RejectedReason) {
                            case Reasons.TargetNotYetVisible:
                            case Reasons.TargetLowerScore:
                                futureEvents.Add(new StopEvent(planTarget.StartTime, StopEvent.FutureTargetStartTime, planTarget));
                                break;
                            case Reasons.TargetBeforeMeridianWindow:
                                futureEvents.Add(new StopEvent(planTarget.StartTime, StopEvent.FutureTargetMWStartTime, planTarget));
                                break;
                        }
                    }
                }
            }

            futureEvents.Sort();
            TSLogger.Debug($"Potential future events to determine stop time:\n{LogStopEvents(futureEvents)}");

            return futureEvents;
        }

        private string LogStopEvents(List<StopEvent> stopEvents) {
            StringBuilder sb = new StringBuilder();
            foreach (StopEvent stopEvent in stopEvents) {
                sb.AppendLine(stopEvent.ToString());
            }

            return sb.ToString();
        }
    }

    class StopEvent : IComparable<StopEvent> {

        public const string FutureTargetStartTime = "future target start time";
        public const string FutureTargetMWStartTime = "future target meridian window start time";
        public const string TargetMinimumTime = "target minimum time";
        public const string TargetMeridianWindowEnd = "target meridian window end";
        public const string TwilightTransitionDarker = "twilight transition darker";
        public const string TwilightTransitionLighter = "twilight transition lighter";

        public DateTime dateTime { get; private set; }
        public string description { get; private set; }
        public IPlanTarget target { get; private set; }

        public StopEvent(DateTime dateTime, string description, IPlanTarget target) {
            this.dateTime = dateTime;
            this.description = description;
            this.target = target;
        }

        public int CompareTo(StopEvent other) {
            if (other == null) return 1;
            return dateTime.CompareTo(other.dateTime);
        }

        public override string ToString() {
            return $"{Utils.FormatDateTimeFull(dateTime)} {description} {target.Name}";
        }
    }

}
