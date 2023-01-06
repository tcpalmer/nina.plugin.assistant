using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Database.Schema {

    public abstract class AssistantPreferences {

        public AssistantPreferences() {
            SetDefaults();
        }

        public abstract void SetDefaults();
    }

    public class AssistantProjectPreferences : AssistantPreferences {

        public double MinimumAltitude { get; set; }
        public bool UseCustomHorizon { get; set; }
        public double HorizonOffset { get; set; }

        public int MinimumTime { get; set; }
        public bool EnableGrader { get; set; }

        public Dictionary<string, double> RuleWeights { get; set; }

        public override void SetDefaults() {
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            HorizonOffset = 0;
            MinimumTime = 30;
            EnableGrader = true;
            RuleWeights = new Dictionary<string, double>();
        }

        public void AddRuleWeight(string key, double weight) {
            if (RuleWeights == null) {
                RuleWeights = new Dictionary<string, double>();
            }

            if (RuleWeights.ContainsKey(key)) {
                RuleWeights.Remove(key);
            }

            RuleWeights.Add(key, weight);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("MinimumAltitude: ").Append(MinimumAltitude).AppendLine();
            sb.Append("UseCustomHorizon: ").Append(UseCustomHorizon).AppendLine();
            sb.Append("HorizonOffset: ").Append(HorizonOffset).AppendLine();
            sb.Append("MinimumTime: ").Append(MinimumTime).AppendLine();
            sb.Append("EnableGrader: ").Append(EnableGrader).AppendLine();

            StringBuilder rw = new StringBuilder();
            foreach (KeyValuePair<string, double> entry in RuleWeights) {
                rw.Append($"{entry.Key}: {entry.Value}, ");
            }

            sb.Append("RuleWeights: ").Append(rw.ToString()).AppendLine();
            return sb.ToString();
        }
    }

    public class AssistantFilterPreferences : AssistantPreferences {

        // some logic depends on this integer order - so don't change
        public const int TWILIGHT_INCLUDE_NONE = 0;
        public const int TWILIGHT_INCLUDE_ASTRO = 1;
        public const int TWILIGHT_INCLUDE_NAUTICAL = 2;
        public const int TWILIGHT_INCLUDE_CIVIL = 3;

        public int TwilightInclude { get; set; }
        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }
        public double MaximumHumidity { get; set; }

        public override void SetDefaults() {
            TwilightInclude = TWILIGHT_INCLUDE_NONE;
            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 0;
            MoonAvoidanceWidth = 0;
            MaximumHumidity = 0;
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
            sb.Append("TwilightInclude: ").Append(TwilightInclude).AppendLine();
            sb.Append("MoonAvoidanceEnabled: ").Append(MoonAvoidanceEnabled).AppendLine();
            sb.Append("MoonAvoidanceSeparation: ").Append(MoonAvoidanceSeparation).AppendLine();
            sb.Append("MoonAvoidanceWidth: ").Append(MoonAvoidanceWidth).AppendLine();
            sb.Append("MaximumHumidity: ").Append(MaximumHumidity).AppendLine();
            return sb.ToString();
        }
    }

}
