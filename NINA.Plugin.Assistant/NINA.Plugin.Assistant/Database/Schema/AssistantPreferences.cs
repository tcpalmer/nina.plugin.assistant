using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public class AssistantPreferences {

        /*
         * I think we're going to want to store prefs at the 'global' level (per profile) as well as
         * at the filter level.
         * 
         * Could also have a Dictionary<string, AssistantPreferences> which maps either 'global' or
         * filterName to the applicable prefs.  Should be loaded when the context.PreferencePlanSet
         * is loaded.
         */

        public const int TWILIGHT_INCLUDE_NONE = 0;
        public const int TWILIGHT_INCLUDE_ASTRO = 1;
        public const int TWILIGHT_INCLUDE_NAUTICAL = 2;
        public const int TWILIGHT_INCLUDE_CIVIL = 3;

        public double MinimumAltitude { get; set; }
        public bool UseCustomHorizon { get; set; }
        // TODO: offset for custom horizon
        public int TwilightInclude { get; set; }

        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }

        public bool MeridianWindowEnabled { get; set; }
        public int MeridianWindowMinutes { get; set; }

        public AssistantPreferences() {
            SetDefaults();
        }

        public void SetDefaults() {
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            TwilightInclude = TWILIGHT_INCLUDE_NONE;

            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 0;
            MoonAvoidanceWidth = 0;

            MeridianWindowEnabled = false;
            MeridianWindowMinutes = 0;
        }

        public bool IsTwilightNightOnly() {
            return TwilightInclude == TWILIGHT_INCLUDE_NONE;
        }

        public bool IsTwilightAstronomical() {
            return TwilightInclude == TWILIGHT_INCLUDE_ASTRO;
        }

        public bool IsTwilightNautical() {
            return TwilightInclude == TWILIGHT_INCLUDE_NAUTICAL;
        }

        public bool IsTwilightCivil() {
            return TwilightInclude == TWILIGHT_INCLUDE_CIVIL;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("MinimumAltitude: ").Append(MinimumAltitude).AppendLine();
            sb.Append("UseCustomHorizon: ").Append(UseCustomHorizon).AppendLine();
            sb.Append("TwilightInclude: ").Append(TwilightInclude).AppendLine();
            sb.Append("MoonAvoidanceEnabled: ").Append(MoonAvoidanceEnabled).AppendLine();
            sb.Append("MoonAvoidanceSeparation: ").Append(MoonAvoidanceSeparation).AppendLine();
            sb.Append("MoonAvoidanceWidth: ").Append(MoonAvoidanceWidth).AppendLine();
            sb.Append("MeridianWindowEnabled: ").Append(MeridianWindowEnabled).AppendLine();
            sb.Append("MeridianWindowMinutes: ").Append(MeridianWindowMinutes).AppendLine();
            return sb.ToString();
        }
    }

}
