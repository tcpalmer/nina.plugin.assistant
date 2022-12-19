using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Target {

        [Key]
        public int id { get; set; }

        [Required]
        public string name { get; set; }

        [Required]
        public double ra { get; set; }

        [Required]
        public double dec { get; set; }

        public double rotation { get; set; }
        public double roi { get; set; }

        [ForeignKey("project")]
        public int projectid { get; set; }
        public Project project { get; set; }

        public List<ExposurePlan> exposureplans { get; set; }

        public Target() {
            rotation = 0;
            roi = 1;
            exposureplans = new List<ExposurePlan>();
        }
    }

    /*
    internal class TargetConfiguration : EntityTypeConfiguration<Target> {

        public TargetConfiguration() {
            ToTable("dbo.target");
            HasKey(x => x.id);
            Property(x => x.name).HasColumnName("name").IsRequired();
            Property(x => x.ra).HasColumnName("ra").IsRequired();
            Property(x => x.dec).HasColumnName("dec").IsRequired();
        }

    }*/
}
