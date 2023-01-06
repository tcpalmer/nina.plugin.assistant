using NINA.Core.Model;
using System;

namespace Assistant.NINAPlugin.Astrometry {

    public class HorizonDefinition {

        private static readonly double HORIZON_VALUE = double.MinValue;
        private readonly double minimumAltitude;
        private readonly CustomHorizon horizon;
        private readonly double offset;

        public HorizonDefinition(double minimumAltitude) {
            this.minimumAltitude = minimumAltitude;
        }

        public HorizonDefinition(CustomHorizon customHorizon, double offset) {

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

        public double GetTargetAltitude(double azimuth) {
            if (minimumAltitude != HORIZON_VALUE) {
                return minimumAltitude;
            }

            return horizon.GetAltitude(azimuth) + offset;
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

    }

}
