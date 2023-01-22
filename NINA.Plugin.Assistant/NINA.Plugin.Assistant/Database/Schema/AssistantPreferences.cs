using Assistant.NINAPlugin.Astrometry;
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

        public int MinimumTime { get; set; }
        public double MinimumAltitude { get; set; }
        public bool UseCustomHorizon { get; set; }
        public double HorizonOffset { get; set; }
        public int FilterSwitchFrequency { get; set; }
        public int DitherEvery { get; set; }
        public bool EnableGrader { get; set; }

        public Dictionary<string, double> RuleWeights { get; set; }

        public override void SetDefaults() {
            MinimumTime = 30;
            MinimumAltitude = 0;
            UseCustomHorizon = false;
            HorizonOffset = 0;
            FilterSwitchFrequency = 0;
            DitherEvery = 0;
            EnableGrader = false;
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
            sb.AppendLine($"MinimumTime: {MinimumTime}");
            sb.AppendLine($"MinimumAltitude: {MinimumAltitude}");
            sb.AppendLine($"UseCustomHorizon: {UseCustomHorizon}");
            sb.AppendLine($"HorizonOffset: {HorizonOffset}");
            sb.AppendLine($"FilterSwitchFrequency: {FilterSwitchFrequency}");
            sb.AppendLine($"DitherEvery: {DitherEvery}");
            sb.AppendLine($"EnableGrader: {EnableGrader}");

            StringBuilder rw = new StringBuilder();
            foreach (KeyValuePair<string, double> entry in RuleWeights) {
                rw.Append($"{entry.Key}: {entry.Value}, ");
            }

            sb.AppendLine("RuleWeights: ").Append(rw.ToString());
            return sb.ToString();
        }
    }

    public class AssistantFilterPreferences : AssistantPreferences {

        public TwilightLevel TwilightLevel { get; set; }
        public bool MoonAvoidanceEnabled { get; set; }
        public double MoonAvoidanceSeparation { get; set; }
        public int MoonAvoidanceWidth { get; set; }
        public double MaximumHumidity { get; set; }

        public override void SetDefaults() {
            TwilightLevel = TwilightLevel.Nighttime;
            MoonAvoidanceEnabled = false;
            MoonAvoidanceSeparation = 0;
            MoonAvoidanceWidth = 0;
            MaximumHumidity = 0;
        }
        public bool IsTwilightNightOnly() {
            return TwilightLevel == TwilightLevel.Nighttime;
        }

        public bool IsTwilightAstronomical() {
            return TwilightLevel == TwilightLevel.Astronomical;
        }

        public bool IsTwilightNautical() {
            return TwilightLevel == TwilightLevel.Nautical;
        }

        public bool IsTwilightCivil() {
            return TwilightLevel == TwilightLevel.Civil;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"TwilightLevel: {TwilightLevel}");
            sb.AppendLine($"MoonAvoidanceEnabled: {MoonAvoidanceEnabled}");
            sb.AppendLine($"MoonAvoidanceSeparation: {MoonAvoidanceSeparation}");
            sb.AppendLine($"MoonAvoidanceWidth: {MoonAvoidanceWidth}");
            sb.AppendLine($"MaximumHumidity: {MaximumHumidity}");
            return sb.ToString();
        }
    }

}
