using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Plan {

    public class AssistantPlan {
        public string PlanId { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public IPlanTarget PlanTarget { get; private set; }
        public List<IPlanInstruction> PlanInstructions { get; private set; }
        public DateTime? WaitForNextTargetTime { get; private set; }

        public AssistantPlan(IPlanTarget planTarget, TimeInterval timeInterval, List<IPlanInstruction> planInstructions) {
            this.PlanId = Guid.NewGuid().ToString();
            this.PlanTarget = planTarget;
            this.TimeInterval = timeInterval;
            this.PlanInstructions = planInstructions;
            this.WaitForNextTargetTime = null;
        }

        public AssistantPlan(DateTime waitForNextTargetTime) {
            this.PlanId = Guid.NewGuid().ToString();
            this.WaitForNextTargetTime = waitForNextTargetTime;
        }

        public string PlanSummary() {
            StringBuilder sb = new StringBuilder();
            if (WaitForNextTargetTime != null) {
                sb.AppendLine($"Waiting until {Utils.FormatDateTimeFull(WaitForNextTargetTime)}");
            }
            else {
                sb.AppendLine($"Target:         {PlanTarget.Name} at {PlanTarget.Coordinates.RAString} {PlanTarget.Coordinates.DecString}");
                sb.AppendLine($"Imaging window: {TimeInterval}");
                sb.Append($"Instructions:   {PlanInstruction.InstructionsSummary(PlanInstructions)}");
            }

            return sb.ToString();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id: {PlanId}");
            sb.Append("Target: ").AppendLine(PlanTarget != null ? PlanTarget.Name : null);
            sb.AppendLine($"Interval: {TimeInterval}");
            sb.AppendLine($"Wait: {WaitForNextTargetTime}");
            sb.AppendLine($"Instructions:\n");
            if (PlanInstructions != null) {
                foreach (IPlanInstruction instruction in PlanInstructions) {
                    sb.AppendLine($"{instruction}");
                }
            }

            return sb.ToString();
        }
    }

    public interface IPlanProject {
        string PlanId { get; set; }
        int DatabaseId { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        int State { get; set; }
        int Priority { get; set; }
        DateTime CreateDate { get; set; }
        DateTime? ActiveDate { get; set; }
        DateTime? InactiveDate { get; set; }
        DateTime? StartDate { get; set; }
        DateTime? EndDate { get; set; }
        List<IPlanTarget> Targets { get; set; }
        AssistantProjectPreferences Preferences { get; set; }
        HorizonDefinition HorizonDefinition { get; set; }
        bool Rejected { get; set; }
        string RejectedReason { get; set; }

        string ToString();
    }

    public class PlanProject : IPlanProject {

        public string PlanId { get; set; }
        public int DatabaseId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int State { get; set; }
        public int Priority { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? InactiveDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<IPlanTarget> Targets { get; set; }
        public AssistantProjectPreferences Preferences { get; set; }
        public HorizonDefinition HorizonDefinition { get; set; }
        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }

        public PlanProject(IProfile profile, Project project, Dictionary<string, AssistantFilterPreferences> filterPreferences) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = project.id;
            this.Name = project.name;
            this.Description = project.description;
            this.State = project.state;
            this.Priority = project.priority;
            this.CreateDate = project.CreateDate;
            this.ActiveDate = project.ActiveDate;
            this.InactiveDate = project.InactiveDate;
            this.StartDate = project.StartDate;
            this.EndDate = project.EndDate;
            this.Preferences = project.ProjectPreferences;
            this.HorizonDefinition = DetermineHorizon(profile, project.ProjectPreferences);
            this.Rejected = false;

            Targets = new List<IPlanTarget>();
            foreach (Target target in project.targets) {
                Targets.Add(new PlanTarget(this, target, filterPreferences));
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- Project:");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"State: {Project.State(State)}");
            sb.AppendLine($"Priority: {Project.Priority(Priority)}");
            sb.AppendLine($"StartDate: {Utils.FormatDateTimeFull(StartDate)}");
            sb.AppendLine($"EndDate: {Utils.FormatDateTimeFull(EndDate)}");
            sb.AppendLine($"Horizon: {HorizonDefinition}");
            sb.AppendLine($"Rejected: {Rejected}");
            sb.AppendLine($"RejectedReason: {RejectedReason}");
            sb.AppendLine($"Preferences:\n{Preferences}");

            sb.AppendLine("-- Targets:");
            foreach (PlanTarget planTarget in Targets) {
                sb.AppendLine(planTarget.ToString());
            }

            return sb.ToString();
        }

        public static string ListToString(List<IPlanProject> list) {
            if (list == null || list.Count == 0) {
                return "no projects";
            }

            StringBuilder sb = new StringBuilder();
            foreach (IPlanProject planProject in list) {
                sb.AppendLine(planProject.ToString());
            }

            return sb.ToString();
        }

        private HorizonDefinition DetermineHorizon(IProfile profile, AssistantProjectPreferences projectPreferences) {
            if (projectPreferences.UseCustomHorizon) {
                return new HorizonDefinition(profile.AstrometrySettings.Horizon, projectPreferences.HorizonOffset);
            }

            return new HorizonDefinition(projectPreferences.MinimumAltitude);
        }
    }

    public interface IPlanTarget {
        string PlanId { get; set; }
        int DatabaseId { get; set; }
        string Name { get; set; }
        Coordinates Coordinates { get; set; }
        Epoch Epoch { get; set; }
        double Rotation { get; set; }
        double ROI { get; set; }
        List<IPlanFilter> FilterPlans { get; set; }
        IPlanProject Project { get; set; }
        bool Rejected { get; set; }
        string RejectedReason { get; set; }
        DateTime StartTime { get; set; }
        DateTime EndTime { get; set; }
        DateTime CulminationTime { get; set; }

        void SetCircumstances(TargetCircumstances targetCircumstances);
        string ToString();
    }

    public class PlanTarget : IPlanTarget {

        public string PlanId { get; set; }
        public int DatabaseId { get; set; }
        public string Name { get; set; }
        public Coordinates Coordinates { get; set; }
        public Epoch Epoch { get; set; }
        public double Rotation { get; set; }
        public double ROI { get; set; }
        public List<IPlanFilter> FilterPlans { get; set; }
        public IPlanProject Project { get; set; }
        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CulminationTime { get; set; }

        public PlanTarget(IPlanProject planProject, Target target, Dictionary<string, AssistantFilterPreferences> filterPreferences) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = target.id;
            this.Name = target.name;
            this.Coordinates = new Coordinates(Angle.ByHours(target.ra), Angle.ByDegree(target.dec), target.Epoch);
            this.Epoch = target.Epoch;
            this.Rotation = target.rotation;
            this.ROI = target.roi;
            this.Project = planProject;
            this.Rejected = false;

            this.FilterPlans = new List<IPlanFilter>();
            foreach (FilterPlan plan in target.filterplans) {
                AssistantFilterPreferences filterPrefs = filterPreferences[plan.filterName];
                PlanFilter planFilter = new PlanFilter(this, plan, filterPrefs);

                // add only if the plan is incomplete
                if (planFilter.IsIncomplete()) {
                    this.FilterPlans.Add(planFilter);
                }
            }
        }

        public void SetCircumstances(TargetCircumstances targetCircumstances) {
            if (targetCircumstances.IsVisible) {
                StartTime = targetCircumstances.RiseAboveHorizonTime;
                EndTime = targetCircumstances.SetBelowHorizonTime;
                CulminationTime = targetCircumstances.CulminationTime;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id: {PlanId}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Coords: {Coordinates.RAString} {Coordinates.DecString} {Epoch}");
            sb.AppendLine($"Rotation: {Rotation}");
            sb.AppendLine($"ROI: {ROI}");
            sb.AppendLine($"StartTime: {Utils.FormatDateTimeFull(StartTime)}");
            sb.AppendLine($"EndTime: {Utils.FormatDateTimeFull(EndTime)}");
            sb.AppendLine($"CulminationTime: {Utils.FormatDateTimeFull(CulminationTime)}");
            sb.AppendLine($"Rejected: {Rejected}");
            sb.AppendLine($"RejectedReason: {RejectedReason}");

            sb.AppendLine("-- FilterPlans:");
            foreach (PlanFilter planFilter in FilterPlans) {
                sb.AppendLine(planFilter.ToString());
            }

            return sb.ToString();
        }

        public override bool Equals(object obj) {
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            }

            PlanTarget p = (PlanTarget)obj;
            return this.Name.Equals(p.Name) &&
                   this.Coordinates.RA.Equals(p.Coordinates.RA) &&
                   this.Coordinates.Dec.Equals(p.Coordinates.Dec) &&
                   this.Rotation.Equals(p.Rotation);
        }

        public override int GetHashCode() {
            int hash = 17;
            hash = hash * 23 + this.Coordinates.RAString.GetHashCode();
            hash = hash * 23 + this.Coordinates.DecString.GetHashCode();
            hash = hash * 23 + this.Rotation.ToString().GetHashCode();
            return hash;
        }
    }

    public interface IPlanFilter {
        string PlanId { get; set; }
        int DatabaseId { get; set; }
        string FilterName { get; set; }
        double ExposureLength { get; set; }
        int? Gain { get; set; }
        int? Offset { get; set; }
        BinningMode BinningMode { get; set; }
        int? ReadoutMode { get; set; }
        int Desired { get; set; }
        int Acquired { get; set; }
        int Accepted { get; set; }
        IPlanTarget PlanTarget { get; set; }
        AssistantFilterPreferences Preferences { get; set; }
        bool Rejected { get; set; }
        string RejectedReason { get; set; }
        int PlannedExposures { get; set; }

        int NeededExposures();
        bool IsIncomplete();
        string ToString();
    }

    public class PlanFilter : IPlanFilter {

        public string PlanId { get; set; }
        public int DatabaseId { get; set; }
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
        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }

        public int PlannedExposures { get; set; }

        public PlanFilter(IPlanTarget planTarget, FilterPlan filterPlan, AssistantFilterPreferences preferences) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = filterPlan.id;
            this.FilterName = filterPlan.filterName;
            this.ExposureLength = filterPlan.exposure;
            this.Gain = filterPlan.gain;
            this.Offset = filterPlan.offset;
            this.BinningMode = new BinningMode((short)filterPlan.bin, (short)filterPlan.bin);
            this.ReadoutMode = filterPlan.readoutMode;
            this.Desired = filterPlan.desired;
            this.Acquired = filterPlan.acquired;
            this.Accepted = filterPlan.accepted;
            this.PlanTarget = planTarget;
            this.Preferences = preferences;
            this.Rejected = false;
            this.PlannedExposures = 0;
        }

        public int NeededExposures() { return Accepted > Desired ? 0 : Desired - Accepted; }
        public bool IsIncomplete() { return Accepted < Desired; }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id: {PlanId}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"ExposureLength: {ExposureLength}");
            sb.AppendLine($"Gain: {Gain}");
            sb.AppendLine($"Offset: {Offset}");
            sb.AppendLine($"Bin: {BinningMode}");
            sb.AppendLine($"ReadoutMode: {ReadoutMode}");
            sb.AppendLine($"Desired: {Desired}");
            sb.AppendLine($"Acquired: {Acquired}");
            sb.AppendLine($"Accepted: {Accepted}");
            sb.AppendLine($"PlannedExposures: {PlannedExposures}");
            sb.AppendLine($"Rejected: {Rejected}");
            sb.AppendLine($"RejectedReason: {RejectedReason}");
            sb.AppendLine($"Preferences:\n{Preferences}");
            return sb.ToString();
        }
    }

    public class Reasons {

        public const string ProjectComplete = "complete";
        public const string ProjectNoVisibleTargets = "no visible targets";
        public const string ProjectMoonAvoidance = "moon avoidance";
        public const string ProjectAllTargets = "all targets rejected";

        public const string TargetComplete = "complete";
        public const string TargetNeverRises = "never rises at location";
        public const string TargetNotVisible = "not visible at this time";
        public const string TargetNotYetVisible = "not yet visible at this time";
        public const string TargetMoonAvoidance = "moon avoidance";
        public const string TargetAllFilterPlans = "all filter plans rejected";

        public const string FilterComplete = "complete";
        public const string FilterMoonAvoidance = "moon avoidance";
        public const string FilterNoExposuresPlanned = "no exposures planned";

        private Reasons() { }

    }
}
