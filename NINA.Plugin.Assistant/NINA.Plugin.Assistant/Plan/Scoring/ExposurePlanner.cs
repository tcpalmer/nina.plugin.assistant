using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
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
    /// some planExposure in the planTarget.
    /// 
    /// At high latitudes near the summer solstice, you can lose true nighttime, astronomical twilight,
    /// and perhaps even nautical twilight.  We handle all cases for locations below the polar circle -
    /// even allowing imaging during civil twilight on the solstice if that's your thing.
    public class ExposurePlanner {

        private ProfilePreference profilePreferences;
        private IPlanTarget planTarget;
        private TimeInterval planInterval;
        private NighttimeCircumstances nighttimeCircumstances;
        public int filterSwitchFrequency;

        public ExposurePlanner(ProfilePreference profilePreferences, IPlanTarget planTarget, TimeInterval planInterval, NighttimeCircumstances nighttimeCircumstances) {
            this.profilePreferences = profilePreferences;
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

            return Cleanup(instructions);
        }

        public static List<IPlanInstruction> Cleanup(List<IPlanInstruction> instructions) {

            if (instructions is null || instructions.Count == 0) {
                return instructions;
            }

            // The instruction planning process can add spurious instructions - remove them

            for (int i = 0; i < instructions.Count - 1; i++) {
                IPlanInstruction i1 = instructions[i];
                IPlanInstruction i2 = instructions[i + 1];
                if (i1.GetType() == typeof(PlanSwitchFilter) && i2.GetType() == typeof(PlanSwitchFilter)) {
                    instructions.RemoveAt(i);
                }
            }

            IPlanInstruction last = instructions[instructions.Count - 1];
            if (last.GetType() == typeof(PlanSetReadoutMode)) {
                instructions.RemoveAt(instructions.Count - 1);
            }

            last = instructions[instructions.Count - 1];
            if (last.GetType() == typeof(PlanSwitchFilter)) {
                instructions.RemoveAt(instructions.Count - 1);
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
                    List<IPlanExposure> planExposures = GetPlanExposuresForTwilightLevel(twilightLevel);
                    List<IPlanInstruction> added = GetPlanInstructions(planExposures, overlap);

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

        private List<IPlanInstruction> GetPlanInstructions(List<IPlanExposure> planExposures, TimeInterval timeWindow) {
            List<IPlanInstruction> instructions = new List<IPlanInstruction>();
            long timeRemaining = timeWindow.Duration;
            string lastFilter = null;

            while (timeRemaining > 0) {
                if (AllPlanExposuresAreComplete(planExposures)) { break; }

                foreach (IPlanExposure planExposure in planExposures) {

                    if (IsPlanExposureComplete(planExposure)) { continue; }

                    if (planExposure.FilterName != lastFilter) {
                        instructions.Add(new PlanSwitchFilter(planExposure));
                        lastFilter = planExposure.FilterName;
                    }

                    // Since we don't know what readout mode the camera might have been left in, we have to always set it
                    instructions.Add(new PlanSetReadoutMode(planExposure));

                    // filterSwitchFrequency = zero -> take as many as possible per filter before switching
                    if (filterSwitchFrequency == 0) {
                        while (!IsPlanExposureComplete(planExposure)) {
                            timeRemaining -= (long)planExposure.ExposureLength;
                            if (timeRemaining <= 0) { break; }
                            instructions.Add(new PlanTakeExposure(planExposure));
                            planExposure.PlannedExposures++;
                        }
                    }
                    else {
                        // otherwise, take filterSwitchFrequency of this filter before switching
                        for (int i = 0; i < filterSwitchFrequency; i++) {
                            timeRemaining -= (long)planExposure.ExposureLength;
                            if (timeRemaining <= 0) { break; }
                            instructions.Add(new PlanTakeExposure(planExposure));
                            planExposure.PlannedExposures++;

                            if (IsPlanExposureComplete(planExposure)) { break; }
                        }
                    }

                    if (timeRemaining <= 0) { break; }
                }
            }

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

        private bool AllPlanExposuresAreComplete(List<IPlanExposure> planExposures) {
            foreach (IPlanExposure planExposure in planExposures) {
                if (!IsPlanExposureComplete(planExposure)) {
                    return false;
                }
            }

            return true;
        }

        private bool IsPlanExposureComplete(IPlanExposure planExposure) {
            double exposureThrottlePercentage = planTarget.Project.EnableGrader ? -1 : profilePreferences.ExposureThrottle;
            return planExposure.NeededExposures(exposureThrottlePercentage) <= planExposure.PlannedExposures;
        }

        private List<IPlanExposure> GetPlanExposuresForTwilightLevel(TwilightLevel twilightLevel) {
            return NightPrioritize(planTarget.ExposurePlans.Where(f => f.TwilightLevel >= twilightLevel && !f.Rejected).ToList(), twilightLevel);
        }

        private List<IPlanExposure> NightPrioritize(List<IPlanExposure> planExposures, TwilightLevel twilightLevel) {

            // If the twilight level is nighttime, order the filters to prioritize those for nighttime only.
            // Assuming there are also filters for brighter twilights, the nighttime only should be done first,
            // allowing the others to be done during (presumed future) brighter twilight levels.
            return twilightLevel == TwilightLevel.Nighttime ?
                planExposures.OrderBy(p => p.TwilightLevel).ToList() :
                planExposures;
        }
    }

    public interface IPlanInstruction {
        IPlanExposure planExposure { get; set; }
    }

    public class PlanInstruction : IPlanInstruction {
        public IPlanExposure planExposure { get; set; }

        public PlanInstruction(IPlanExposure planExposure) {
            this.planExposure = planExposure;
        }

        public static string InstructionsSummary(List<IPlanInstruction> instructions) {
            if (instructions?.Count == 0) {
                return "";
            }

            Dictionary<string, int> exposures = new Dictionary<string, int>();
            StringBuilder order = new StringBuilder();
            foreach (IPlanInstruction instruction in instructions) {
                if (instruction is PlanTakeExposure) {
                    string filterName = instruction.planExposure.FilterName;
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
        public PlanSwitchFilter(IPlanExposure planExposure) : base(planExposure) { }

        public override string ToString() {
            return $"SwitchFilter: {planExposure.FilterName}";
        }
    }

    public class PlanSetReadoutMode : PlanInstruction {
        public PlanSetReadoutMode(IPlanExposure planExposure) : base(planExposure) { }

        public override string ToString() {
            return $"Set readoutmode: mode={planExposure.ReadoutMode}";
        }
    }

    public class PlanTakeExposure : PlanInstruction {
        public PlanTakeExposure(IPlanExposure planExposure) : base(planExposure) { }

        public override string ToString() {
            return $"TakeExposure: {planExposure.FilterName} {planExposure.ExposureLength}";
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
