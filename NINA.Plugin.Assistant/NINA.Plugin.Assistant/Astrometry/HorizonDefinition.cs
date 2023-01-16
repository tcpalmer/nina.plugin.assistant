using Assistant.NINAPlugin.Astrometry.Solver;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using System;

namespace Assistant.NINAPlugin.Astrometry {


    public class HorizonDefinition {

        private static readonly double HORIZON_VALUE = double.MinValue;

        private readonly double minimumAltitude;
        private readonly CustomHorizon horizon;
        private readonly double offset;

        public HorizonDefinition(double minimumAltitude) {
            Assert.isTrue(minimumAltitude >= 0 && minimumAltitude < 90, "minimumAltitude must be >= 0 and < 90");
            this.minimumAltitude = minimumAltitude;
        }

        public HorizonDefinition(CustomHorizon customHorizon, double offset) {
            Assert.isTrue(offset >= 0, "offset must be >= 0");

            if (customHorizon != null) {
                this.minimumAltitude = HORIZON_VALUE;
                this.horizon = customHorizon;
                this.offset = offset;
            }
            else {
                // Protection against a weird horizon change in the profile
                this.minimumAltitude = 0;
            }
        }

        public double GetTargetAltitude(AltitudeAtTime aat) {
            if (minimumAltitude != HORIZON_VALUE) {
                return minimumAltitude;
            }

            return horizon.GetAltitude(aat.Azimuth) + offset;
        }

        public bool IsCustom() {
            return minimumAltitude == HORIZON_VALUE;
        }

        public double GetFixedMinimumAltitude() {
            if (IsCustom()) {
                throw new ArgumentException("minimumAltitude n/a in this context");
            }

            return this.minimumAltitude;
        }

        public string GetCacheKey() {
            if (minimumAltitude != HORIZON_VALUE) {
                return minimumAltitude.ToString();
            }

            // Something of a hack but CustomHorizon is closed up, hopefully reasonably unique
            double sum = horizon.GetMinAltitude() + horizon.GetMaxAltitude();
            sum += horizon.GetAltitude(0);
            sum += horizon.GetAltitude(45);
            sum += horizon.GetAltitude(90);
            sum += horizon.GetAltitude(135);
            sum += horizon.GetAltitude(180);
            sum += horizon.GetAltitude(225);
            sum += horizon.GetAltitude(270);
            sum += horizon.GetAltitude(315);

            return sum.ToString();
        }

        public override string ToString() {
            return minimumAltitude != HORIZON_VALUE ? $"min alt: {minimumAltitude.ToString()}" : "custom";
        }
    }

}
