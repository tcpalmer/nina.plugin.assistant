using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assistant.NINAPlugin.Astrometry.Solver {

    public class Altitudes {

        public List<AltitudeAtTime> AltitudeList { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }

        public Altitudes(List<AltitudeAtTime> altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.Count > 1, "altitudes must have at least two values");

            this.AltitudeList = altitudes;
            this.StartTime = altitudes[0].AtTime;
            this.EndTime = altitudes[altitudes.Count - 1].AtTime;

            Assert.isTrue(StartTime < EndTime, "startTime must be before endTime");

            DateTime cmp = StartTime.AddSeconds(-1);
            foreach (AltitudeAtTime altitude in altitudes) {
                Assert.isTrue(cmp < altitude.AtTime, "time is not always increasing");
                cmp = altitude.AtTime;
            }
        }

        public bool IsRisingAtEnd() {
            int lastPos = AltitudeList.Count - 1;
            return AltitudeList[lastPos].Altitude > AltitudeList[lastPos - 1].Altitude;
        }

        public int GetIntervalSeconds() {
            return (int)EndTime.Subtract(StartTime).TotalSeconds;
        }

        public Tuple<int, AltitudeAtTime> FindMaximumAltitude() {
            double alt = double.MinValue;
            AltitudeAtTime max = null;
            int pos = -1;

            for (int i = 0; i < AltitudeList.Count; i++) {
                if (AltitudeList[i].Altitude > alt) {
                    max = AltitudeList[i];
                    alt = AltitudeList[i].Altitude;
                    pos = i;
                }
            }

            return Tuple.Create(pos, max);
        }

        public Tuple<int, AltitudeAtTime> FindMinimumAltitude() {
            double alt = double.MaxValue;
            AltitudeAtTime min = null;
            int pos = -1;

            for (int i = 0; i < AltitudeList.Count; i++) {
                if (AltitudeList[i].Altitude < alt) {
                    min = AltitudeList[i];
                    alt = AltitudeList[i].Altitude;
                    pos = i;
                }
            }

            return Tuple.Create(pos, min);
        }

        /// <summary>
        /// Remove any leading ascending samples.  For daylight checks, a set of altitudes may run from local noon until the next noon.
        /// If local noon is before solar noon, then the altitudes will still be climbing for that span (which we don't want).
        /// </summary>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public Altitudes ClipAscendingStart() {

            List<AltitudeAtTime> alts = AltitudeList;
            if (alts[0].Altitude > alts[1].Altitude) {
                return this;
            }

            for (int i = 0; i < alts.Count - 1; i++) {
                if (alts[i].Altitude > alts[i + 1].Altitude) {
                    return new Altitudes(AltitudeList.GetRange(i, alts.Count - i));
                }
            }

            throw new ArgumentException("altitude list is unexpectedly always ascending");
        }

        /// <summary>
        /// Find the time span containing the target altitude.  If descending is true, it will detect the altitude in the
        /// region where the sequence is descending, otherwise where ascending.  The motivation is finding altitudes of interest
        /// such as horizon crossings.
        /// </summary>
        /// <param name="targetAltitude"></param>
        /// <param name="descending"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Altitudes FindSpan(double targetAltitude, bool descending) {
            List<AltitudeAtTime> alts = AltitudeList;

            // If descending, start from beginning
            if (descending) {
                for (int i = 0; i < alts.Count; i++) {
                    if (alts[i].Altitude < targetAltitude) {
                        List<AltitudeAtTime> span = new List<AltitudeAtTime>(2) {
                            alts[i-1],
                            alts[i]
                        };
                        return new Altitudes(span);
                    }
                }
            }
            else {
                // If ascending, find minimum and start from there
                Tuple<int, AltitudeAtTime> min = FindMinimumAltitude();

                // If the minimum is above the target, then the target is not present in this set of samples
                if (min.Item2.Altitude > targetAltitude) {
                    return null;
                }

                for (int i = min.Item1; i < alts.Count; i++) {
                    if (alts[i].Altitude > targetAltitude) {
                        List<AltitudeAtTime> span = new List<AltitudeAtTime>(2) {
                            alts[i-1],
                            alts[i]
                        };
                        return new Altitudes(span);
                    }
                }
            }

            // If the span can't be found, it might be after further refinement ...
            return null;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < AltitudeList.Count; i++) {
                AltitudeAtTime altitude = AltitudeList[i];
                sb.Append(String.Format("{0,2:F0} {1,9:F2} {2,9:F2} ", i, altitude.Altitude, altitude.Azimuth));
                sb.Append(altitude.AtTime.ToString("MM/dd/yyyy HH:mm:ss"));
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }

    public class AltitudeAtTime {

        public double Altitude { get; private set; }
        public double Azimuth { get; private set; }
        public DateTime AtTime { get; private set; }

        public AltitudeAtTime(double altitude, double azimuth, DateTime atTime) {
            Assert.isTrue(altitude <= 90 && altitude >= -90, "altitude must be <= 90 and >= -90");
            Assert.isTrue(azimuth >= 0 && azimuth <= 360, "azimuth must be >= 0 and <= 360");

            this.Altitude = altitude;
            this.Azimuth = azimuth;
            this.AtTime = atTime;
        }

        public override string ToString() {
            return "AltitudeAtTime{" + "altitude=" + Altitude + ", azimuth=" + Azimuth + ", atTime=" + AtTime + '}';
        }
    }

}
