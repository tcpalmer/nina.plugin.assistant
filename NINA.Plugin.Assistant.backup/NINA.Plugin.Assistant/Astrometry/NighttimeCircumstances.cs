using Assistant.NINAPlugin.Astrometry.Solver;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Astrometry.Body;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Caching;
using System.Text;

namespace Assistant.NINAPlugin.Astrometry {

    public enum TwilightLevel {
        Nighttime, Astronomical, Nautical, Civil
    };

    public enum TwilightStage {
        Dusk, Dawn
    };

    /// <summary>
    /// Determine the nightly twilight circumstances for the provided date.  The dusk (start) times will be on the provided date
    /// while the dawn (end) times will be on the following day (in general) - determining the potential imaging time span for
    /// a single 'night'.
    /// </summary>
    public class NighttimeCircumstances {

        public DateTime CivilTwilightStart { get; protected set; }
        public DateTime CivilTwilightEnd { get; protected set; }
        public DateTime? NauticalTwilightStart { get; protected set; }
        public DateTime? NauticalTwilightEnd { get; protected set; }
        public DateTime? AstronomicalTwilightStart { get; protected set; }
        public DateTime? AstronomicalTwilightEnd { get; protected set; }
        public DateTime? NighttimeStart { get; protected set; }
        public DateTime? NighttimeEnd { get; protected set; }

        public DateTime Sunset { get => CivilTwilightStart; }
        public DateTime Sunrise { get => CivilTwilightEnd; }

        private double SunAltitude { get => AstroUtil.ArcminToDegree(-50); } // refraction adjustment
        private double CivilSunAltitude { get => -6; }
        private double NauticalSunAltitude { get => -12; }
        private double AstronomicalSunAltitude { get => -18; }

        private readonly ObserverInfo observerInfo;
        private readonly DateTime onDate;

        public NighttimeCircumstances() { /* support testing */ }

        public static NighttimeCircumstances AdjustNighttimeCircumstances(ObserverInfo observerInfo, DateTime atTime) {
            NighttimeCircumstances nighttimeCircumstances = new NighttimeCircumstances(observerInfo, atTime);
            DateTime CivilTwilightStart = nighttimeCircumstances.CivilTwilightStart;
            DateTime CivilTwilightEnd = nighttimeCircumstances.CivilTwilightEnd;
            DateTime noon = atTime.Date.AddHours(12);
            DateTime midnight = atTime.Date.AddHours(24);

            // If atTime is between noon and civil start, return next dusk/following dawn
            if (noon <= atTime && atTime < CivilTwilightStart) {
                return nighttimeCircumstances;
            }

            // If atTime is between civil start/end and atTime is before midnight, return next dusk/following dawn
            if (CivilTwilightStart <= atTime && atTime < CivilTwilightEnd && atTime < midnight) {
                return nighttimeCircumstances;
            }

            // If atTime is after the previous dawn and before noon, return next dusk/following dawn
            NighttimeCircumstances previous = new NighttimeCircumstances(observerInfo, atTime.AddDays(-1));
            CivilTwilightEnd = previous.CivilTwilightEnd;
            if (CivilTwilightEnd <= atTime && atTime < noon) {
                return nighttimeCircumstances;
            }

            // Otherwise, we want NighttimeCircumstances for the previous day
            return previous;
        }

        public NighttimeCircumstances(ObserverInfo observerInfo, DateTime onDate) {
            this.observerInfo = observerInfo;
            this.onDate = onDate.Date.AddHours(12); // fix to noon on date

            if (AstrometryUtils.IsAbovePolarCircle(observerInfo)) {
                throw new ArgumentException("locations cannot be above a polar circle");
            }

            string cacheKey = GetCacheKey();
            TSLogger.Trace($"NighttimeCircumstances cache key: {cacheKey}");

            NighttimeCircumstances cached = NighttimeCircumstancesCache.Get(cacheKey);
            if (cached == null) {
                Calculate();
                NighttimeCircumstancesCache.Put(this, cacheKey);
            }
            else {
                this.CivilTwilightStart = cached.CivilTwilightStart;
                this.CivilTwilightEnd = cached.CivilTwilightEnd;
                this.NauticalTwilightStart = cached.NauticalTwilightStart;
                this.NauticalTwilightEnd = cached.NauticalTwilightEnd;
                this.AstronomicalTwilightStart = cached.AstronomicalTwilightStart;
                this.AstronomicalTwilightEnd = cached.AstronomicalTwilightEnd;
                this.NighttimeStart = cached.NighttimeStart;
                this.NighttimeEnd = cached.NighttimeEnd;
            }
        }

        public bool HasNighttime() { return NighttimeStart != null; }
        public bool HasAstronomicalTwilight() { return AstronomicalTwilightStart != null; }
        public bool HasNauticalTwilight() { return NauticalTwilightStart != null; }
        public bool HasCivilTwilight() { return CivilTwilightStart != null; }

        public TimeInterval GetTwilightSpan(TwilightLevel twilightLevel) {
            switch (twilightLevel) {
                case TwilightLevel.Nighttime: return SafeTwilightSpan(NighttimeStart, NighttimeEnd);
                case TwilightLevel.Astronomical: return SafeTwilightSpan(AstronomicalTwilightStart, AstronomicalTwilightEnd);
                case TwilightLevel.Nautical: return SafeTwilightSpan(NauticalTwilightStart, NauticalTwilightEnd);
                case TwilightLevel.Civil: return SafeTwilightSpan(CivilTwilightStart, CivilTwilightEnd);
                default:
                    throw new ArgumentException($"unknown twilight level: {twilightLevel}");
            }
        }

        public TimeInterval GetTwilightWindow(TwilightLevel twilightLevel, TwilightStage twilightStage) {

            if (twilightLevel == TwilightLevel.Nighttime) {
                return HasNighttime() ? SafeTwilightSpan(NighttimeStart, NighttimeEnd) : null;
            }

            if (twilightStage == TwilightStage.Dusk) {

                if (twilightLevel == TwilightLevel.Astronomical) {
                    if (HasAstronomicalTwilight()) {
                        return HasNighttime() ? SafeTwilightSpan(AstronomicalTwilightStart, NighttimeStart) : SafeTwilightSpan(AstronomicalTwilightStart, AstronomicalTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }

                if (twilightLevel == TwilightLevel.Nautical) {
                    if (HasNauticalTwilight()) {
                        return HasAstronomicalTwilight() ? SafeTwilightSpan(NauticalTwilightStart, AstronomicalTwilightStart) : SafeTwilightSpan(NauticalTwilightStart, NauticalTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }

                if (twilightLevel == TwilightLevel.Civil) {
                    if (HasCivilTwilight()) {
                        return HasNauticalTwilight() ? SafeTwilightSpan(CivilTwilightStart, NauticalTwilightStart) : SafeTwilightSpan(CivilTwilightStart, CivilTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }
            }
            else {
                if (twilightLevel == TwilightLevel.Astronomical) {
                    if (HasAstronomicalTwilight()) {
                        return HasNighttime() ? SafeTwilightSpan(NighttimeEnd, AstronomicalTwilightEnd) : SafeTwilightSpan(AstronomicalTwilightStart, AstronomicalTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }

                if (twilightLevel == TwilightLevel.Nautical) {
                    if (HasNauticalTwilight()) {
                        return HasAstronomicalTwilight() ? SafeTwilightSpan(AstronomicalTwilightEnd, NauticalTwilightEnd) : SafeTwilightSpan(NauticalTwilightStart, NauticalTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }

                if (twilightLevel == TwilightLevel.Civil) {
                    if (HasCivilTwilight()) {
                        return HasNauticalTwilight() ? SafeTwilightSpan(NauticalTwilightEnd, CivilTwilightEnd) : SafeTwilightSpan(CivilTwilightStart, CivilTwilightEnd);
                    }
                    else {
                        return null;
                    }
                }
            }

            return null;
        }

        private void Calculate() {
            DateTime start = onDate;
            DateTime end = start.AddDays(1);

            Altitudes altitudes = GetSamples(start, end, 24).ClipAscendingStart();

            // Since we're below a polar circle, we're guaranteed to have sun set/rise
            CivilTwilightStart = (DateTime)DetectAltitudeEvent(altitudes, SunAltitude, true);
            CivilTwilightEnd = (DateTime)DetectAltitudeEvent(altitudes, SunAltitude, false);

            // We are not, however, guaranteed to have twilight events.  In particular, at high latitudes
            // near the summer solstice we can lose true nighttime and even astro twilight while still below
            // the polar circle.  So if the minimum of the initial samples doesn't get below
            // AstronomicalSunAltitude, then resample at a higher rate from approximate sunset to sunrise.

            Tuple<int, AltitudeAtTime> min = altitudes.FindMinimumAltitude();
            if (min.Item2.Altitude > AstronomicalSunAltitude) {
                altitudes = ResampleForHighLatitudes(altitudes, min.Item1);
            }

            NauticalTwilightStart = DetectAltitudeEvent(altitudes, CivilSunAltitude, true);
            AstronomicalTwilightStart = DetectAltitudeEvent(altitudes, NauticalSunAltitude, true);
            NighttimeStart = DetectAltitudeEvent(altitudes, AstronomicalSunAltitude, true);
            NighttimeEnd = DetectAltitudeEvent(altitudes, AstronomicalSunAltitude, false);
            AstronomicalTwilightEnd = DetectAltitudeEvent(altitudes, NauticalSunAltitude, false);
            NauticalTwilightEnd = DetectAltitudeEvent(altitudes, CivilSunAltitude, false);
        }

        private Altitudes ResampleForHighLatitudes(Altitudes altitudes, int minPos) {

            // Restrict to approximate set-rise
            int startPos = 0;
            for (int i = minPos; i >= 0; i--) {
                if (altitudes.AltitudeList[i].Altitude > 0) {
                    startPos = i;
                    break;
                }
            }

            int endPos = 0;
            for (int i = minPos; i < altitudes.AltitudeList.Count; i++) {
                if (altitudes.AltitudeList[i].Altitude > 0) {
                    endPos = i;
                    break;
                }
            }

            List<AltitudeAtTime> span = new List<AltitudeAtTime>(2) {
                altitudes.AltitudeList[startPos],
                altitudes.AltitudeList[endPos]
            };

            DateTime start = altitudes.AltitudeList[startPos].AtTime;
            DateTime end = altitudes.AltitudeList[endPos].AtTime;

            // Do every 5 min and call it a day
            int samples = ((int)(end - start).TotalMinutes) / 60 * 12;
            return GetSamples(altitudes.AltitudeList[startPos].AtTime, altitudes.AltitudeList[endPos].AtTime, samples);
        }

        private DateTime? DetectAltitudeEvent(Altitudes altitudes, double targetAltitude, bool descending) {

            // Assuming initial sample is 24 (1h) then 24/6/10/6/5/2 (52 total) uses intervals of 1h, 10m, 1m, 10s, 2s, 1s
            int[] resample = { 6, 10, 6, 5, 2 };

            Altitudes span = altitudes.FindSpan(targetAltitude, descending);
            if (span == null) {
                return null;
            }

            for (int i = 0; i < resample.Length; i++) {
                int newSample = resample[i];
                DateTime spanStart = span.AltitudeList[0].AtTime;
                DateTime spanEnd = span.AltitudeList[1].AtTime;
                if ((spanEnd - spanStart).TotalSeconds <= newSample) {
                    break;
                }

                altitudes = GetSamples(spanStart, spanEnd, newSample);
                span = altitudes.FindSpan(targetAltitude, descending);
                if (span == null) {
                    return null;
                }
            }

            return descending ? span.AltitudeList[1].AtTime : span.AltitudeList[0].AtTime;
        }

        private Altitudes GetSamples(DateTime start, DateTime end, int samples) {

            int timeSpan = (int)(end - start).TotalSeconds;
            int timeStep = timeSpan / samples;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>(samples + 1);
            DateTime atTime = start;

            for (int i = 0; i <= samples; i++) {
                Sun sun = new Sun(atTime, observerInfo.Latitude, observerInfo.Longitude);
                sun.Calculate().Wait();
                // TODO: optimization: since this is the sun only, we can bail once altitude is > 0, ascending
                alts.Add(new AltitudeAtTime(sun.Altitude, 0, atTime));
                atTime = atTime.AddSeconds(timeStep);
            }

            return new Altitudes(alts);
        }

        private TimeInterval SafeTwilightSpan(DateTime? t1, DateTime? t2) {
            if (t1 == null || t2 == null) {
                return null;
            }

            return new TimeInterval((DateTime)t1, (DateTime)t2);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CivilTwilightStart:        {CivilTwilightStart}");
            sb.AppendLine($"NauticalTwilightStart:     {NauticalTwilightStart}");
            sb.AppendLine($"AstronomicalTwilightStart: {AstronomicalTwilightStart}");
            sb.AppendLine($"NighttimeStart:            {NighttimeStart}");
            sb.AppendLine($"NighttimeEnd:              {NighttimeEnd}");
            sb.AppendLine($"AstronomicalTwilightEnd:   {AstronomicalTwilightEnd}");
            sb.AppendLine($"NauticalTwilightEnd:       {NauticalTwilightEnd}");
            sb.AppendLine($"CivilTwilightEnd:          {CivilTwilightEnd}");
            return sb.ToString();
        }

        private string GetCacheKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{onDate:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append($"{observerInfo.Latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{observerInfo.Longitude.ToString("0.000000", CultureInfo.InvariantCulture)}");
            return sb.ToString();
        }

    }

    class NighttimeCircumstancesCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(12);
        private static readonly MemoryCache _cache = new MemoryCache("Scheduler NighttimeCircumstances");

        public static NighttimeCircumstances Get(string cacheKey) {
            return (NighttimeCircumstances)_cache.Get(cacheKey);
        }

        public static void Put(NighttimeCircumstances nighttimeCircumstances, string cacheKey) {
            _cache.Add(cacheKey, nighttimeCircumstances, DateTime.Now.Add(ITEM_TIMEOUT));
        }

        private NighttimeCircumstancesCache() { }
    }

}
