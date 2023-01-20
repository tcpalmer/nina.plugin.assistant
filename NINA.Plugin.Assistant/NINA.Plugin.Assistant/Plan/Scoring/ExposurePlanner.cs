using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.NINAPlugin.Plan {

    public class ExposurePlanner {

        private DateTime atTime;
        private NighttimeCircumstances nighttimeCircumstances;
        private IPlanTarget planTarget;

        public ExposurePlanner(DateTime atTime, NighttimeCircumstances nighttimeCircumstances, IPlanTarget planTarget) {
            this.atTime = atTime;
            this.planTarget = planTarget;
            this.nighttimeCircumstances = nighttimeCircumstances;
        }

        /*
         The ideal plan ... 
           - uses any twilight in the window to image those filters that are OK with it
           - but also inserts stop triggers so that filters that can't are stopped when a dawn twilight approaches
           - obeys the FilterSwitchFrequency
         */

        public List<IPlanInstruction> Plan() {
            List<IPlanInstruction> instructions = new List<IPlanInstruction>();

            // Basically, walk the time interval ...

            // For each nighttime interval in the window ...
            //   - Civil dusk? schedule any images ok for that
            //   - Nautical dusk? schedule any images ok for that
            //   - Astro dusk? schedule any images ok for that
            //   - Night? schedule any images ok for that PLUS stop trigger
            //   - Astro dawn? schedule any images ok for that PLUS stop trigger
            //   - Nautical dawn? schedule any images ok for that PLUS stop trigger
            //   - Civil dawn? schedule any images ok for that PLUS stop trigger
            // 
            // 
            // 

            int twilightInclude = AssistantFilterPreferences.TWILIGHT_INCLUDE_CIVIL;
            if (SchedulePlanFiltersForWindow(twilightInclude, true)) {
                List<IPlanFilter> planFilters = GetPlanFiltersForWindow(twilightInclude);
                // build instruction list
            }

            return instructions;
        }

        private bool SchedulePlanFiltersForWindow(int twilightInclude, bool isDusk) {
            throw new NotImplementedException();
        }

        private List<IPlanFilter> GetPlanFiltersForWindow(int twilightInclude) {
            return planTarget.FilterPlans.Where(f => f.Preferences.TwilightInclude == twilightInclude).ToList();
        }
    }



    /* OLD CODE:
     * 
DateTime actualStartTime = planTarget.StartTime < DateTime.Now ? DateTime.Now : planTarget.StartTime;
int timeAvailable = (int)(planTarget.EndTime - actualStartTime).TotalSeconds;
int timeUsed = 0;

foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
    if (planFilter.Rejected) { continue; }

    int maxPossibleExposures = (int)((timeAvailable - timeUsed) / planFilter.ExposureLength);
    planFilter.PlannedExposures = Math.Min(planFilter.NeededExposures, maxPossibleExposures);
    timeUsed += planFilter.PlannedExposures * (int)planFilter.ExposureLength;

    if (timeUsed >= timeAvailable) {
        break;
    }
}

// Reject any that don't have exposures planned
foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
    if (planFilter.Rejected) { continue; }

    if (planFilter.PlannedExposures == 0) {
        planFilter.Rejected = true;
        planFilter.RejectedReason = Reasons.FilterNoExposuresPlanned;
    }
}

//  TODO: Pass 4 refines the order of filters/exposures for the selected target:
 //   - If the time span on the target includes twilight, order so that exposures with more restrictive twilight are not
//      scheduled during those periods. For example a NB filter set to image at astro twilight could be imaged during that time
 //     at dusk and dawn, but a WB filter could not and would require nighttime darkness before imaging.
 //   - Consideration may also be given to prioritizing filters with lower percent complete so that overall acquisition on a target is balanced.


return planTarget;
*/

    public interface IPlanInstruction {
        IPlanFilter planFilter { get; set; }
    }

    public class PlanInstruction : IPlanInstruction {
        public IPlanFilter planFilter { get; set; }

        public PlanInstruction(IPlanFilter planFilter) {
            this.planFilter = planFilter;
        }
    }

    public class PlanSwitchFilter : PlanInstruction {
        public PlanSwitchFilter(IPlanFilter planFilter) : base(planFilter) { }
    }

    public class PlanTakeExposure : PlanInstruction {
        public PlanTakeExposure(IPlanFilter planFilter) : base(planFilter) { }
    }

    public class PlanStopTrigger : PlanInstruction {
        public DateTime stopTime { get; set; }

        public PlanStopTrigger(IPlanFilter planFilter, DateTime stopTime) : base(planFilter) {
            this.stopTime = stopTime;
        }
    }

}
