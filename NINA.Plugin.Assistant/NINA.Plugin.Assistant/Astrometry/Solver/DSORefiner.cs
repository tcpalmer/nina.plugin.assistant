using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Astrometry.Solver {

    /// <summary>
    /// DSORefiner generates altitudes and times from the equatorial coordinates of a target, a geographic
    /// location, and a date.  Provided coordinates should represent a deep space object or a star - not a
    /// solar system object.
    /// </summary>
    public class DSORefiner : IAltitudeRefiner {

        private ObserverInfo location;
        private Coordinates target;

        public DSORefiner(ObserverInfo location, Coordinates target) {
            Assert.notNull(location, "location cannot be null");
            Assert.notNull(target, "target cannot be null");

            this.location = location;
            this.target = target;
        }

        /// <summary>
        /// Add additional calculated points between the two elements in the altitudes list.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        public Altitudes Refine(Altitudes altitudes, int numPoints) {

            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count == 2, "altitudes must have exactly two elements");
            Assert.isTrue(numPoints > 0, "numPoints must be >= 1");

            AltitudeAtTime start = altitudes.AltitudeList[0];
            AltitudeAtTime end = altitudes.AltitudeList[1];

            List<AltitudeAtTime> newAltitudes = new List<AltitudeAtTime>(numPoints + 2);
            newAltitudes.Add(start);

            TimeSpan span = end.AtTime.Subtract(start.AtTime);
            long timeDiffSpanMS = (long)span.TotalMilliseconds;
            long timeIncrement = timeDiffSpanMS / (numPoints + 1);

            DateTime atTime = start.AtTime;
            for (int i = 0; i < numPoints; i++) {
                atTime = atTime.AddMilliseconds(timeIncrement);
                HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(location, target, atTime);
                newAltitudes.Add(new AltitudeAtTime(hc.Altitude, hc.Azimuth, atTime));
            }

            newAltitudes.Add(end);
            return new Altitudes(newAltitudes);
        }

        /// <summary>
        /// Generate altitude values for the target at location on date from midnight to midnight for every hour.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public Altitudes GetHourlyAltitudesForDay(DateTime date) {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(24);
            DateTime dateTime = date.Date;

            for (int i = 0; i < 24; i++) {
                HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(location, target, dateTime);
                alts.Add(new AltitudeAtTime(hc.Altitude, hc.Azimuth, dateTime));
                dateTime = dateTime.AddHours(1);
            }

            return new Altitudes(alts);
        }

        /// <summary>
        /// Return true if the target object ever rises at the location.  An object never rises at a location if it is
        /// circumpolar around the pole in the opposite hemisphere from location.
        /// </summary>
        /// <returns></returns>
        public bool RisesAtLocation() {
            return AstrometryUtils.RisesAtLocation(location, target);
        }

        /// <summary>
        /// Return true if the target object is circumpolar at the location.
        /// </summary>
        /// <returns></returns>
        public bool CircumpolarAtLocation() {
            return AstrometryUtils.CircumpolarAtLocation(location, target);
        }

        private double getAltitude(DateTime atTime) {
            throw new NotImplementedException();
        }
    }

}
