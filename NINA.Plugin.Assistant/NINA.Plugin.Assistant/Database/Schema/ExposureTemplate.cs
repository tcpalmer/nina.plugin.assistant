using Assistant.NINAPlugin.Astrometry;
using NINA.Core.Model.Equipment;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ExposureTemplate : INotifyPropertyChanged {

        [Key] public int Id { get; set; }
        [Required] public string profileId { get; set; }
        [Required] public string name { get; set; }
        [Required] public string filterName { get; set; }

        public int gain { get; set; }
        public int offset { get; set; }
        public int? bin { get; set; }
        public int readoutMode { get; set; }

        public int twilightlevel_col { get; set; }
        public bool moonAvoidanceEnabled { get; set; }
        public double moonAvoidanceSeparation { get; set; }
        public int moonAvoidanceWidth { get; set; }
        public double maximumHumidity { get; set; }

        [NotMapped]
        public string ProfileId {
            get { return profileId; }
            set {
                profileId = value;
                RaisePropertyChanged(nameof(ProfileId));
            }
        }

        [NotMapped]
        public string Name {
            get { return name; }
            set {
                name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        [NotMapped]
        public string FilterName {
            get { return filterName; }
            set {
                filterName = value;
                RaisePropertyChanged(nameof(FilterName));
            }
        }

        [NotMapped]
        public int Gain {
            get { return gain; }
            set {
                gain = value;
                RaisePropertyChanged(nameof(Gain));
            }
        }

        [NotMapped]
        public int Offset {
            get { return offset; }
            set {
                offset = value;
                RaisePropertyChanged(nameof(Offset));
            }
        }

        [NotMapped]
        public BinningMode BinningMode {
            get { return new BinningMode((short)bin, (short)bin); }
            set {
                bin = value.X;
                RaisePropertyChanged(nameof(BinningMode));
            }
        }

        [NotMapped]
        public int ReadoutMode {
            get { return readoutMode; }
            set {
                readoutMode = value;
                RaisePropertyChanged(nameof(ReadoutMode));
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

        public ExposureTemplate() { }

        public ExposureTemplate(string profileId, string name, string filterName) {
            ProfileId = profileId;
            Name = name;
            FilterName = filterName;

            Gain = -1;
            Offset = -1;
            BinningMode = new BinningMode(1, 1);
            ReadoutMode = -1;

            TwilightLevel = TwilightLevel.Nighttime;
            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 60;
            MoonAvoidanceWidth = 7;
            MaximumHumidity = 0;
        }

        /* TODO: do we need a GetPasteCopy for this?  If so, following came from Exp Plan:
            exposurePlan.gain = gain;
            exposurePlan.offset = offset;
            exposurePlan.bin = bin;
            exposurePlan.readoutMode = readoutMode;
         */

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
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"Gain: {Gain}");
            sb.AppendLine($"Offset: {Offset}");
            sb.AppendLine($"BinningMode: {BinningMode}");
            sb.AppendLine($"ReadoutMode: {ReadoutMode}");
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

    internal class ExposureTemplateConfiguration : EntityTypeConfiguration<ExposureTemplate> {

        public ExposureTemplateConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x.twilightlevel_col).HasColumnName("twilightlevel");
        }
    }
}
