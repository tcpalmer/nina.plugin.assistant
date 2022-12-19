using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ExposurePlan {

        [Key]
        public int id { get; set; }

        [Required]
        public string filtername { get; set; }

        [Required]
        public int filterpos { get; set; }

        [Required]
        public double exposure { get; set; }

        public int gain { get; set; }
        public int offset { get; set; }
        public int bin { get; set; }

        public int desired { get; set; }
        public int acquired { get; set; }
        public int accepted { get; set; }

        [ForeignKey("target")]
        public int targetid { get; set; }
        public Target target { get; set; }

        public ExposurePlan() {
            exposure = 60;
            gain = 0;
            offset = 0;
            bin = 1;
            desired = 1;
            acquired = 0;
            accepted = 0;
        }

    }

    /*
    internal class ExposurePlanConfiguration : EntityTypeConfiguration<ExposurePlan> {

        public ExposurePlanConfiguration() {
            ToTable("dbo.exposureplan");
            HasKey(x => x.id);
            Property(x => x.filtername).HasColumnName("filtername").IsRequired();
            Property(x => x.filterpos).HasColumnName("filterpos").IsRequired();
            Property(x => x.exposure).HasColumnName("exposure").IsRequired();
        }

    }*/
}
