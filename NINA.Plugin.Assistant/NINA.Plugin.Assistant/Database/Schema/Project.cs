using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Project {

        public const int STATE_DRAFT = 0;
        public const int STATE_ACTIVE = 1;
        public const int STATE_INACTIVE = 2;
        public const int STATE_CLOSED = 3;

        [Key]
        public int id { get; set; }

        [Required]
        public string name { get; set; }

        public string description { get; set; }

        [Required]
        public int state { get; set; }

        [Required]
        public int priority { get; set; }

        public long createdate { get; set; }
        public long? activedate { get; set; }
        public long? inactivedate { get; set; }

        public List<Target> targets { get; set; }

        public Project() {
            state = STATE_DRAFT;
            priority = 1;
            createdate = AssistantDbContext.DateTimeToUnixSeconds(DateTime.Now);
            targets = new List<Target>();
        }

    }

    /*
    internal class ProjectConfiguration : EntityTypeConfiguration<Project> {

        public ProjectConfiguration() {
            ToTable("dbo.project");
            HasKey(x => x.id);
            Property(x => x.name).HasColumnName("name").IsRequired();
            Property(x => x.state).HasColumnName("state").IsRequired();
            Property(x => x.createdate).HasColumnName("createdate").IsRequired();
        }

    }*/
}
