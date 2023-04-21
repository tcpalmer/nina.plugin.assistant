using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Utility;
using System;

namespace Assistant.NINAPlugin.Astrometry {

    public class AstrometryUtils {

        private const double DAYS_IN_LUNAR_CYCLE = 29.53059;

        /// <summary>
        /// Return the horizontal coordinates for the coordinates at the specific location and time.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="atTime"></param>
        /// <returns>horizontal coordinates</returns>
        public static HorizontalCoordinate GetHorizontalCoordinates(ObserverInfo location, Coordinates coordinates, DateTime atTime) {
            Assert.notNull(location, "location cannot be null");
            Assert.notNull(coordinates, "coordinates cannot be null");

            double siderealTime = AstroUtil.GetLocalSiderealTime(atTime, location.Longitude);
            double hourAngle = AstroUtil.GetHourAngle(siderealTime, coordinates.RA);
            double degAngle = AstroUtil.HoursToDegrees(hourAngle);

            double altitude = AstroUtil.GetAltitude(degAngle, location.Latitude, coordinates.Dec);
            double azimuth = AstroUtil.GetAzimuth(degAngle, altitude, location.Latitude, coordinates.Dec);

            return new HorizontalCoordinate(altitude, azimuth);
        }

        /// <summary>
        /// Return the altitude for the coordinates at the specific location and time.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="atTime"></param>
        /// <returns></returns>
        public static double GetAltitude(ObserverInfo location, Coordinates coordinates, DateTime atTime) {
            double siderealTime = AstroUtil.GetLocalSiderealTime(atTime, location.Longitude);
            double hourAngle = AstroUtil.GetHourAngle(siderealTime, coordinates.RA);
            double degAngle = AstroUtil.HoursToDegrees(hourAngle);
            return AstroUtil.GetAltitude(degAngle, location.Latitude, coordinates.Dec);
        }

        /// <summary>
        /// Get the moon illumination fraction at time.  Code was copied from NINA AstroUtils.CalculateMoonIllumination
        /// which is private.
        /// </summary>
        /// <param name="atTime"></param>
        /// <returns></returns>
        public static double GetMoonIllumination(DateTime atTime) {

            var jd = AstroUtil.GetJulianDate(atTime);
            var tuple = AstroUtil.GetMoonAndSunPosition(atTime, jd);
            var moonPosition = tuple.Item1;
            var sunPosition = tuple.Item2;

            var sunRAAngle = Angle.ByHours(sunPosition.RA);
            var sunDecAngle = Angle.ByDegree(sunPosition.Dec);
            var moonRAAngle = Angle.ByHours(moonPosition.RA);
            var moonDecAngle = Angle.ByDegree(moonPosition.Dec);

            var phi = (
                sunDecAngle.Sin() * moonDecAngle.Sin()
                + sunDecAngle.Cos() * moonDecAngle.Cos() * (sunRAAngle - moonRAAngle).Cos()
                ).Acos();

            var phaseAngle = Angle.Atan2(
                sunPosition.Dis * phi.Sin(),
                moonPosition.Dis - sunPosition.Dis * phi.Cos()
            );

            var illuminatedFraction = (1.0 + phaseAngle.Cos().Radians) / 2.0;
            return illuminatedFraction;
        }

        /// <summary>
        /// Determine the angle in degrees between the moon and a target at location and time.  Copied from
        /// NINA MoonInfo.CalculateSeparation() which is private.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="atTime"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static double GetMoonSeparationAngle(ObserverInfo location, DateTime atTime, Coordinates target) {

            NOVAS.SkyPosition pos = AstroUtil.GetMoonPosition(atTime, AstroUtil.GetJulianDate(atTime), location);
            var moonRaRadians = AstroUtil.ToRadians(AstroUtil.HoursToDegrees(pos.RA));
            var moonDecRadians = AstroUtil.ToRadians(pos.Dec);

            Coordinates targetJNow = target.Transform(Epoch.JNOW);
            var targetRaRadians = AstroUtil.ToRadians(targetJNow.RADegrees);
            var targetDecRadians = AstroUtil.ToRadians(targetJNow.Dec);

            var theta = SOFA.Seps(moonRaRadians, moonDecRadians, targetRaRadians, targetDecRadians);
            return AstroUtil.ToDegree(theta);
        }

        /// <summary>
        /// Determine the moon avoidance separation for the moon age and separation angle (distance) to the target.
        /// 
        /// Basically, distance is selected to be the minimum acceptable separation at full moon.  Width is then the
        /// number of days (before or after full) for the acceptable separation to drop to distance/2.
        /// 
        /// The Moon Avoidance Lorentzian concept is from the Berkeley Automated Imaging Telescope (BAIT) team.  See
        /// http://astron.berkeley.edu/~bait/.  This formulation is from ACP, see
        /// http://bobdenny.com/ar/RefDocs/HelpFiles/ACPScheduler81Help/Constraints.htm.
        /// </summary>
        /// <param name="moonAge"></param>
        /// <param name="distance"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static double GetMoonAvoidanceLorentzianSeparation(double moonAge, double distance, int width) {
            if (width == 0) {
                TSLogger.Error("moon avoidance width cannot be zero");
                return 0;
            }

            // The ACP page has a typo in the formula - missing parens.  The JavaScript on that page shows:
            //     (d / (1.0 + Math.pow(((0.5 - (a / 29.5)) / (w / 29.5)), 2)))
            // With that change, this duplicates ACP.

            return distance / (1 + Math.Pow((0.5 - (moonAge / DAYS_IN_LUNAR_CYCLE)) / (width / DAYS_IN_LUNAR_CYCLE), 2));
        }

        /// <summary>
        /// Get the moon's age in days at time.
        /// </summary>
        /// <param name="atTime"></param>
        /// <returns>age in days</returns>
        public static double GetMoonAge(DateTime atTime) {
            double moonPA = AstroUtil.GetMoonPositionAngle(atTime);
            moonPA = moonPA > 0 ? moonPA : (180 + moonPA) + 180;
            return moonPA * (DAYS_IN_LUNAR_CYCLE / 360);
        }

        /// <summary>
        /// Return true if the target object can ever rise above the horizon at the location.  Note that this doesn't
        /// necessarily mean that the target has a rising event (which a circumpolar target would not), just that it
        /// can be above the horizon at some point.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <returns>true/false</returns>
        public static bool RisesAtLocation(ObserverInfo location, Coordinates coordinates) {
            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object will never rise above the local horizon if dec - lat is less than -90° (observer in Northern Hemisphere),
            // or dec - lat is greater than +90° (observer in Southern Hemisphere)
            return !((declination - latitude) < -90) && !(declination - latitude > 90);
        }

        /// <summary>
        /// Return true if the target object can ever rise above the horizon at the location when a minimum viewing altitude
        /// is considered.  Note that this doesn't necessarily mean that the target has a rising event (which a circumpolar
        /// target would not), just that it can be above the horizon at some point.
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="minimumAltitude"></param>
        /// <returns>true/false</returns>
        public static bool RisesAtLocationWithMinimumAltitude(ObserverInfo location, Coordinates coordinates, double minimumAltitude) {
            Assert.notNull(location, "location cannot be null");
            Assert.notNull(coordinates, "coordinates cannot be null");
            Assert.isTrue(minimumAltitude > 0, "minimumAltitude must be > 0");

            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object will never rise above the local horizon with a minimum altitude if (dec-min) - lat is less than -90°
            // (observer in Northern Hemisphere), or (dec+min) - lat is greater than +90° (observer in Southern Hemisphere)
            return !((declination - minimumAltitude - latitude) < -90) && !(declination + minimumAltitude - latitude > 90);
        }

        /// <summary>
        /// Return true if the target object is circumpolar at the location.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <returns>true/false</returns>
        public static bool CircumpolarAtLocation(ObserverInfo location, Coordinates coordinates) {
            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object is circumpolar if lat + dec is greater than +90° (observer in Northern Hemisphere), or lat + dec is less
            // than -90° (observer in Southern Hemisphere)
            return (latitude + declination) > 90 || latitude + declination < -90;
        }

        /// <summary>
        /// Return true if the target object is still 'circumpolar' (has no rising or setting) when a minimum viewing
        /// altitude is considered.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="coordinates"></param>
        /// <param name="minimumAltitude"></param>
        /// <returns>true/false</returns>
        public static bool CircumpolarAtLocationWithMinimumAltitude(ObserverInfo location, Coordinates coordinates, double minimumAltitude) {
            Assert.notNull(location, "location cannot be null");
            Assert.notNull(coordinates, "coordinates cannot be null");
            Assert.isTrue(minimumAltitude > 0, "minimumAltitude must be > 0");

            double declination = coordinates.Dec;
            double latitude = location.Latitude;

            // The object is 'circumpolar with minimum' if lat + (dec-min) is greater than +90° (observer in Northern Hemisphere),
            // or lat + (dec+min) is less than -90° (observer in Southern Hemisphere)
            return (latitude + (declination - minimumAltitude)) > 90 || (latitude + (declination + minimumAltitude)) < -90;
        }

        /// <summary>
        /// Return true if the location latitude is above the corresponding polar (arctic or antarctic) circle.
        /// </summary>
        /// <param name="location"></param>
        /// <returns>true/false</returns>
        public static bool IsAbovePolarCircle(ObserverInfo location) {
            return location.Latitude >= 66.6 || location.Latitude <= -66.6;
        }

        private AstrometryUtils() { }
    }

}
