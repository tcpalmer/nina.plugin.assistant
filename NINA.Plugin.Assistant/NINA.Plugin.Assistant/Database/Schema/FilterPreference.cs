using Assistant.NINAPlugin.Astrometry;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class FilterPreference : ICloneable {

        [Key] public int Id { get; set; }
        [Required] public string ProfileId { get; set; }
        [Required] public string FilterName { get; set; }
        public int twilightlevel_col { get; set; }
        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }
        public double MaximumHumidity { get; set; }

        [NotMapped]
        public TwilightLevel TwilightLevel {
            get { return (TwilightLevel)twilightlevel_col; }
            set { twilightlevel_col = (int)value; }
        }

        public FilterPreference() { }

        public FilterPreference(string profileId, string filterName) {
            ProfileId = profileId;
            FilterName = filterName;

            TwilightLevel = TwilightLevel.Nighttime;
            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 0;
            MoonAvoidanceWidth = 0;
            MaximumHumidity = 0;
        }

        public bool IsTwilightNightOnly() {
            return TwilightLevel == TwilightLevel.Nighttime;
        }

        public bool IsTwilightAstronomical() {
            return TwilightLevel == TwilightLevel.Astronomical;
        }

        public bool IsTwilightNautical() {
            return TwilightLevel == TwilightLevel.Nautical;
        }

        public bool IsTwilightCivil() {
            return TwilightLevel == TwilightLevel.Civil;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"TwilightLevel: {TwilightLevel}");
            sb.AppendLine($"MoonAvoidanceEnabled: {MoonAvoidanceEnabled}");
            sb.AppendLine($"MoonAvoidanceSeparation: {MoonAvoidanceSeparation}");
            sb.AppendLine($"MoonAvoidanceWidth: {MoonAvoidanceWidth}");
            sb.AppendLine($"MaximumHumidity: {MaximumHumidity}");

            return sb.ToString();
        }

        public object Clone() {
            return MemberwiseClone();
        }

    }

    internal class FilterPreferenceConfiguration : EntityTypeConfiguration<FilterPreference> {

        public FilterPreferenceConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x.twilightlevel_col).HasColumnName("twilightlevel");
        }
    }
}
