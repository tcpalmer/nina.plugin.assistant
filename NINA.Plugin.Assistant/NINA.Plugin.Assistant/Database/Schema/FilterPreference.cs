using Assistant.NINAPlugin.Astrometry;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class FilterPreference : INotifyPropertyChanged {

        [Key] public int Id { get; set; }
        [Required] public string profileId { get; set; }
        [Required] public string filterName { get; set; }
        public int twilightlevel_col { get; set; }
        public bool moonAvoidanceEnabled { get; set; }
        public double moonAvoidanceSeparation { get; set; }
        public int moonAvoidanceWidth { get; set; }
        public double maximumHumidity { get; set; }

        [NotMapped]
        public string FilterName {
            get { return filterName; }
            set {
                filterName = value;
                RaisePropertyChanged(nameof(FilterName));
            }
        }

        [NotMapped]
        public string ProfileId {
            get { return profileId; }
            set {
                profileId = value;
                RaisePropertyChanged(nameof(ProfileId));
            }
        }

        [NotMapped]
        public TwilightLevel TwilightLevel {
            get { return (TwilightLevel)twilightlevel_col; }
            set {
                twilightlevel_col = (int)value;
                RaisePropertyChanged(nameof(TwilightLevel));
            }
        }

        [NotMapped]
        public bool MoonAvoidanceEnabled {
            get { return moonAvoidanceEnabled; }
            set {
                moonAvoidanceEnabled = value;
                RaisePropertyChanged(nameof(MoonAvoidanceEnabled));
            }
        }

        [NotMapped]
        public double MoonAvoidanceSeparation {
            get { return moonAvoidanceSeparation; }
            set {
                moonAvoidanceSeparation = value;
                RaisePropertyChanged(nameof(MoonAvoidanceSeparation));
            }
        }

        [NotMapped]
        public int MoonAvoidanceWidth {
            get { return moonAvoidanceWidth; }
            set {
                moonAvoidanceWidth = value;
                RaisePropertyChanged(nameof(MoonAvoidanceWidth));
            }
        }

        [NotMapped]
        public double MaximumHumidity {
            get { return maximumHumidity; }
            set {
                maximumHumidity = value;
                RaisePropertyChanged(nameof(MaximumHumidity));
            }
        }

        public FilterPreference() { }

        public FilterPreference(string profileId, string filterName) {
            ProfileId = profileId;
            FilterName = filterName;

            TwilightLevel = TwilightLevel.Nighttime;
            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 60;
            MoonAvoidanceWidth = 7;
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    internal class FilterPreferenceConfiguration : EntityTypeConfiguration<FilterPreference> {

        public FilterPreferenceConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x.twilightlevel_col).HasColumnName("twilightlevel");
        }
    }
}
