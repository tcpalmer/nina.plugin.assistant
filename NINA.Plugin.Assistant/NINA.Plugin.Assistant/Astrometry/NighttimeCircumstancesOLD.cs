using NINA.Astrometry;
using NINA.Astrometry.Body;
using NINA.Astrometry.RiseAndSet;
using NINA.Core.Utility;
using System;
using System.Globalization;
using System.Runtime.Caching;
using System.Text;

namespace Assistant.NINAPlugin.Astrometry {

    public class NighttimeCircumstancesOLD {

        public DateTime CivilTwilightStart { get; private set; }
        public DateTime CivilTwilightEnd { get; private set; }
        public DateTime NauticalTwilightStart { get; private set; }
        public DateTime NauticalTwilightEnd { get; private set; }
        public DateTime AstronomicalTwilightStart { get; private set; }
        public DateTime AstronomicalTwilightEnd { get; private set; }
        public DateTime NighttimeStart { get; private set; }
        public DateTime NighttimeEnd { get; private set; }

        public DateTime SunSet { get => CivilTwilightStart; }
        public DateTime SunRise { get => CivilTwilightEnd; }

        // TODO: other getters for the various twilight include times

        private readonly ObserverInfo observerInfo;
        private readonly DateTime onDate;

        public NighttimeCircumstancesOLD(ObserverInfo observerInfo, DateTime onDate) {
            this.observerInfo = observerInfo;
            this.onDate = onDate.Date.AddHours(12); // fix to noon on date

            string cacheKey = GetCacheKey();
            Logger.Info($"NighttimeCircumstances cache key: {cacheKey}");

            NighttimeCircumstancesOLD nighttimeCircumstances = NighttimeCircumstancesCacheOLD.GetNighttimeCircumstances(cacheKey);
            if (nighttimeCircumstances == null) {
                Calculate();
                NighttimeCircumstancesCacheOLD.PutNighttimeCircumstances(this, cacheKey);
            }
            else {
                this.CivilTwilightStart = nighttimeCircumstances.CivilTwilightStart;
                this.CivilTwilightEnd = nighttimeCircumstances.CivilTwilightEnd;
                this.NauticalTwilightStart = nighttimeCircumstances.NauticalTwilightStart;
                this.NauticalTwilightEnd = nighttimeCircumstances.NauticalTwilightEnd;
                this.AstronomicalTwilightStart = nighttimeCircumstances.AstronomicalTwilightStart;
                this.AstronomicalTwilightEnd = nighttimeCircumstances.AstronomicalTwilightEnd;
                this.NighttimeStart = nighttimeCircumstances.NighttimeStart;
                this.NighttimeEnd = nighttimeCircumstances.NighttimeEnd;
            }
        }

        private void Calculate() {
            if (AstrometryUtils.IsAbovePolarCircle(observerInfo)) {
                throw new ArgumentException("Assistant: does not support locations above a polar circle");
            }

            RiseAndSetEvent sun = AstroUtil.GetSunRiseAndSet(onDate, observerInfo.Latitude, observerInfo.Longitude);
            RiseAndSetEvent nautical = CivilTwilightRiseAndSet.GetCivilTwilightTimes(onDate, observerInfo.Latitude, observerInfo.Longitude);
            RiseAndSetEvent astro = AstroUtil.GetNauticalNightTimes(onDate, observerInfo.Latitude, observerInfo.Longitude);
            RiseAndSetEvent night = AstroUtil.GetNightTimes(onDate, observerInfo.Latitude, observerInfo.Longitude);

            CivilTwilightStart = (DateTime)sun.Set;
            CivilTwilightEnd = (DateTime)sun.Rise;
            NauticalTwilightStart = (DateTime)nautical.Set;
            NauticalTwilightEnd = (DateTime)nautical.Rise;
            AstronomicalTwilightStart = (DateTime)astro.Set;
            AstronomicalTwilightEnd = (DateTime)astro.Rise;
            NighttimeStart = (DateTime)night.Set;
            NighttimeEnd = (DateTime)night.Rise;
        }

        private string GetCacheKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{onDate:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append($"{observerInfo.Latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{observerInfo.Longitude.ToString("0.000000", CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"SunSet/CivilTwilightStart: {CivilTwilightStart}");
            sb.AppendLine($"NauticalTwilightStart:     {NauticalTwilightStart}");
            sb.AppendLine($"AstronomicalTwilightStart: {AstronomicalTwilightStart}");
            sb.AppendLine($"NighttimeStart:            {NighttimeStart}");
            sb.AppendLine($"NighttimeEnd:              {NighttimeEnd}");
            sb.AppendLine($"AstronomicalTwilightEnd:   {AstronomicalTwilightEnd}");
            sb.AppendLine($"NauticalTwilightEnd:       {NauticalTwilightEnd}");
            sb.AppendLine($"SunRise/CivilTwilightEnd:  {CivilTwilightEnd}");
            return sb.ToString();
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

    class NighttimeCircumstancesCacheOLD {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(12);
        private static readonly MemoryCache _cache = new MemoryCache("Assistant NighttimeCircumstances");

        public static NighttimeCircumstancesOLD GetNighttimeCircumstances(string cacheKey) {
            return (NighttimeCircumstancesOLD)_cache.Get(cacheKey);
        }

        public static void PutNighttimeCircumstances(NighttimeCircumstancesOLD nighttimeCircumstances, string cacheKey) {
            _cache.Add(cacheKey, nighttimeCircumstances, DateTime.Now.Add(ITEM_TIMEOUT));
        }

        private NighttimeCircumstancesCacheOLD() { }
    }

}
