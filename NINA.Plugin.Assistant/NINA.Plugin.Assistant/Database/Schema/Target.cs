using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Target : INotifyPropertyChanged {

        [Key] public int Id { get; set; }

        [Required]
        public string name { get; set; }

        [Required] public double ra { get; set; }
        [Required] public double dec { get; set; }
        [Required] public int epochCode { get; set; }
        public double rotation { get; set; }
        public double roi { get; set; }

        [ForeignKey("Project")] public int ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public virtual List<ExposurePlan> ExposurePlans { get; set; }

        public Target() {
            ra = 0;
            dec = 0;
            epochCode = (int)Epoch.J2000;
            rotation = 0;
            roi = 1;
            ExposurePlans = new List<ExposurePlan>();
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
        public double RA {
            get => ra;
            private set { }
        }

        [NotMapped]
        public double Dec {
            get => dec;
            private set { }
        }

        [NotMapped] private Coordinates coordinates = null;
        [NotMapped]
        public Coordinates Coordinates {
            get {
                if (coordinates is null) {
                    coordinates = new Coordinates(Angle.ByHours(ra), Angle.ByDegree(dec), Epoch);
                }

                return coordinates;
            }
            set {
                coordinates = value;
                RaiseCoordinatesChanged();
            }
        }

        [NotMapped]
        public int RAHours {
            get {
                return (int)Math.Truncate(Coordinates.RA);
            }
            set {
                if (value >= 0) {
                    Coordinates.RA = Coordinates.RA - RAHours + value;
                    RaiseCoordinatesChanged();
                }
            }
        }

        [NotMapped]
        public int RAMinutes {
            get {
                var minutes = (Math.Abs(Coordinates.RA * 60.0d) % 60);
                var seconds = (int)Math.Round((Math.Abs(Coordinates.RA * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    minutes += 1;
                }

                return (int)Math.Floor(minutes);
            }
            set {
                if (value >= 0) {
                    Coordinates.RA = Coordinates.RA - RAMinutes / 60.0d + value / 60.0d;
                    RaiseCoordinatesChanged();
                }
            }
        }

        [NotMapped]
        public int RASeconds {
            get {
                var seconds = (int)Math.Round((Math.Abs(Coordinates.RA * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    seconds = 0;
                }
                return seconds;
            }
            set {
                if (value >= 0) {
                    Coordinates.RA = Coordinates.RA - RASeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                    RaiseCoordinatesChanged();
                }
            }
        }

        [NotMapped]
        public string RAString { get => Utils.GetRAString(Coordinates.RADegrees); }

        [NotMapped] private bool negativeDec;
        [NotMapped]
        public bool NegativeDec {
            get => negativeDec;
            set {
                negativeDec = value;
                RaisePropertyChanged();
            }
        }

        [NotMapped]
        public int DecDegrees {
            get {
                return (int)Math.Truncate(Coordinates.Dec);
            }
            set {
                if (NegativeDec) {
                    Coordinates.Dec = value - DecMinutes / 60.0d - DecSeconds / (60.0d * 60.0d);
                }
                else {
                    Coordinates.Dec = value + DecMinutes / 60.0d + DecSeconds / (60.0d * 60.0d);
                }
                RaiseCoordinatesChanged();
            }
        }

        [NotMapped]
        public int DecMinutes {
            get {
                var minutes = (Math.Abs(Coordinates.Dec * 60.0d) % 60);
                var seconds = (int)Math.Round((Math.Abs(Coordinates.Dec * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    minutes += 1;
                }

                return (int)Math.Floor(minutes);
            }
            set {
                if (NegativeDec) {
                    Coordinates.Dec = Coordinates.Dec + DecMinutes / 60.0d - value / 60.0d;
                }
                else {
                    Coordinates.Dec = Coordinates.Dec - DecMinutes / 60.0d + value / 60.0d;
                }

                RaiseCoordinatesChanged();
            }
        }

        [NotMapped]
        public int DecSeconds {
            get {
                var seconds = (int)Math.Round((Math.Abs(Coordinates.Dec * 60.0d * 60.0d) % 60));
                if (seconds > 59) {
                    seconds = 0;
                }
                return seconds;
            }
            set {
                if (NegativeDec) {
                    Coordinates.Dec = Coordinates.Dec + DecSeconds / (60.0d * 60.0d) - value / (60.0d * 60.0d);
                }
                else {
                    Coordinates.Dec = Coordinates.Dec - DecSeconds / (60.0d * 60.0d) + value / (60.0d * 60.0d);
                }

                RaiseCoordinatesChanged();
            }
        }

        [NotMapped]
        public string DecString { get => Coordinates.DecString; }

        [NotMapped]
        public Epoch Epoch {
            get => (Epoch)epochCode;
            set {
                epochCode = (int)value;
                RaisePropertyChanged(nameof(Epoch));
            }
        }

        [NotMapped]
        public double Rotation {
            get => rotation;
            set {
                rotation = value;
                RaisePropertyChanged(nameof(Rotation));
            }
        }

        [NotMapped]
        public double ROI {
            get => roi;
            set {
                roi = value;
                RaisePropertyChanged(nameof(ROI));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseCoordinatesChanged() {
            RaisePropertyChanged(nameof(RAHours));
            RaisePropertyChanged(nameof(RAMinutes));
            RaisePropertyChanged(nameof(RASeconds));
            RaisePropertyChanged(nameof(RAString));
            RaisePropertyChanged(nameof(DecDegrees));
            RaisePropertyChanged(nameof(DecMinutes));
            RaisePropertyChanged(nameof(DecSeconds));
            RaisePropertyChanged(nameof(DecString));
            NegativeDec = Coordinates.Dec < 0;

            ra = Coordinates.RA;
            dec = Coordinates.Dec;
        }

        public Target GetPasteCopy(string profileId) {
            Target target = new Target();

            target.name = Utils.CopiedItemName(name);
            target.ra = ra;
            target.dec = dec;
            target.epochCode = epochCode;
            target.rotation = rotation;
            target.roi = roi;
            ExposurePlans.ForEach(item => target.ExposurePlans.Add(item.GetPasteCopy(profileId)));

            return target;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"RA: {RA}");
            sb.AppendLine($"Dec: {Dec}");
            sb.AppendLine($"Coords: {Coordinates}");
            sb.AppendLine($"Epoch: {Epoch}");
            sb.AppendLine($"Rotation: {Rotation}");
            sb.AppendLine($"ROI: {ROI}");

            return sb.ToString();
        }
    }

    internal class TargetConfiguration : EntityTypeConfiguration<Target> {

        public TargetConfiguration() {
            HasKey(t => new { t.Id });
            HasMany(t => t.ExposurePlans)
             .WithRequired(e => e.Target)
             .HasForeignKey(e => e.TargetId);
        }
    }
}
