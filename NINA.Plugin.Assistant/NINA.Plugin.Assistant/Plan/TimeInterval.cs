using Assistant.NINAPlugin.Util;
using System;

namespace Assistant.NINAPlugin.Plan {

    public class TimeInterval {

        public DateTime StartTime { get; private set; }
        public DateTime EndTime { get; private set; }
        public long Duration { get; private set; }

        public TimeInterval(DateTime startTime, DateTime endTime) {

            if (startTime > endTime) {
                throw new ArgumentException("startTime must be before endTime");
            }

            StartTime = startTime;
            EndTime = endTime;
            Duration = (long)(endTime - startTime).TotalSeconds;
        }

        public TimeInterval Overlap(TimeInterval ti2) {
            if (StartTime > ti2.EndTime) {
                return null;
            }

            if (EndTime < ti2.StartTime) {
                return null;
            }

            if (ti2.StartTime < StartTime && ti2.EndTime <= EndTime) {
                return new TimeInterval(StartTime, ti2.EndTime);
            }

            if (StartTime < ti2.StartTime && EndTime <= ti2.EndTime) {
                return new TimeInterval(ti2.StartTime, EndTime);
            }

            if (StartTime < ti2.StartTime && ti2.EndTime <= EndTime) {
                return new TimeInterval(ti2.StartTime, ti2.EndTime);
            }

            if (ti2.StartTime < StartTime && EndTime <= ti2.EndTime) {
                return new TimeInterval(StartTime, EndTime);
            }

            return new TimeInterval(StartTime, EndTime);
        }

        public override string ToString() {
            return $"{Utils.FormatDateTimeFull(StartTime)} - {Utils.FormatDateTimeFull(EndTime)}";
        }

        public static TimeInterval GetTotalTimeSpan(TimeInterval first, params TimeInterval[] values) {
            DateTime start = first.StartTime;
            DateTime end = first.EndTime;

            for (int i = 0; i < values.Length; i++) {
                TimeInterval val = values[i];
                if (val.StartTime < start) start = val.StartTime;
                if (val.EndTime > end) end = val.EndTime;
            }

            return new TimeInterval(start, end);
        }
    }
}
