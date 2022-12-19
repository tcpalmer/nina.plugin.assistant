using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Preferences {

        [Key]
        public int id { get; set; }

        // TODO: not clear how we want to organize this.  Maybe each row is a key/value where value is a string ...
    }

    internal class PreferencesConfiguration : EntityTypeConfiguration<Preferences> {

        public PreferencesConfiguration() {
            ToTable("dbo.preferences");
            HasKey(x => x.id);
        }

    }

}
