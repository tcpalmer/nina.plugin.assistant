using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ExposurePlan : INotifyPropertyChanged {

        [Key] public int Id { get; set; }
        [Required] public string profileId { get; set; }
        [Required] public double exposure { get; set; }
        public int exposureTemplateId { get; set; }

        public int desired { get; set; }
        public int acquired { get; set; }
        public int accepted { get; set; }

        public virtual ExposureTemplate ExposureTemplate { get; set; }

        [ForeignKey("Target")] public int TargetId { get; set; }
        public virtual Target Target { get; set; }

        [NotMapped]
        public string ProfileId {
            get { return profileId; }
            set {
                profileId = value;
                RaisePropertyChanged(nameof(ProfileId));
            }
        }

        [NotMapped]
        public double Exposure {
            get { return exposure; }
            set {
                exposure = value;
                RaisePropertyChanged(nameof(Exposure));
            }
        }

        [NotMapped]
        public int Desired {
            get { return desired; }
            set {
                desired = value;
                RaisePropertyChanged(nameof(Desired));
            }
        }

        [NotMapped]
        public int Acquired {
            get { return acquired; }
            set {
                acquired = value;
                RaisePropertyChanged(nameof(Acquired));
            }
        }

        [NotMapped]
        public int Accepted {
            get { return accepted; }
            set {
                accepted = value;
                RaisePropertyChanged(nameof(Accepted));
            }
        }

        public ExposurePlan() { }

        public ExposurePlan(string profileId, ExposureTemplate exposureTemplate) {
            this.ProfileId = profileId;
            this.ExposureTemplate = exposureTemplate;
            Exposure = 60;
            Desired = 1;
            Acquired = 0;
            Accepted = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ExposurePlan GetPasteCopy(string newProfileId) {
            ExposurePlan exposurePlan = new ExposurePlan();

            exposurePlan.profileId = newProfileId;
            exposurePlan.ExposureTemplate = this.ExposureTemplate;
            exposurePlan.exposure = exposure;
            exposurePlan.desired = desired;
            exposurePlan.acquired = 0;
            exposurePlan.accepted = 0;

            return exposurePlan;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"ProfileId: {ProfileId}");
            sb.AppendLine($"ExposureTemplate: {ExposureTemplate}");
            sb.AppendLine($"Exposure: {Exposure}");
            sb.AppendLine($"Desired: {Desired}");
            sb.AppendLine($"Acquired: {Acquired}");
            sb.AppendLine($"Accepted: {Accepted}");

            return sb.ToString();
        }
    }

    internal class ExposurePlanConfiguration : EntityTypeConfiguration<ExposurePlan> {

        public ExposurePlanConfiguration() {
            HasKey(e => new { e.Id });
            HasRequired(e => e.ExposureTemplate)
                .WithMany()
                .HasForeignKey(e => e.exposureTemplateId);

        }
    }
}
