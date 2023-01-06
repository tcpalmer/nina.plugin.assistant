using Assistant.NINAPlugin.Plan;
using NINA.Astrometry.RiseAndSet;
using NINA.Profile.Interfaces;
using System;

namespace Assistant.NINAPlugin.Astrometry {

    public class AstrometryUtils {

        /* Needed:
         *   - find above horizon (rise/set) times for a target
         */

        /// <summary>
        /// Get the imaging window (start/end times) for a target taking into account location, horizon, and twilight 
        /// </summary>
        /// <param name="forTime"></param>
        /// <param name="planTarget"></param>
        /// <param name="profile"></param>
        /// <param name="horizonDefinition"></param>
        /// <param name="twilightInclude"></param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> GetImagingWindow(DateTime forTime, PlanTarget planTarget, IProfile profile, HorizonDefinition horizonDefinition, int twilightInclude) {
            RiseAndSetEvent rs = TwilightTimeCache.Get(forTime, profile.AstrometrySettings.Latitude, profile.AstrometrySettings.Longitude, twilightInclude);

            // TODO: Determine target rise above and set below horizon times

            return null;
        }

        private AstrometryUtils() {

        }
    }

}
