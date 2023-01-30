using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    /// <summary>
    /// PlannerEmulator isolates the NINA Assistant sequence instructions from the Planner, allowing comprehensive
    /// testing of the sequencer operation without having to have an working database or running planner that relies
    /// on the current time, nighttime circumstances, etc.
    /// </summary>
    public class PlannerEmulator {

        private static int CallNumber = 0; // per NINA invocation

        private DateTime atTime;
        private IProfile profileService;

        public PlannerEmulator(DateTime atTime, IProfile profileService) {
            this.atTime = atTime;
            this.profileService = profileService;
        }

        public AssistantPlan GetPlan(IPlanTarget previousPlanTarget) {
            CallNumber++;

            if (CallNumber == 2) {
                return null;
            }

            return Plan1();
        }

        private AssistantPlan WaitForTime(DateTime waitFor) {
            return new AssistantPlan(waitFor);
        }

        private AssistantPlan Plan1() {
            DateTime endTime = atTime.AddMinutes(5);
            TimeInterval timeInterval = new TimeInterval(atTime, endTime);

            IPlanProject planProject = new PlanProjectEmulator();
            planProject.PlanId = Guid.NewGuid().ToString();
            planProject.Name = "P01";
            planProject.Preferences = GetProjectPreferences(true, 0);
            IPlanTarget planTarget = GetBasePlanTarget("T01", planProject, Cp5n5);
            planTarget.EndTime = endTime;
            IPlanFilter lum = GetPlanFilter("Lum", 6, null, null, 3);

            List<IPlanInstruction> instructions = new List<IPlanInstruction>();
            instructions.Add(new PlanMessage("planner emulator: Plan1"));
            instructions.Add(new PlanSwitchFilter(lum));
            instructions.Add(new PlanTakeExposure(lum));
            instructions.Add(new PlanTakeExposure(lum));

            return new AssistantPlan(planTarget, timeInterval, instructions);
        }

        private IPlanFilter GetPlanFilter(string name, int exposure, int? gain, int? offset, int desired) {
            PlanFilterEmulator planFilter = new PlanFilterEmulator();
            planFilter.PlanId = Guid.NewGuid().ToString();
            planFilter.FilterName = name;
            planFilter.ExposureLength = exposure;
            planFilter.Gain = gain;
            planFilter.Offset = offset;
            planFilter.BinningMode = new BinningMode(1, 1);
            planFilter.Desired = desired;
            return planFilter;
        }

        private AssistantProjectPreferences GetProjectPreferences(bool enableGrader, int ditherEvery) {
            AssistantProjectPreferences pp = new AssistantProjectPreferences();
            pp.SetDefaults();
            pp.MinimumAltitude = 10;
            pp.EnableGrader = enableGrader;
            pp.DitherEvery = ditherEvery;
            return pp;
        }

        private IPlanTarget GetBasePlanTarget(string name, IPlanProject planProject, Coordinates coordinates) {
            IPlanTarget planTarget = new PlanTargetEmulator();
            planTarget.PlanId = Guid.NewGuid().ToString();
            planTarget.Project = planProject;
            planTarget.Name = name;
            planTarget.Coordinates = coordinates;
            planTarget.Rotation = 0;
            return planTarget;
        }

        public static readonly Coordinates Cp5n5 = new Coordinates(AstroUtil.HMSToDegrees("5:0:0"), AstroUtil.DMSToDegrees("-5:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);
    }

    class PlanProjectEmulator : IPlanProject {

        public string PlanId { get; set; }
        public string Name { get; set; }
        public AssistantProjectPreferences Preferences { get; set; }

        public int DatabaseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Description { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int State { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Priority { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime CreateDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? ActiveDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? InactiveDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? StartDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime? EndDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IPlanTarget> Targets { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public HorizonDefinition HorizonDefinition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Rejected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RejectedReason { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    class PlanTargetEmulator : IPlanTarget {

        public string PlanId { get; set; }
        public string Name { get; set; }
        public IPlanProject Project { get; set; }
        public Coordinates Coordinates { get; set; }
        public double Rotation { get; set; }
        public double ROI { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int DatabaseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Epoch Epoch { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<IPlanFilter> FilterPlans { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Rejected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RejectedReason { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime CulminationTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void SetCircumstances(TargetCircumstances targetCircumstances) {
            throw new NotImplementedException();
        }
    }

    class PlanFilterEmulator : IPlanFilter {

        public string PlanId { get; set; }
        public string FilterName { get; set; }
        public double ExposureLength { get; set; }
        public int? Gain { get; set; }
        public int? Offset { get; set; }
        public BinningMode BinningMode { get; set; }
        public int? ReadoutMode { get; set; }
        public int Desired { get; set; }
        public int Acquired { get; set; }
        public int Accepted { get; set; }
        public IPlanTarget PlanTarget { get; set; }
        public AssistantFilterPreferences Preferences { get; set; }

        public int DatabaseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool Rejected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string RejectedReason { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int PlannedExposures { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsIncomplete() {
            throw new NotImplementedException();
        }

        public int NeededExposures() {
            throw new NotImplementedException();
        }
    }
}
