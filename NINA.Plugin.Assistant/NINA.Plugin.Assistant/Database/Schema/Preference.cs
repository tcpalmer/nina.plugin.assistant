using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ProjectPreference {

        [Key]
        public int id { get; set; }

        internal string _preferences { get; set; }

        [NotMapped]
        public AssistantProjectPreferences Preferences {
            get {
                return JsonConvert.DeserializeObject<AssistantProjectPreferences>(_preferences);
            }
            set {
                _preferences = JsonConvert.SerializeObject(value);
            }
        }

        public ProjectPreference() { }

        public ProjectPreference(AssistantProjectPreferences preferences) {
            this.Preferences = preferences;
        }
    }

    public class FilterPreference {

        [Key]
        public int id { get; set; }

        public string profileId { get; set; }
        public string filterName { get; set; }
        internal string _preferences { get; set; }

        [NotMapped]
        public AssistantFilterPreferences Preferences {
            get {
                return JsonConvert.DeserializeObject<AssistantFilterPreferences>(_preferences);
            }
            set {
                _preferences = JsonConvert.SerializeObject(value);
            }
        }

        public FilterPreference() { }

        public FilterPreference(string profileId, string filterName, AssistantFilterPreferences preferences) {
            this.profileId = profileId;
            this.filterName = filterName;
            this.Preferences = preferences;
        }
    }

    internal class ProjectPreferenceConfiguration : EntityTypeConfiguration<ProjectPreference> {

        public ProjectPreferenceConfiguration() {
            HasKey(x => new { x.id });
            Property(x => x._preferences).HasColumnName("preferences");
        }
    }

    internal class FilterPreferenceConfiguration : EntityTypeConfiguration<FilterPreference> {

        public FilterPreferenceConfiguration() {
            HasKey(x => new { x.id });
            Property(x => x._preferences).HasColumnName("preferences");
        }
    }

}
