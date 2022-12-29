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
    }
}
