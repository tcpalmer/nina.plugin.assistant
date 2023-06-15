using Assistant.NINAPlugin.Astrometry;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class PlanStopTimeExpert {

        public PlanStopTimeExpert() { }

        /*
         * For now, we just use the meridian window end time or the project's minimum time in determining the stop time.
         * 
         * In the future, this could be expanded to consider other events of interest to determine a better stop time:
         * - Upcoming twilight transitions (there's a danger that the plan interval could be too short to do much of anything)
         * - Start time of MW for a target.  But if the current target is using a MW, you probably don't want to stop the current for the new one.
         * - The time that visibility begins for other targets?
         * - If selected target will cause a MF in the near future, we could stop before that (but danger that planner would just select it again)
         * 
         * Might also need new profile preferences to drive behavior here.
         */

        /// <summary>
        /// Determine an appropriate end time for the target plan window.
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="selectedTarget"></param>
        /// <param name="projects"></param>
        /// <returns></returns>
        public DateTime GetStopTime(DateTime startTime,
                                    IPlanTarget selectedTarget,
                                    List<IPlanProject> projects) {

            // If the selected target is using a meridian window, then use the window end time
            if (selectedTarget.Project.MeridianWindow > 0 && selectedTarget.MeridianWindow != null) {
                return selectedTarget.MeridianWindow.EndTime;
            }

            // Otherwise, use the startTime plus the project minimum time
            return startTime.AddMinutes(selectedTarget.Project.MinimumTime);
        }

        private List<IPlanTarget> GetTargets(List<IPlanProject> projects) {
            // Get the list of targets we want to check for other timing events
            throw new NotImplementedException();
        }
    }

    class StopEvent {

        const string TargetMinimumTime = "target minimum time";
        const string TargetMeridianWindowEnd = "target meridian window end";
        const string TwilightTransitionDarker = "twilight transition darker";
        const string TwilightTransitionLighter = "twilight transition lighter";

        public DateTime dateTime { get; private set; }
        public string description { get; private set; }

        public StopEvent(DateTime dateTime, string description) {
            this.dateTime = dateTime;
            this.description = description;
        }
    }

}
