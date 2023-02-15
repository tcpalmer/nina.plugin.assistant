using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public enum ProjectState {
        Draft, Active, Inactive, Closed
    }

    public enum ProjectPriority {
        Low, Normal, High
    }

    public class Project : INotifyPropertyChanged, ICloneable {

        [Key] public int Id { get; set; }
        [Required] public string ProfileId { get; set; }
        [Required] public string name { get; set; }
        public string description { get; set; }
        public int state_col { get; set; }
        public int priority_col { get; set; }
        public long createDate { get; set; }
        public long? activeDate { get; set; }
        public long? inactiveDate { get; set; }
        public long? startDate { get; set; }
        public long? endDate { get; set; }

        public int minimumTime { get; set; }
        public double minimumAltitude { get; set; }
        public int useCustomHorizon { get; set; }
        public double horizonOffset { get; set; }
        public int filterSwitchFrequency { get; set; }
        public int ditherEvery { get; set; }
        public int enableGrader { get; set; }

        public List<RuleWeight> ruleWeights { get; set; }
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
            Targets = new List<Target>();
        }

        [NotMapped]
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        [NotMapped]
        public string Description {
            get => description;
            set {
                description = value;
                RaisePropertyChanged(nameof(Description));
            }
        }

        [NotMapped]
        public ProjectPriority Priority {
            get { return (ProjectPriority)priority_col; }
            set {
                priority_col = (int)value;
                RaisePropertyChanged(nameof(ProjectPriority));
            }
        }

        [NotMapped]
        public ProjectState State {
            get { return (ProjectState)state_col; }
            set {
                state_col = (int)value;
                RaisePropertyChanged(nameof(ProjectState));
            }
        }

        [NotMapped]
        public DateTime CreateDate {
            get { return AssistantDatabaseContext.UnixSecondsToDateTime(createDate); }
            set {
                createDate = AssistantDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(CreateDate));
            }
        }

        [NotMapped]
        public DateTime? ActiveDate {
            get { return AssistantDatabaseContext.UnixSecondsToDateTime(activeDate); }
            set {
                activeDate = AssistantDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(ActiveDate));
            }
        }

        [NotMapped]
        public DateTime? InactiveDate {
            get { return AssistantDatabaseContext.UnixSecondsToDateTime(inactiveDate); }
            set {
                inactiveDate = AssistantDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(InactiveDate));
            }
        }

        [NotMapped]
        public DateTime? StartDate {
            get { return AssistantDatabaseContext.UnixSecondsToDateTime(startDate); }
            set {
                startDate = AssistantDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(StartDate));
            }
        }

        [NotMapped]
        public DateTime? EndDate {
            get { return AssistantDatabaseContext.UnixSecondsToDateTime(endDate); }
            set {
                endDate = AssistantDatabaseContext.DateTimeToUnixSeconds(value);
                RaisePropertyChanged(nameof(EndDate));
            }
        }

        [NotMapped]
        public int MinimumTime {
            get => minimumTime;
            set {
                minimumTime = value;
                RaisePropertyChanged(nameof(MinimumTime));
            }
        }

        [NotMapped]
        public double MinimumAltitude {
            get => minimumAltitude;
            set {
                minimumAltitude = value;
                RaisePropertyChanged(nameof(MinimumAltitude));
            }
        }

        [NotMapped]
        public bool UseCustomHorizon {
            get { return useCustomHorizon == 1; }
            set {
                useCustomHorizon = value ? 1 : 0;
                RaisePropertyChanged(nameof(UseCustomHorizon));
            }
        }

        [NotMapped]
        public double HorizonOffset {
            get => horizonOffset;
            set {
                horizonOffset = value;
                RaisePropertyChanged(nameof(HorizonOffset));
            }
        }

        [NotMapped]
        public int FilterSwitchFrequency {
            get => filterSwitchFrequency;
            set {
                filterSwitchFrequency = value;
                RaisePropertyChanged(nameof(FilterSwitchFrequency));
            }
        }

        [NotMapped]
        public int DitherEvery {
            get => ditherEvery;
            set {
                ditherEvery = value;
                RaisePropertyChanged(nameof(DitherEvery));
            }
        }

        [NotMapped]
        public bool EnableGrader {
            get { return enableGrader == 1; }
            set {
                enableGrader = value ? 1 : 0;
                RaisePropertyChanged(nameof(EnableGrader));
            }
        }

        [NotMapped]
        public List<RuleWeight> RuleWeights {
            get => ruleWeights;
            set {
                ruleWeights = value;
                RaisePropertyChanged(nameof(RuleWeights));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public object Clone() {
            Project clone = (Project)MemberwiseClone();

            List<RuleWeight> list = new List<RuleWeight>();
            if (ruleWeights != null) {
                ruleWeights.ForEach((item) => {
                    list.Add((RuleWeight)item.Clone());
                });
            }

            clone.RuleWeights = list;
            return clone;
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
                sb.AppendLine($"  {item.Name} {item.Weight}");
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
