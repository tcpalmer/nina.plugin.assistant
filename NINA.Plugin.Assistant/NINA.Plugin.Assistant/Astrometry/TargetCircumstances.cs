using Assistant.NINAPlugin.Astrometry.Solver;
using NINA.Astrometry;
using NINA.Core.Utility;
using System;
using System.Globalization;
using System.Runtime.Caching;
using System.Text;

namespace Assistant.NINAPlugin.Astrometry {

    public class TargetCircumstances {

        public DateTime RiseAboveHorizonTime { get; private set; }
        public DateTime SetBelowHorizonTime { get; private set; }
        public DateTime CulminationTime { get; private set; }
        public int TimeOnTargetSeconds { get; private set; }
        public bool IsVisible { get; private set; }

        private readonly Coordinates coordinates;
        private readonly ObserverInfo observerInfo;
        private readonly HorizonDefinition horizonDefinition;
        private readonly DateTime startTime;
        private readonly DateTime endTime;

        public TargetCircumstances(Coordinates coordinates, ObserverInfo observerInfo, HorizonDefinition horizonDefinition, Tuple<DateTime, DateTime> twilightSpan) {
            this.coordinates = coordinates;
            this.observerInfo = observerInfo;
            this.horizonDefinition = horizonDefinition;
            this.startTime = twilightSpan.Item1;
            this.endTime = twilightSpan.Item2;

            string cacheKey = GetCacheKey();
            Logger.Info($"TargetCircumstances cache key: {cacheKey}");

            TargetCircumstances targetCircumstances = TargetCircumstancesCache.GetTargetCircumstances(cacheKey);
            if (targetCircumstances == null) {
                Calculate();
                TargetCircumstancesCache.PutTargetCircumstances(this, cacheKey);
            }
            else {
                this.RiseAboveHorizonTime = targetCircumstances.RiseAboveHorizonTime;
                this.SetBelowHorizonTime = targetCircumstances.SetBelowHorizonTime;
                this.CulminationTime = targetCircumstances.CulminationTime;
                this.TimeOnTargetSeconds = targetCircumstances.TimeOnTargetSeconds;
                this.IsVisible = targetCircumstances.IsVisible;
            }
        }

        private void Calculate() {
            TargetImagingCircumstances tc = new TargetImagingCircumstances(observerInfo, coordinates, startTime, endTime, horizonDefinition);

            int status = tc.Analyze();
            this.IsVisible = status == TargetImagingCircumstances.STATUS_POTENTIALLY_VISIBLE;

            if (IsVisible) {
                this.RiseAboveHorizonTime = tc.RiseAboveMinimumTime;
                this.SetBelowHorizonTime = tc.SetBelowMinimumTime;
                this.CulminationTime = tc.TransitTime;

                DateTime actualStart = DateTime.Now > RiseAboveHorizonTime ? DateTime.Now : RiseAboveHorizonTime;
                this.TimeOnTargetSeconds = (int)(SetBelowHorizonTime - actualStart).TotalSeconds;
            }
            else {
                this.TimeOnTargetSeconds = 0;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"RiseAboveHorizonTime: {RiseAboveHorizonTime}");
            sb.AppendLine($"SetAboveHorizonTime:  {SetBelowHorizonTime}");
            sb.AppendLine($"CulminationTime:      {CulminationTime}");
            sb.AppendLine($"IsVisible:            {IsVisible}");
            return sb.ToString();
        }

        private string GetCacheKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{startTime:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append($"{endTime:yyyy-MM-dd-HH-mm-ss}_");
            sb.Append($"{observerInfo.Latitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{observerInfo.Longitude.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{observerInfo.Elevation.ToString("0.##", CultureInfo.InvariantCulture)}_");
            sb.Append($"{coordinates.RADegrees.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{coordinates.Dec.ToString("0.000000", CultureInfo.InvariantCulture)}_");
            sb.Append($"{coordinates.Epoch.ToString()}");
            return sb.ToString();
        }
    }

    class TargetCircumstancesCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(12);
        private static readonly MemoryCache _cache = new MemoryCache("Assistant TargetCircumstances");

        public static TargetCircumstances GetTargetCircumstances(string cacheKey) {
            return (TargetCircumstances)_cache.Get(cacheKey);
        }

        public static void PutTargetCircumstances(TargetCircumstances targetCircumstances, string cacheKey) {
            _cache.Add(cacheKey, targetCircumstances, DateTime.Now.Add(ITEM_TIMEOUT));
        }

        private TargetCircumstancesCache() { }
    }

}
