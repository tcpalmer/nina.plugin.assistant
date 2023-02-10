using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class RuleWeight : ICloneable {

        [Key] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public double Weight { get; set; }

        public virtual Project Project { get; set; }

        public RuleWeight() { }

        public RuleWeight(string name, double weight) {
            Name = name;
            Weight = weight;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"Weight: {Weight}");

            return sb.ToString();
        }

    }

}
