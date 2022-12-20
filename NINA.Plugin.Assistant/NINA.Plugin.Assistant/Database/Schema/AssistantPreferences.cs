namespace Assistant.NINAPlugin.Database.Schema {

    public class AssistantPreferences {

        public const int TWILIGHT_INCLUDE_NONE = 0;
        public const int TWILIGHT_INCLUDE_ASTRO = 1;
        public const int TWILIGHT_INCLUDE_NAUTICAL = 2;
        public const int TWILIGHT_INCLUDE_CIVIL = 3;

        public double MinimumAltitude { get; set; }
        public bool UseCustomHorizon { get; set; }
        public int TwilightInclude { get; set; }

        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }

        public bool MeridianWindowEnabled { get; set; }
        public int MeridianWindowMinutes { get; set; }

        public AssistantPreferences() {
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            TwilightInclude = TWILIGHT_INCLUDE_NONE;

            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 0;
            MoonAvoidanceWidth = 0;

            MeridianWindowEnabled = false;
            MeridianWindowMinutes = 0;
        }

    }

}
