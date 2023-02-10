using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class AcquiredImage {

        [Key] public int Id { get; set; }
        [Required] public int TargetId { get; set; }
        public long acquiredDate { get; set; }
        public string FilterName { get; set; }
        internal string _metadata { get; set; }

        [NotMapped]
        public DateTime AcquiredDate {
            get { return AssistantDbContext.UnixSecondsToDateTime(acquiredDate); }
            set { acquiredDate = AssistantDbContext.DateTimeToUnixSeconds(value); }
        }

        [NotMapped]
        public ImageMetadata Metadata {
            get {
                return JsonConvert.DeserializeObject<ImageMetadata>(_metadata);
            }
            set {
                _metadata = JsonConvert.SerializeObject(value);
            }
        }

        public AcquiredImage() { }

        public AcquiredImage(ImageMetadata imageMetadata) {
            this.Metadata = imageMetadata;
        }

        public AcquiredImage(int targetId, DateTime acquiredDate, string filterName, ImageMetadata imageMetadata) {
            this.TargetId = targetId;
            this.AcquiredDate = acquiredDate;
            this.FilterName = filterName;
            this.Metadata = imageMetadata;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"AcquiredDate: {AcquiredDate}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"Metadata: {_metadata}");

            return sb.ToString();
        }
    }

    internal class AcquiredImageConfiguration : EntityTypeConfiguration<AcquiredImage> {

        public AcquiredImageConfiguration() {
            HasKey(x => new { x.Id });
            Property(x => x._metadata).HasColumnName("metadata");
        }
    }
}
