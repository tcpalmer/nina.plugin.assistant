using System;

namespace Assistant.NINAPlugin.Util {

    public class Utils {

        public static string MtoHM(int minutes) {
            decimal hours = Math.Floor((decimal)minutes / 60);
            int min = minutes % 60;
            return $"{hours}h {min}m";
        }

        public static string FormatDateTimeFull(DateTime dateTime) {
            return String.Format("{0:MM/dd/yyyy HH:mm:ss zzzz}", dateTime);
        }

        public static DateTime GetMidpointTime(DateTime startTime, DateTime endTime) {
            long span = (long)endTime.Subtract(startTime).TotalSeconds;
            return startTime.AddSeconds(span / 2);
        }
    }

}
