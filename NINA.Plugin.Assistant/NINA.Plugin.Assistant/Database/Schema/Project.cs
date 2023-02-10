﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public enum ProjectState {
        Draft, Active, Inactive, Closed
    }

    public enum ProjectPriority {
        Low, Normal, High
    }

    public class Project : ICloneable {

        [Key] public int Id { get; set; }
        [Required] public string ProfileId { get; set; }
        [Required] public string Name { get; set; }

        public string Description { get; set; }
        public int state_col { get; set; }
        public int priority_col { get; set; }
        public long createDate { get; set; }
        public long? activeDate { get; set; }
        public long? inactiveDate { get; set; }
        public long? startDate { get; set; }
        public long? endDate { get; set; }

        public int MinimumTime { get; set; }
        public double MinimumAltitude { get; set; }
        public int useCustomHorizon { get; set; }
        public double HorizonOffset { get; set; }
        public int FilterSwitchFrequency { get; set; }
        public int DitherEvery { get; set; }
        public int enableGrader { get; set; }

        public List<RuleWeight> ruleWeights { get; set; }

        [NotMapped]
        Dictionary<string, double> _ruleWeights;

        [NotMapped]
        public Dictionary<string, double> RuleWeights {
            get {
                if (_ruleWeights == null) {
                    _ruleWeights = new Dictionary<string, double>();
                    if (ruleWeights?.Count > 0) {
                        foreach (RuleWeight ruleWeight in ruleWeights) {
                            _ruleWeights.Add(ruleWeight.Name, ruleWeight.Weight);
                        }
                    }
                }

                return _ruleWeights;
            }
            set { _ruleWeights = value; }
        }

        public List<Target> Targets { get; set; }

        public Project() { }

        public Project(string profileId) {
            ProfileId = profileId;
            State = ProjectState.Draft;
            Priority = ProjectPriority.Normal;
            CreateDate = DateTime.Now;

            MinimumTime = 30;
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            HorizonOffset = 0;
            FilterSwitchFrequency = 0;
            DitherEvery = 0;
            EnableGrader = true;

            ruleWeights = new List<RuleWeight>();
            RuleWeights = new Dictionary<string, double>();

            Targets = new List<Target>();
        }

        [NotMapped]
        public ProjectPriority Priority {
            get { return (ProjectPriority)priority_col; }
            set { priority_col = (int)value; }
        }

        [NotMapped]
        public ProjectState State {
            get { return (ProjectState)state_col; }
            set { state_col = (int)value; }
        }

        [NotMapped]
        public DateTime CreateDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(createDate); }
            set { createDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? ActiveDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(activeDate); }
            set { activeDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? InactiveDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(inactiveDate); }
            set { inactiveDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? StartDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(startDate); }
            set { startDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public DateTime? EndDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(endDate); }
            set { endDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public bool UseCustomHorizon {
            get { return useCustomHorizon == 1; }
            set { useCustomHorizon = value ? 1 : 0; }
        }

        [NotMapped]
        public bool EnableGrader {
            get { return enableGrader == 1; }
            set { enableGrader = value ? 1 : 0; }
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Description: {Description}");
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"Priority: {Priority}");
            sb.AppendLine($"CreateDate: {CreateDate}");
            sb.AppendLine($"ActiveDate: {ActiveDate}");
            sb.AppendLine($"InactiveDate: {InactiveDate}");
            sb.AppendLine($"StartDate: {StartDate}");
            sb.AppendLine($"EndDate: {EndDate}");
            sb.AppendLine($"MinimumTime: {MinimumTime}");
            sb.AppendLine($"MinimumAltitude: {MinimumAltitude}");
            sb.AppendLine($"UseCustomHorizon: {UseCustomHorizon}");
            sb.AppendLine($"HorizonOffset: {HorizonOffset}");
            sb.AppendLine($"FilterSwitchFrequency: {FilterSwitchFrequency}");
            sb.AppendLine($"DitherEvery: {DitherEvery}");
            sb.AppendLine($"EnableGrader: {EnableGrader}");
            sb.AppendLine($"RuleWeights:");
            foreach (var item in RuleWeights) {
                sb.AppendLine($"  {item.Key} {item.Value}");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    internal class ProjectConfiguration : EntityTypeConfiguration<Project> {

        public ProjectConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x.state_col).HasColumnName("state");
            Property(x => x.priority_col).HasColumnName("priority");
        }
    }
}
