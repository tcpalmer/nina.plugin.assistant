﻿using System;

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