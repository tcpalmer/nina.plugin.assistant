using NINA.Astrometry;
using System;
using System.Text.RegularExpressions;

namespace Assistant.NINAPlugin.Util {

    public class Utils {

        public static string MtoHM(int minutes) {
            decimal hours = Math.Floor((decimal)minutes / 60);
            int min = minutes % 60;
            return $"{hours}h {min}m";
        }

        public static int HMtoM(string hm) {

            if (string.IsNullOrEmpty(hm)) {
                return 0;
            }

            Regex re = new Regex(@"(\d+)h\s*(\d+)m");
            Match match = re.Match(hm);
            if (match.Success) {
                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                return hours * 60 + minutes;
            }

            return 0;
        }

        public static string FormatDateTimeFull(DateTime? dateTime) {
            return dateTime == null ? "n/a" : String.Format("{0:MM/dd/yyyy HH:mm:ss zzzz}", dateTime);
        }

        public static string FormatCoordinates(Coordinates coordinates) {
            return coordinates == null ? "n/a" : $"{coordinates.RAString} {coordinates.DecString}";
        }

        public static DateTime GetMidpointTime(DateTime startTime, DateTime endTime) {
            long span = (long)endTime.Subtract(startTime).TotalSeconds;
            return startTime.AddSeconds(span / 2);
        }
    }

}
