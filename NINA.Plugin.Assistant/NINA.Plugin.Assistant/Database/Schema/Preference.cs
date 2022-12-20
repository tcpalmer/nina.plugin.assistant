using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Assistant.NINAPlugin.Database.Schema {

    public class Preference {

        // TODO: unique constraint on id:filterName ?

        [Key]
        public int id { get; set; }
        public string profileId { get; set; }
        public string filterName { get; private set; }
        public string json { get; set; }

        public Preference(AssistantPreferences assistantPreferences) {
            filterName = null;
            json = JsonConvert.SerializeObject(assistantPreferences);
        }
    }

    // internal class PreferenceConfiguration : IEntityTypeConfiguration<Preference> {

    //public void Configure(EntityTypeBuilder<Preference> builder) {

    //}
    // }
}
