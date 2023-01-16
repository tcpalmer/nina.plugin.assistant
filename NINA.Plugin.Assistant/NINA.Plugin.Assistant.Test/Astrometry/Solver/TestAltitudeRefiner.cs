using Assistant.NINAPlugin.Astrometry.Solver;
using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Astrometry.Solver {

    public class TestAltitudeRefiner : IAltitudeRefiner {

        private readonly bool risesAtlocation;

        private readonly bool circumpolar;

        public TestAltitudeRefiner() {
            risesAtlocation = true;
            circumpolar = false;
        }

        public TestAltitudeRefiner(bool risesAtlocation, bool circumpolar) {
            this.risesAtlocation = risesAtlocation;
            this.circumpolar = circumpolar;
        }

        Altitudes IAltitudeRefiner.Refine(Altitudes altitudes, int numPoints) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count == 2, "altitudes must have exactly two elements");
            Assert.isTrue(numPoints > 0, "numPoints must be >= 1");

            AltitudeAtTime start = altitudes.AltitudeList[0];
            AltitudeAtTime end = altitudes.AltitudeList[1];

            List<AltitudeAtTime> newAltitudes = new List<AltitudeAtTime>(numPoints + 2);
            newAltitudes.Add(start);

            double altDiff = end.Altitude - start.Altitude;
            double altIncrement = altDiff / (numPoints + 1);

            long timeDiffSpanMS = (long)end.AtTime.Subtract(start.AtTime).TotalMilliseconds;
            long timeIncrement = timeDiffSpanMS / (numPoints + 1);

            double altitude = start.Altitude;
            DateTime atTime = start.AtTime;
            for (int i = 0; i < numPoints; i++) {
                altitude += altIncrement;
                atTime = atTime.AddMilliseconds(timeIncrement);
                newAltitudes.Add(new AltitudeAtTime(altitude, 180, atTime));
            }

            newAltitudes.Add(end);

            Assert.isTrue(newAltitudes.Count == numPoints + 2,
                            "new altitudes list is unexpected size: " + newAltitudes.Count);

            return new Altitudes(newAltitudes);
        }

        Altitudes IAltitudeRefiner.GetHourlyAltitudesForDay(DateTime date) {
            Assert.notNull(date, "date cannot be null");

            double altitude = 0;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            DateTime dateTime = date.Date;

            for (int i = 0; i < 24; i++) {
                alts.Add(new AltitudeAtTime(altitude, 180, dateTime));
                altitude += getAltitudeDelta(i);
                dateTime = dateTime.AddHours(1);
            }

            return new Altitudes(alts);
        }

        bool IAltitudeRefiner.RisesAtLocation() {
            return risesAtlocation;
        }

        bool IAltitudeRefiner.CircumpolarAtLocation() {
            return circumpolar;
        }

        private double getAltitudeDelta(int hour) {
            if ((hour >= 0 && hour < 6) || (hour >= 18 && hour < 24)) {
                return 15;
            }

            return -15;
        }
    }

}
