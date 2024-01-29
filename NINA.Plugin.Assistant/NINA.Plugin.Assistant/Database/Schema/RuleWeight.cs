using Assistant.NINAPlugin.Util;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class RuleWeight : INotifyPropertyChanged, IComparable<RuleWeight> {
        [Key] public int Id { get; set; }
        [Required] public string name { get; set; }
        [Required] public double weight { get; set; }

        [NotMapped]
        public string Name {
            get => name;
            set {
                name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        [NotMapped]
        public double Weight {
            get => weight;
            set {
                weight = value;
                RaisePropertyChanged(nameof(Weight));
            }
        }

        [ForeignKey("Project")] public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public RuleWeight() {
        }

        public RuleWeight(string name, double weight) {
            Assert.isTrue(weight >= 0 && weight <= 100, "weight must be 0-100");

            Name = name;
            Weight = weight;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RuleWeight GetPasteCopy() {
            RuleWeight ruleWeight = new RuleWeight {
                name = name,
                weight = weight
            };

            return ruleWeight;
        }

        public int CompareTo(RuleWeight other) {
            if (other == null) {
                return 1;
            }

            return Name.CompareTo(other.Name);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Weight: {Weight}");

            return sb.ToString();
        }
    }
}