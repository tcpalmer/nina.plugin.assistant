using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Assistant.NINAPlugin.Database.Schema {

    public class AcquiredImage {

        [Key]
        public int id { get; set; }

        [Required]
        public int targetId { get; set; }

        public long acquiredDate { get; set; }
        public string filterName { get; set; }

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
            this.targetId = targetId;
            this.AcquiredDate = acquiredDate;
            this.filterName = filterName;
            this.Metadata = imageMetadata;
        }
    }

    internal class AcquiredImageConfiguration : EntityTypeConfiguration<AcquiredImage> {

        public AcquiredImageConfiguration() {
            HasKey(x => new { x.id });
            Property(x => x._metadata).HasColumnName("metadata");
        }
    }

}
