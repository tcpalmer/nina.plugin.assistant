using NINA.Core.Model.Equipment;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class FilterPlan : ICloneable {

        [Key] public int Id { get; set; }
        [Required] public string FilterName { get; set; }
        [Required] public string ProfileId { get; set; }
        [Required] public double Exposure { get; set; }

        public int? Gain { get; set; }
        public int? Offset { get; set; }
        public int? bin { get; set; }
        public int? ReadoutMode { get; set; }

        public int Desired { get; set; }
        public int Acquired { get; set; }
        public int Accepted { get; set; }

        [NotMapped]
        public BinningMode BinningMode {
            get { return new BinningMode((short)bin, (short)bin); }
            set { bin = value.X; }
        }

        [ForeignKey("Target")] public int targetId { get; set; }
        public Target Target { get; set; }

        public FilterPlan() { }

        public FilterPlan(string profileId, string filterName) {
            this.ProfileId = profileId;
            this.FilterName = filterName;
            Exposure = 60;
            Gain = 0;
            Offset = 0;
            BinningMode = new BinningMode(1, 1);
            ReadoutMode = -1;
            Desired = 1;
            Acquired = 0;
            Accepted = 0;
        }

        public object Clone() {
            return MemberwiseClone();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"Exposure: {Exposure}");
            sb.AppendLine($"Gain: {Gain}");
            sb.AppendLine($"Offset: {Offset}");
            sb.AppendLine($"BinningMode: {BinningMode}");
            sb.AppendLine($"ReadoutMode: {ReadoutMode}");
            sb.AppendLine($"Desired: {Desired}");
            sb.AppendLine($"Acquired: {Acquired}");
            sb.AppendLine($"Accepted: {Accepted}");

            return sb.ToString();
        }
    }
}
