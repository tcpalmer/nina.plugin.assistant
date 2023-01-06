using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Astrometry.Body;
using NINA.Astrometry.RiseAndSet;
using System;
using System.Globalization;
using System.Runtime.Caching;

namespace Assistant.NINAPlugin.Astrometry {

    public class TwilightTimeCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(6);
        private static readonly MemoryCache _cache = new MemoryCache("Assistant Twilight Times");

        public static RiseAndSetEvent Get(DateTime dateTime, double latitude, double longitude, int twilightInclude) {
            DateTime checkDate = dateTime.Date;
            string key = GetCacheKey(checkDate, latitude, longitude, twilightInclude);
            RiseAndSetEvent riseAndSetEvent = (RiseAndSetEvent)_cache.Get(key);

            if (riseAndSetEvent == null) {
                //Logger.Trace($"twilight time cache miss: {key}");
                switch (twilightInclude) {
                    case AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE:
                        riseAndSetEvent = GetNight(checkDate, latitude, longitude);
                        break;
                    case AssistantFilterPreferences.TWILIGHT_INCLUDE_ASTRO:
                        riseAndSetEvent = GetAstronomical(checkDate, latitude, longitude);
                        break;
                    case AssistantFilterPreferences.TWILIGHT_INCLUDE_NAUTICAL:
                        riseAndSetEvent = GetNautical(checkDate, latitude, longitude);
                        break;
                    case AssistantFilterPreferences.TWILIGHT_INCLUDE_CIVIL:
                        riseAndSetEvent = GetCivil(checkDate, latitude, longitude);
                        break;
                }

                _cache.Add(key, riseAndSetEvent, DateTime.Now.Add(ITEM_TIMEOUT));
            }
            else {
                //Logger.Trace($"twilight time cache hit: {key}");
            }

            return riseAndSetEvent;
        }

        private static RiseAndSetEvent GetCivil(DateTime dateTime, double latitude, double longitude) {
            return AstroUtil.GetSunRiseAndSet(dateTime, latitude, longitude);
        }

        private static RiseAndSetEvent GetNautical(DateTime dateTime, double latitude, double longitude) {
            return CivilTwilightRiseAndSet.GetCivilTwilightTimes(dateTime, latitude, longitude);
        }

        private static RiseAndSetEvent GetAstronomical(DateTime dateTime, double latitude, double longitude) {
            return AstroUtil.GetNauticalNightTimes(dateTime, latitude, longitude);
        }

        private static RiseAndSetEvent GetNight(DateTime dateTime, double latitude, double longitude) {
            return AstroUtil.GetNightTimes(dateTime, latitude, longitude);
        }

        private static string GetCacheKey(DateTime dateTime, double latitude, double longitude, int twilightInclude) {
            return $"{twilightInclude}{dateTime:yyyy-MM-dd-HH-mm-ss}_{latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_{longitude.ToString("0.000000", CultureInfo.InvariantCulture)}";
        }
    }

    // Following other NINA usage ...
    public class CivilTwilightRiseAndSet : RiseAndSetEvent {

        public CivilTwilightRiseAndSet(DateTime date, double latitude, double longitude) : base(date, latitude, longitude) {
        }

        public static RiseAndSetEvent GetCivilTwilightTimes(DateTime date, double latitude, double longitude) {
            var riseAndSet = new CivilTwilightRiseAndSet(date, latitude, longitude);
            var t = riseAndSet.Calculate().Result;

            return riseAndSet;
        }

        private double CivilTwilightDegree {
            get {
                return -6;
            }
        }

        protected override double AdjustAltitude(BasicBody body) {
            return body.Altitude - CivilTwilightDegree;
        }

        protected override BasicBody GetBody(DateTime date) {
            return new Sun(date, Latitude, Longitude);
        }
    }

}
