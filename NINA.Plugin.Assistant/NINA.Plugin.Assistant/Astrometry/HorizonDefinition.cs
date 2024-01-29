using Assistant.NINAPlugin.Astrometry.Solver;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using System;
using System.IO;

namespace Assistant.NINAPlugin.Astrometry {

    public class HorizonDefinition {
        private bool isBasicMinimumAltitude;
        private readonly double minimumAltitude;
        private readonly CustomHorizon horizon;
        private readonly double offset;

        public HorizonDefinition(double minimumAltitude) {
            Assert.isTrue(minimumAltitude >= 0 && minimumAltitude < 90, "minimumAltitude must be >= 0 and < 90");
            this.minimumAltitude = minimumAltitude;
            this.isBasicMinimumAltitude = true;
        }

        public HorizonDefinition(CustomHorizon customHorizon, double offset, double minimumAltitude = 0) {
            Assert.isTrue(offset >= 0, "offset must be >= 0");
            Assert.isTrue(minimumAltitude >= 0 && minimumAltitude < 90, "minimumAltitude must be >= 0 and < 90");

            if (customHorizon != null) {
                this.minimumAltitude = minimumAltitude;
                this.isBasicMinimumAltitude = false;
                this.horizon = customHorizon;
                this.offset = offset;
            } else {
                // Protection against a weird horizon change in the profile
                this.minimumAltitude = 0;
            }
        }

        public double GetTargetAltitude(AltitudeAtTime aat) {
            if (isBasicMinimumAltitude) {
                return minimumAltitude;
            }

            double raw = Math.Max(horizon.GetAltitude(aat.Azimuth) + offset, minimumAltitude);
            return raw > 90 ? 90 : raw;
        }

        public bool IsCustom() {
            return !isBasicMinimumAltitude;
        }

        public double GetFixedMinimumAltitude() {
            if (IsCustom()) {
                throw new ArgumentException("minimumAltitude n/a in this context");
            }

            return this.minimumAltitude;
        }

        public string GetCacheKey() {
            if (isBasicMinimumAltitude) {
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
            return isBasicMinimumAltitude ? $"min alt: {minimumAltitude.ToString()}" : "custom";
        }

        public static CustomHorizon GetConstantHorizon(double altitude) {
            string alt = String.Format("{0:F0}", altitude);
            string horizonDefinition = $"0 {alt}" + Environment.NewLine
            + $"90 {alt}" + Environment.NewLine
            + $"180 {alt}" + Environment.NewLine
            + $"270 {alt}";

            using (var sr = new StringReader(horizonDefinition)) {
                return CustomHorizon.FromReader_Standard(sr);
            }
        }
    }
}