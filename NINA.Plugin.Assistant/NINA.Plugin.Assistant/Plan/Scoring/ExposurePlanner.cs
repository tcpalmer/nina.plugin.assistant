using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assistant.NINAPlugin.Plan {

    /// <summary>
    /// Plan the exposures for a target.
    /// 
    /// The planInterval is the actual interval we want to image for.  So it already takes into account
    /// a potential delay at the start as well as an end time that reflects when we either must stop
    /// (due to horizon, meridian or twilight) or we want to stop to let the full planner run again.
    /// Although it may cover periods of twilight, we're guaranteed that the window is appropriate for
    /// some planFilter in the planTarget.
    /// 
    /// At high latitudes near the summer solstice, you can lose true nighttime, astronomical twilight,
    /// and perhaps even nautical twilight.  We handle all cases for locations below the polar circle -
    /// even allowing imaging during civil twilight on the solstice if that's your thing.
    public class ExposurePlanner {

        private IPlanTarget planTarget;
        private TimeInterval planInterval;
        private NighttimeCircumstances nighttimeCircumstances;
        public int filterSwitchFrequency;

        public ExposurePlanner(IPlanTarget planTarget, TimeInterval planInterval, NighttimeCircumstances nighttimeCircumstances) {
            this.planTarget = planTarget;
            this.planInterval = planInterval;
            this.nighttimeCircumstances = nighttimeCircumstances;
            this.filterSwitchFrequency = planTarget.Project.FilterSwitchFrequency;
        }

        public List<IPlanInstruction> Plan() {
            List<IPlanInstruction> instructions = new List<IPlanInstruction> {
                new PlanMessage($"start exposure plan for target {planTarget.Name} in window: {planInterval}")
            };

            // Nighttime: civil but no nautical twilight
            if (nighttimeCircumstances.HasCivilTwilight() && !nighttimeCircumstances.HasNauticalTwilight()) {
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dusk));
            }
            // Nighttime: nautical but no astronomical twilight
            else if (nighttimeCircumstances.HasNauticalTwilight() && !nighttimeCircumstances.HasAstronomicalTwilight()) {
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nautical, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dawn));
            }
            // Nighttime: astronomical but no nighttime
            else if (nighttimeCircumstances.HasAstronomicalTwilight() && !nighttimeCircumstances.HasNighttime()) {
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nautical, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Astronomical, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nautical, TwilightStage.Dawn));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dawn));
            }
            // Nighttime: true nighttime
            else {
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nautical, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Astronomical, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nighttime, TwilightStage.Dusk));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Astronomical, TwilightStage.Dawn));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Nautical, TwilightStage.Dawn));
                instructions.AddRange(GetPlanInstructionsForTwilight(TwilightLevel.Civil, TwilightStage.Dawn));
            }

            if (!HasActionableInstructions(instructions)) {
                instructions.Clear();
            }

            return instructions;
        }

        private List<IPlanInstruction> GetPlanInstructionsForTwilight(TwilightLevel twilightLevel, TwilightStage twilightStage) {
            TimeInterval twilightWindow = nighttimeCircumstances.GetTwilightWindow(twilightLevel, twilightStage);

            List<IPlanInstruction> instructions = new List<IPlanInstruction> {
                new PlanMessage($"exposure plan for twilight period {twilightLevel} {twilightStage}: {twilightWindow}")
            };

            if (twilightWindow != null) {
                TimeInterval overlap = twilightWindow.Overlap(planInterval);
                if (overlap != null && overlap.Duration > 0) {
                    List<IPlanExposure> planFilters = GetPlanFiltersForTwilightLevel(twilightLevel);
                    List<IPlanInstruction> added = GetPlanInstructions(planFilters, overlap);

                    if (HasActionableInstructions(added)) {
                        instructions.AddRange(added);
                    }
                }
            }

            if (!HasActionableInstructions(instructions)) {
                instructions.Clear();
            }

            return instructions;
        }

        private List<IPlanInstruction> GetPlanInstructions(List<IPlanExposure> planFilters, TimeInterval timeWindow) {
            List<IPlanInstruction> instructions = new List<IPlanInstruction>();
            long timeRemaining = timeWindow.Duration;
            string lastFilter = null;

            while (timeRemaining > 0) {
                if (AllPlanFiltersAreComplete(planFilters)) { break; }

                foreach (IPlanExposure planFilter in planFilters) {

                    if (IsPlanFilterComplete(planFilter)) { continue; }

                    if (planFilter.FilterName != lastFilter) {
                        instructions.Add(new PlanSwitchFilter(planFilter));
                        lastFilter = planFilter.FilterName;
                    }

                    // Since we don't know what readout mode the camera might have been left in, we have to always set it
                    instructions.Add(new PlanSetReadoutMode(planFilter));

                    // filterSwitchFrequency = zero -> take as many as possible per filter before switching
                    if (filterSwitchFrequency == 0) {
                        while (!IsPlanFilterComplete(planFilter)) {
                            timeRemaining -= (long)planFilter.ExposureLength;
                            if (timeRemaining <= 0) { break; }
                            instructions.Add(new PlanTakeExposure(planFilter));
                            planFilter.PlannedExposures++;
                        }
                    }
                    else {
                        // otherwise, take filterSwitchFrequency of this filter before switching
                        for (int i = 0; i < filterSwitchFrequency; i++) {
                            timeRemaining -= (long)planFilter.ExposureLength;
                            if (timeRemaining <= 0) { break; }
                            instructions.Add(new PlanTakeExposure(planFilter));
                            planFilter.PlannedExposures++;

                            if (IsPlanFilterComplete(planFilter)) { break; }
                        }
                    }

                    if (timeRemaining <= 0) { break; }
                }
            }

            /* TODO: I don't think we want to do this.  If the exposure container finishes early, just let the instruction come back to the planner.
             * If the planner says wait then, that's fine - it will know the time to wait until.
             * TODO: and I think we can remove PlanWait instruction if it's not needed here.
            if (timeRemaining >= 0) {
                instructions.Add(new PlanWait(timeWindow.EndTime));
            }*/

            return instructions;
        }

        private bool HasActionableInstructions(List<IPlanInstruction> instructions) {
            if (instructions == null || instructions.Count == 0) { return false; }

            foreach (IPlanInstruction instruction in instructions) {
                if (!(instruction is PlanMessage)) {
                    return true;
                }
            }

            return false;
        }

        private bool AllPlanFiltersAreComplete(List<IPlanExposure> planFilters) {
            foreach (IPlanExposure planFilter in planFilters) {
                if (!IsPlanFilterComplete(planFilter)) {
                    return false;
                }
            }

            return true;
        }

        private bool IsPlanFilterComplete(IPlanExposure planFilter) {
            return planFilter.NeededExposures() <= planFilter.PlannedExposures;
        }

        private List<IPlanExposure> GetPlanFiltersForTwilightLevel(TwilightLevel twilightLevel) {
            return NightPrioritize(planTarget.ExposurePlans.Where(f => f.TwilightLevel >= twilightLevel).ToList(), twilightLevel);
        }

        private List<IPlanExposure> NightPrioritize(List<IPlanExposure> planFilters, TwilightLevel twilightLevel) {

            // If the twilight level is nighttime, order the filters to prioritize those for nighttime only.
            // Assuming there are also filters for brighter twilights, the nighttime only should be done first,
            // allowing the others to be done during (presumed future) brighter twilight levels.
            return twilightLevel == TwilightLevel.Nighttime ?
                planFilters.OrderBy(p => p.TwilightLevel).ToList() :
                planFilters;
        }
    }

    public interface IPlanInstruction {
        IPlanExposure planFilter { get; set; }
    }

    public class PlanInstruction : IPlanInstruction {
        public IPlanExposure planFilter { get; set; }

        public PlanInstruction(IPlanExposure planFilter) {
            this.planFilter = planFilter;
        }

        public static string InstructionsSummary(List<IPlanInstruction> instructions) {
            if (instructions?.Count == 0) {
                return "";
            }

            Dictionary<string, int> exposures = new Dictionary<string, int>();
            StringBuilder order = new StringBuilder();
            foreach (IPlanInstruction instruction in instructions) {
                if (instruction is PlanTakeExposure) {
                    string filterName = instruction.planFilter.FilterName;
                    order.Append(filterName);
                    if (exposures.ContainsKey(filterName)) {
                        exposures[filterName]++;
                    }
                    else {
                        exposures.Add(filterName, 1);
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Order: {order}");
            foreach (KeyValuePair<string, int> entry in exposures) {
                sb.AppendLine($"{entry.Key}: {entry.Value}");
            }

            foreach (IPlanInstruction instruction in instructions) {
                if (instruction is PlanWait) {
                    sb.AppendLine($"Wait until {Utils.FormatDateTimeFull(((PlanWait)instruction).waitForTime)}");
                }
            }

            return sb.ToString();
        }
    }

    public class PlanMessage : PlanInstruction {
        public string msg { get; set; }

        public PlanMessage(string msg) : base(null) {
            this.msg = msg;
        }

        public override string ToString() {
            return $"Message: {msg}";
        }
    }

    public class PlanSlew : PlanInstruction {
        public bool center { get; private set; }

        public PlanSlew(bool center) : base(null) {
            this.center = center;
        }

        public override string ToString() {
            return $"Slew: and center={center}";
        }
    }

    public class PlanSwitchFilter : PlanInstruction {
        public PlanSwitchFilter(IPlanExposure planFilter) : base(planFilter) { }

        public override string ToString() {
            return $"SwitchFilter: {planFilter.FilterName}";
        }
    }

    public class PlanSetReadoutMode : PlanInstruction {
        public PlanSetReadoutMode(IPlanExposure planFilter) : base(planFilter) { }

        public override string ToString() {
            return $"Set readoutmode: mode={planFilter.ReadoutMode}";
        }
    }

    public class PlanTakeExposure : PlanInstruction {
        public PlanTakeExposure(IPlanExposure planFilter) : base(planFilter) { }

        public override string ToString() {
            return $"TakeExposure: {planFilter.FilterName} {planFilter.ExposureLength}";
        }
    }

    public class PlanWait : PlanInstruction {
        public DateTime waitForTime { get; set; }

        public PlanWait(DateTime waitForTime) : base(null) {
            this.waitForTime = waitForTime;
        }

        public override string ToString() {
            return $"Wait: {Utils.FormatDateTimeFull(waitForTime)}";
        }
    }

}
