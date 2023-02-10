using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Target : ICloneable {

        [Key] public int Id { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Target name must be less than 256 characters")]
        public string Name { get; set; }

        [Required] public double RA { get; set; }
        [Required] public double Dec { get; set; }
        [Required] public int epochCode { get; set; }
        public double Rotation { get; set; }
        public double ROI { get; set; }

        [NotMapped]
        public Epoch Epoch {
            get => (Epoch)epochCode;
            set {
                epochCode = (int)value;
            }
        }

        public virtual Project Project { get; set; }

        public List<FilterPlan> FilterPlans { get; set; }

        public Target() {
            epochCode = (int)Epoch.J2000;
            Rotation = 0;
            ROI = 1;
            FilterPlans = new List<FilterPlan>();
        }

        public object Clone() {
            return MemberwiseClone();
            // TODO: filter plans?
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"RA: {RA}");
            sb.AppendLine($"Dec: {Dec}");
            sb.AppendLine($"Epoch: {Epoch}");
            sb.AppendLine($"Rotation: {Rotation}");
            sb.AppendLine($"ROI: {ROI}");

            return sb.ToString();
        }

        public Coordinates GetCoordinates() {
            return new Coordinates(Angle.ByHours(RA), Angle.ByDegree(Dec), Epoch);
        }

    }
}
