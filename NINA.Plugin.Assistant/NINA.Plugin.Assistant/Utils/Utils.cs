using NINA.Astrometry;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Util {

    public class Utils {

        public static readonly string DateFMT = "yyyy-MM-dd HH:mm:ss";

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

        public static string CopiedItemName(string name) {
            if (string.IsNullOrEmpty(name)) {
                return " (1)";
            }

            Regex re = new Regex(@"^(.*) \((\d+)\)$");
            Match match = re.Match(name);
            if (match.Success) {
                string baseName = match.Groups[1].Value;
                int count = int.Parse(match.Groups[2].Value);
                return $"{baseName} ({count + 1})";
            }

            return name + " (1)";
        }

        public static string FormatDateTimeFull(DateTime? dateTime) {
            return dateTime == null ? "n/a" : String.Format("{0:yyyy-MM-dd HH:mm:ss zzzz}", dateTime);
        }

        public static string FormatCoordinates(Coordinates coordinates) {
            return coordinates == null ? "n/a" : $"{coordinates.RAString} {coordinates.DecString}";
        }

        public static DateTime GetMidpointTime(DateTime startTime, DateTime endTime) {
            long span = (long)endTime.Subtract(startTime).TotalSeconds;
            return startTime.AddSeconds(span / 2);
        }

        // Cobbled from NINA (NINA private)
        public static string GetRAString(double raDegrees) {
            string pattern = "{0:0}h {1:0}m {2:0}s";
            double hours = AstroUtil.DegreesToHours(raDegrees);

            bool negative = false;
            if (hours < 0) {
                negative = true;
                hours = -hours;
            }
            if (negative) {
                pattern = "-" + pattern;
            }

            var degree = Math.Floor(hours);
            var arcmin = Math.Floor(AstroUtil.DegreeToArcmin(hours - degree));
            var arcminDeg = AstroUtil.ArcminToDegree(arcmin);

            var arcsec = Math.Round(AstroUtil.DegreeToArcsec(hours - degree - arcminDeg), 0);
            if (arcsec == 60) {
                /* If arcsec got rounded to 60 add to arcmin instead */
                arcsec = 0;
                arcmin += 1;

                if (arcmin == 60) {
                    /* If arcmin got rounded to 60 add to degree instead */
                    arcmin = 0;
                    degree += 1;
                }
            }

            return string.Format(pattern, degree, arcmin, arcsec);
        }

        public static bool IsCancelException(Exception ex) {
            if (ex == null) { return false; }

            if (ex is TaskCanceledException) { return true; }
            if (ex is OperationCanceledException) { return true; }
            if (ex.Message.Contains("canceled")) { return true; }
            if (ex.Message.Contains("cancelled")) { return true; }

            if (ex.InnerException != null) {
                return IsCancelException(ex.InnerException);
            }

            return false;
        }

        private Utils() { }
    }

}
