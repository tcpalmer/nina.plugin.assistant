using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Preference {

        public const int TYPE_GLOBAL = 0;
        public const int TYPE_FILTER = 1;

        public int type { get; set; }
        public string profileId { get; set; }
        public string filterName { get; private set; }
        internal string _preferences { get; set; }

        [NotMapped]
        public AssistantPreferences preferences {
            get { return _preferences == null ? new AssistantPreferences() : JsonConvert.DeserializeObject<AssistantPreferences>(_preferences); }
            set { _preferences = JsonConvert.SerializeObject(value); }
        }

        public Preference() { }

        public Preference(string profileId, AssistantPreferences preferences) {
            this.type = TYPE_GLOBAL;
            this.profileId = profileId;
            this.filterName = "";
            this.preferences = preferences;
        }

        public Preference(string profileId, string filterName, AssistantPreferences preferences) {
            this.type = TYPE_FILTER;
            this.profileId = profileId;
            this.filterName = filterName;
            this.preferences = preferences;
        }
    }

    internal class PreferenceConfiguration : EntityTypeConfiguration<Preference> {

        public PreferenceConfiguration() {
            HasKey(x => new { x.type, x.profileId, x.filterName });
            Property(x => x._preferences).HasColumnName("preferences");
        }
    }

}
