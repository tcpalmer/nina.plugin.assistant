using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using Assistant.NINAPlugin.Sequencer;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Plan {

    public class SchedulerPlan {
        public string PlanId { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public DateTime PlanTime { get; private set; }
        public List<IPlanProject> Projects { get; private set; }
        public IPlanTarget PlanTarget { get; private set; }
        public List<IPlanInstruction> PlanInstructions { get; private set; }
        public DateTime? WaitForNextTargetTime { get; private set; }
        public bool IsEmulator { get; set; }
        public string DetailsLog { get; private set; }

        public SchedulerPlan(DateTime planTime, List<IPlanProject> projects, IPlanTarget planTarget, TimeInterval timeInterval, List<IPlanInstruction> planInstructions, bool logPlan) {
            this.PlanId = Guid.NewGuid().ToString();
            this.PlanTime = planTime;
            this.Projects = projects;
            this.PlanTarget = planTarget;
            this.TimeInterval = timeInterval;
            this.PlanInstructions = planInstructions;
            this.WaitForNextTargetTime = null;

            if (logPlan) {
                string log = LogPlanResults();
                DetailsLog = DetailsLog + log;
                TSLogger.Info(log);
            }
        }

        public SchedulerPlan(DateTime planTime, List<IPlanProject> projects, DateTime waitForNextTargetTime, bool logPlan) {
            this.PlanId = Guid.NewGuid().ToString();
            this.PlanTime = planTime;
            this.Projects = projects;
            this.WaitForNextTargetTime = waitForNextTargetTime;

            if (logPlan) {
                string log = LogPlanResults();
                DetailsLog = DetailsLog + log;
                TSLogger.Info(log);
            }
        }

        public string LogPlanResults() {
            StringBuilder sb = new StringBuilder();
            string type = WaitForNextTargetTime != null ? "WAIT" : "TARGET";

            sb.AppendLine("\n" + String.Format("{0,-6}", type) + " ==========================================================================================");

            if (type == "WAIT") {
                sb.AppendLine($"Plan Start:      {DateFmt(PlanTime)}");
                sb.AppendLine($"Wait Until:      {DateFmt(WaitForNextTargetTime)}");
            }

            if (type == "TARGET") {
                sb.AppendLine($"Selected Target: {PlanTarget.Project.Name}/{PlanTarget.Name}");
                sb.AppendLine($"Plan Start:      {DateFmt(PlanTime)}");
                sb.AppendLine($"Plan Stop:       {DateFmt(PlanTime.AddSeconds(TimeInterval.Duration))}");
                sb.AppendLine($"Hard Stop:       {DateFmt(PlanTarget.EndTime)} (target sets)");
            }

            bool haveScoringRuns = false;
            bool hasAllEPsRejected = false;

            if (Projects != null) {
                sb.AppendLine(String.Format("\n{0,-40} {1,-27} {2,6}   {3,19}", "TARGETS CONSIDERED", "REJECTED FOR", "SCORE", "POTENTIAL START"));
                foreach (IPlanProject project in Projects) {
                    foreach (IPlanTarget target in project.Targets) {
                        string score = "";
                        string startTime = GetStartTime(target);

                        if (target.ScoringResults != null && target.ScoringResults.Results.Count > 0) {
                            haveScoringRuns = true;
                            score = String.Format("{0:0.00}", target.ScoringResults.TotalScore * ScoringRule.WEIGHT_SCALE);
                        }

                        sb.AppendLine(String.Format("{0,-40} {1,-27} {2,6}   {3}", $"{project.Name}/{target.Name}", target.RejectedReason, score, startTime));
                        if (target.RejectedReason == Reasons.TargetAllExposurePlans) {
                            hasAllEPsRejected = true;
                        }
                    }
                }

                if (hasAllEPsRejected) {
                    sb.AppendLine("\n(Rejection for 'all exposure plans' is due to moon avoidance or all exposure plans complete.)");
                }

                if (haveScoringRuns) {
                    sb.AppendLine("\nSCORING RUNS");
                    foreach (IPlanProject project in Projects) {
                        foreach (IPlanTarget target in project.Targets) {
                            if (target.ScoringResults != null && target.ScoringResults.Results.Count > 0) {
                                sb.AppendLine($"\n{project.Name}/{target.Name}");
                                sb.AppendLine(String.Format("{0,-30} {1,-9} {2,11} {3,11}", "RULE", "RAW SCORE", "WEIGHT", "SCORE"));
                                foreach (RuleResult result in target.ScoringResults.Results) {
                                    sb.AppendLine(String.Format("{0,-30} {1,9:0.00} {2,10:0.00}%  {3,10:0.00}",
                                        result.ScoringRule.Name,
                                        result.Score * ScoringRule.WEIGHT_SCALE,
                                        result.Weight * ScoringRule.WEIGHT_SCALE,
                                        result.Score * result.Weight * ScoringRule.WEIGHT_SCALE));
                                }

                                sb.AppendLine("----------------------------------------------------------------");
                                sb.AppendLine(String.Format("{0,57} {1,6:0.00}", "TOTAL SCORE", target.ScoringResults.TotalScore * ScoringRule.WEIGHT_SCALE));
                            }
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string GetStartTime(IPlanTarget target) {
            if (target.Rejected) {
                switch (target.RejectedReason) {
                    case Reasons.TargetNotYetVisible:
                    case Reasons.TargetBeforeMeridianWindow:
                    case Reasons.TargetLowerScore:
                        return DateFmt(target.StartTime);
                }
            }

            return "";
        }

        private string DateFmt(DateTime? dateTime) {
            if (dateTime == null || dateTime == DateTime.MinValue) {
                return "";
            }

            return ((DateTime)dateTime).ToString(Utils.DateFMT);
        }

        public string PlanSummary() {
            StringBuilder sb = new StringBuilder();
            if (WaitForNextTargetTime != null) {
                sb.AppendLine($"Waiting until {Utils.FormatDateTimeFull(WaitForNextTargetTime)}");
            } else {
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
        ProjectState State { get; set; }
        ProjectPriority Priority { get; set; }
        DateTime CreateDate { get; set; }
        DateTime? ActiveDate { get; set; }
        DateTime? InactiveDate { get; set; }
        int SessionId { get; }

        int MinimumTime { get; set; }
        double MinimumAltitude { get; set; }
        bool UseCustomHorizon { get; set; }
        double HorizonOffset { get; set; }
        int MeridianWindow { get; set; }
        int FilterSwitchFrequency { get; set; }
        int DitherEvery { get; set; }
        bool EnableGrader { get; set; }
        bool IsMosaic { get; set; }
        int FlatsHandling { get; set; }
        Dictionary<string, double> RuleWeights { get; set; }

        List<IPlanTarget> Targets { get; set; }
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
        public ProjectState State { get; set; }
        public ProjectPriority Priority { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? ActiveDate { get; set; }
        public DateTime? InactiveDate { get; set; }
        public int SessionId { get; }

        public int MinimumTime { get; set; }
        public double MinimumAltitude { get; set; }
        public bool UseCustomHorizon { get; set; }
        public double HorizonOffset { get; set; }
        public int MeridianWindow { get; set; }
        public int FilterSwitchFrequency { get; set; }
        public int DitherEvery { get; set; }
        public bool EnableGrader { get; set; }
        public bool IsMosaic { get; set; }
        public int FlatsHandling { get; set; }
        public Dictionary<string, double> RuleWeights { get; set; }

        public List<IPlanTarget> Targets { get; set; }
        public HorizonDefinition HorizonDefinition { get; set; }
        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }

        public PlanProject(IProfile profile, Project project) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = project.Id;
            this.Name = project.Name;
            this.Description = project.Description;
            this.State = project.State;
            this.Priority = project.Priority;
            this.CreateDate = project.CreateDate;
            this.ActiveDate = project.ActiveDate;
            this.InactiveDate = project.InactiveDate;
            this.SessionId = new FlatsExpert().GetCurrentSessionId(project, DateTime.Now);

            this.MinimumTime = project.MinimumTime;
            this.MinimumAltitude = project.MinimumAltitude;
            this.UseCustomHorizon = project.UseCustomHorizon;
            this.HorizonOffset = project.HorizonOffset;
            this.MeridianWindow = project.MeridianWindow;
            this.FilterSwitchFrequency = project.FilterSwitchFrequency;
            this.DitherEvery = project.DitherEvery;
            this.EnableGrader = project.EnableGrader;
            this.IsMosaic = project.IsMosaic;
            this.FlatsHandling = project.FlatsHandling;
            this.RuleWeights = GetRuleWeightsDictionary(project.RuleWeights);

            this.HorizonDefinition = DetermineHorizon(profile, project);
            this.Rejected = false;

            Targets = new List<IPlanTarget>();
            foreach (Target target in project.Targets) {
                if (target.Enabled) {
                    Targets.Add(new PlanTarget(this, target));
                }
            }
        }

        private Dictionary<string, double> GetRuleWeightsDictionary(List<RuleWeight> ruleWeights) {
            Dictionary<string, double> dict = new Dictionary<string, double>(ruleWeights.Count);
            ruleWeights.ForEach((rw) => {
                dict.Add(rw.Name, rw.Weight);
            });

            return dict;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("-- Project:");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"Priority: {Priority}");
            sb.AppendLine($"SessionId: {SessionId}");

            sb.AppendLine($"MinimumTime: {MinimumTime}");
            sb.AppendLine($"MinimumAltitude: {MinimumAltitude}");
            sb.AppendLine($"UseCustomHorizon: {UseCustomHorizon}");
            sb.AppendLine($"HorizonOffset: {HorizonOffset}");
            sb.AppendLine($"MeridianWindow: {MeridianWindow}");
            sb.AppendLine($"FilterSwitchFrequency: {FilterSwitchFrequency}");
            sb.AppendLine($"DitherEvery: {DitherEvery}");
            sb.AppendLine($"EnableGrader: {EnableGrader}");
            sb.AppendLine($"IsMosaic: {IsMosaic}");
            sb.AppendLine($"FlatsHandling: {FlatsHandling}");
            sb.AppendLine($"RuleWeights:");
            foreach (KeyValuePair<string, double> entry in RuleWeights) {
                sb.AppendLine($"  {entry.Key}: {entry.Value}");
            }

            sb.AppendLine($"Horizon: {HorizonDefinition}");
            sb.AppendLine($"Rejected: {Rejected}");
            sb.AppendLine($"RejectedReason: {RejectedReason}");

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

        private HorizonDefinition DetermineHorizon(IProfile profile, Project project) {
            if (project.UseCustomHorizon) {
                if (profile.AstrometrySettings.Horizon == null) {
                    TSLogger.Warning("project 'Use Custom Horizon' is enabled but no custom horizon was found in the profile, defaulting to Minimum Altitude");
                    return new HorizonDefinition(project.MinimumAltitude);
                }

                return new HorizonDefinition(profile.AstrometrySettings.Horizon, project.HorizonOffset, project.MinimumAltitude);
            }

            return new HorizonDefinition(project.MinimumAltitude);
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
        string OverrideExposureOrder { get; set; }
        List<IPlanExposure> ExposurePlans { get; set; }
        IPlanProject Project { get; set; }
        bool Rejected { get; set; }
        string RejectedReason { get; set; }
        ScoringResults ScoringResults { get; set; }
        DateTime StartTime { get; set; }
        DateTime EndTime { get; set; }
        DateTime CulminationTime { get; set; }
        TimeInterval MeridianWindow { get; set; }

        void SetCircumstances(bool isVisible, DateTime startTime, DateTime culminationTime, DateTime endTime);

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
        public string OverrideExposureOrder { get; set; }
        public List<IPlanExposure> ExposurePlans { get; set; }
        public IPlanProject Project { get; set; }
        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }
        public ScoringResults ScoringResults { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime CulminationTime { get; set; }
        public TimeInterval MeridianWindow { get; set; }

        public PlanTarget(IPlanProject planProject, Target target) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = target.Id;
            this.Name = target.Name;
            this.Coordinates = target.Coordinates;
            this.Epoch = target.Epoch;
            this.Rotation = target.Rotation;
            this.ROI = target.ROI;
            this.OverrideExposureOrder = target.OverrideExposureOrder;
            this.Project = planProject;
            this.Rejected = false;

            this.ExposurePlans = new List<IPlanExposure>();
            foreach (ExposurePlan plan in GetActiveExposurePlans(target)) {
                PlanExposure planExposure = new PlanExposure(this, plan, plan.ExposureTemplate);

                if (!planExposure.IsIncomplete()) {
                    planExposure.Rejected = true;
                    planExposure.RejectedReason = Reasons.FilterComplete;
                }

                this.ExposurePlans.Add(planExposure);
            }
        }

        public PlanTarget() {
        } // for PlanTargetEmulator only

        private List<ExposurePlan> GetActiveExposurePlans(Target target) {
            if (string.IsNullOrEmpty(target.OverrideExposureOrder)) {
                return target.ExposurePlans;
            }

            List<ExposurePlan> list = new List<ExposurePlan>();
            string[] items = target.OverrideExposureOrder.Split(Controls.AssistantManager.OverrideExposureOrder.SEP);
            foreach (string item in items) {
                if (item == Controls.AssistantManager.OverrideExposureOrder.DITHER) {
                    continue;
                } else {
                    int databaseId = 0;
                    Int32.TryParse(item, out databaseId);

                    ExposurePlan exposurePlan = target.ExposurePlans.Find(ep => ep.Id == databaseId);
                    if (exposurePlan != null && !list.Contains(exposurePlan)) {
                        list.Add(exposurePlan);
                    }
                }
            }

            return list;
        }

        public void SetCircumstances(bool isVisible, DateTime startTime, DateTime culminationTime, DateTime endTime) {
            if (isVisible) {
                StartTime = startTime;
                CulminationTime = culminationTime;
                EndTime = endTime;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Id: {PlanId}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Coords: {Coordinates.RAString} {Coordinates.DecString} {Epoch}");
            sb.AppendLine($"Rotation: {Rotation}");
            sb.AppendLine($"ROI: {ROI}");
            sb.AppendLine($"Override exp order: {OverrideExposureOrder}");
            sb.AppendLine($"StartTime: {Utils.FormatDateTimeFull(StartTime)}");
            sb.AppendLine($"EndTime: {Utils.FormatDateTimeFull(EndTime)}");
            sb.AppendLine($"CulminationTime: {Utils.FormatDateTimeFull(CulminationTime)}");

            if (MeridianWindow != null) {
                sb.AppendLine($"Meridian Window: {Utils.FormatDateTimeFull(MeridianWindow.StartTime)} - {Utils.FormatDateTimeFull(MeridianWindow.EndTime)}");
            }

            sb.AppendLine($"Rejected: {Rejected}");
            sb.AppendLine($"RejectedReason: {RejectedReason}");

            sb.AppendLine("-- ExposurePlans:");
            foreach (PlanExposure planExposure in ExposurePlans) {
                sb.AppendLine(planExposure.ToString());
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
            hash = hash * 23 + this.Name.GetHashCode();
            hash = hash * 23 + this.Coordinates.RAString.GetHashCode();
            hash = hash * 23 + this.Coordinates.DecString.GetHashCode();
            hash = hash * 23 + this.Rotation.ToString().GetHashCode();
            return hash;
        }
    }

    public class ScoringResults {
        public double TotalScore { get; set; }
        public List<RuleResult> Results { get; private set; }

        public ScoringResults() {
            Results = new List<RuleResult>();
        }

        public void AddRuleResult(RuleResult ruleResult) {
            Results.Add(ruleResult);
        }
    }

    public class RuleResult {
        public ScoringRule ScoringRule { get; private set; }
        public double Weight { get; private set; }
        public double Score { get; private set; }

        public RuleResult(ScoringRule scoringRule, double weight, double score) {
            ScoringRule = scoringRule;
            Weight = weight;
            Score = score;
        }
    }

    public interface IPlanExposure {
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

        TwilightLevel TwilightLevel { get; set; }
        bool MoonAvoidanceEnabled { get; set; }
        double MoonAvoidanceSeparation { get; set; }
        int MoonAvoidanceWidth { get; set; }
        double MaximumHumidity { get; set; }

        bool Rejected { get; set; }
        string RejectedReason { get; set; }
        int PlannedExposures { get; set; }

        int NeededExposures(double exposureThrottlePercentage);

        bool IsIncomplete();

        string ToString();
    }

    public class PlanExposure : IPlanExposure {
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

        public TwilightLevel TwilightLevel { get; set; }
        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }
        public double MaximumHumidity { get; set; }

        public bool Rejected { get; set; }
        public string RejectedReason { get; set; }

        public int PlannedExposures { get; set; }

        public PlanExposure(IPlanTarget planTarget, ExposurePlan exposurePlan, ExposureTemplate exposureTemplate) {
            this.PlanId = Guid.NewGuid().ToString();
            this.DatabaseId = exposurePlan.Id;
            this.FilterName = exposureTemplate.FilterName;
            this.ExposureLength = exposurePlan.Exposure != -1 ? exposurePlan.Exposure : exposureTemplate.DefaultExposure;
            this.Gain = GetNullableIntValue(exposureTemplate.Gain);
            this.Offset = GetNullableIntValue(exposureTemplate.Offset);
            this.BinningMode = exposureTemplate.BinningMode;
            this.ReadoutMode = GetNullableIntValue(exposureTemplate.ReadoutMode);
            this.Desired = exposurePlan.Desired;
            this.Acquired = exposurePlan.Acquired;
            this.Accepted = exposurePlan.Accepted;
            this.PlanTarget = planTarget;

            this.TwilightLevel = exposureTemplate.TwilightLevel;
            this.MoonAvoidanceEnabled = exposureTemplate.MoonAvoidanceEnabled;
            this.MoonAvoidanceSeparation = exposureTemplate.MoonAvoidanceSeparation;
            this.MoonAvoidanceWidth = exposureTemplate.MoonAvoidanceWidth;
            this.MaximumHumidity = exposureTemplate.MaximumHumidity;

            this.Rejected = false;
            this.PlannedExposures = 0;
        }

        public int NeededExposures(double exposureThrottlePercentage) {
            if (exposureThrottlePercentage > 0) {
                int throttleAt = (int)((exposureThrottlePercentage / 100) * Desired);
                return Acquired > throttleAt ? 0 : throttleAt - Acquired;
            } else {
                return Accepted > Desired ? 0 : Desired - Accepted;
            }
        }

        public bool IsIncomplete() {
            return Accepted < Desired;
        }

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
            return sb.ToString();
        }

        private int? GetNullableIntValue(int value) {
            if (value >= 0) {
                return value;
            }

            return null;
        }
    }

    public class Reasons {
        public const string ProjectComplete = "complete";
        public const string ProjectNoVisibleTargets = "no visible targets";
        public const string ProjectMoonAvoidance = "moon avoidance";
        public const string ProjectAllTargets = "all targets rejected";

        public const string TargetComplete = "complete";
        public const string TargetNeverRises = "never rises at location";
        public const string TargetNotVisible = "not visible";
        public const string TargetNotYetVisible = "not yet visible";
        public const string TargetMeridianWindowClipped = "clipped by meridian window";
        public const string TargetBeforeMeridianWindow = "before meridian window";
        public const string TargetMoonAvoidance = "moon avoidance";
        public const string TargetLowerScore = "lower score";
        public const string TargetAllExposurePlans = "all exposure plans rejected";

        public const string FilterComplete = "complete";
        public const string FilterMoonAvoidance = "moon avoidance";
        public const string FilterNoExposuresPlanned = "no exposures planned";

        private Reasons() {
        }
    }
}