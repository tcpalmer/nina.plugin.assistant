using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Core.Model.Equipment;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class AssistantPlan {

        public string Id { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public PlanTarget PlanTarget { get; private set; }

        public AssistantPlan(PlanTarget planTarget, TimeInterval timeInterval) {
            this.Id = Guid.NewGuid().ToString();
            this.PlanTarget = planTarget;
            this.TimeInterval = timeInterval;
        }
    }

    public class PlanProject {

        public string Name { get; private set; }
        public string Description { get; private set; }
        public int State { get; private set; }
        public int Priority { get; private set; }
        public DateTime CreateDate { get; private set; }
        public DateTime? ActiveDate { get; private set; }
        public DateTime? InactiveDate { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public List<PlanTarget> Targets { get; private set; }
        public AssistantProjectPreferences Preferences { get; private set; }
        public HorizonDefinition HorizonDefinition { get; private set; }

        public PlanProject(IProfile profile, Project project, Dictionary<string, AssistantFilterPreferences> filterPreferences) {
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

            Targets = new List<PlanTarget>();
            foreach (Target target in project.targets) {
                Targets.Add(new PlanTarget(this, target, filterPreferences));
            }
        }

        private HorizonDefinition DetermineHorizon(IProfile profile, AssistantProjectPreferences projectPreferences) {
            if (projectPreferences.UseCustomHorizon) {
                return new HorizonDefinition(profile.AstrometrySettings.Horizon, projectPreferences.HorizonOffset);
            }

            return new HorizonDefinition(projectPreferences.MinimumAltitude);
        }
    }

    public class PlanTarget {

        public string Id { get; private set; }
        public string Name { get; private set; }
        public Coordinates Coordinates { get; private set; }
        public Epoch Epoch;
        public double Rotation { get; private set; }
        public double ROI { get; private set; }
        public List<PlanFilter> FilterPlans { get; private set; }
        public PlanProject Project { get; private set; }

        public TimeInterval TimeInterval { get; private set; }

        public PlanTarget(PlanProject planProject, Target target, Dictionary<string, AssistantFilterPreferences> filterPreferences) {
            this.Id = new Guid().ToString();
            this.Name = target.name;
            this.Coordinates = new Coordinates(Angle.ByHours(target.ra), Angle.ByDegree(target.dec), target.Epoch);
            this.Epoch = target.Epoch;
            this.Rotation = target.rotation;
            this.ROI = target.roi;
            this.Project = planProject;

            this.FilterPlans = new List<PlanFilter>();
            foreach (FilterPlan plan in target.filterplans) {
                AssistantFilterPreferences filterPrefs = filterPreferences[plan.filterName];
                this.FilterPlans.Add(new PlanFilter(this, plan, filterPrefs));
            }
        }
    }

    public class PlanFilter {

        public string Id { get; private set; }
        public string FilterName { get; private set; }
        public double ExposureLength { get; private set; }
        public int? Gain { get; private set; }
        public int? Offset { get; private set; }
        public BinningMode BinningMode { get; private set; }
        public int? ReadoutMode { get; private set; }
        public int Desired { get; private set; }
        public int Acquired { get; private set; }
        public int Accepted { get; private set; }
        public PlanTarget PlanTarget { get; private set; }
        public AssistantFilterPreferences Preferences { get; private set; }

        public int PlannedExposures { get; set; }

        public PlanFilter(PlanTarget planTarget, FilterPlan filterPlan, AssistantFilterPreferences preferences) {
            this.Id = new Guid().ToString();
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
        }
    }

}
