using Assistant.NINAPlugin.Util;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Astrometry.Solver {

    public abstract class Solver {

        private static readonly long DEFAULT_MAX_STEP_TIME = 1; // seconds

        protected readonly IAltitudeRefiner refiner;

        protected readonly long maxFinalStepTime;

        public Solver(IAltitudeRefiner refiner) : this(refiner, DEFAULT_MAX_STEP_TIME) { }

        public Solver(IAltitudeRefiner refiner, long maxFinalTimeStep) {
            Assert.notNull(refiner, "refiner cannot be null");
            Assert.isTrue(maxFinalTimeStep >= 1, "max final time step must be >= 1");

            this.refiner = refiner;
            this.maxFinalStepTime = maxFinalTimeStep;
        }

        public static long GetDeltaTimeMS(Altitudes altitudes) {
            List<AltitudeAtTime> alts = altitudes.AltitudeList;
            return (long)alts[1].AtTime.Subtract(alts[0].AtTime).TotalMilliseconds;
        }

        public long GetStepInterval(Altitudes step) {
            Assert.notNull(step, "step cannot be null");
            Assert.isTrue(step.AltitudeList.Count == 2, "altitudes must have exactly two points");
            return (long)step.EndTime.Subtract(step.StartTime).TotalSeconds;
        }
    }

    interface StepFunction {
        Altitudes determineStep(Altitudes altitudes);
    }

    public class RisingFunction : StepFunction {

        public Altitudes determineStep(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count > 1, "altitudes must have at least two points");

            List<AltitudeAtTime> list = altitudes.AltitudeList;

            double lastAltitude = list[0].Altitude;
            bool lastIsPositive = lastAltitude >= 0;
            double currentAltitude;
            bool currentIsPositive;

            for (int i = 1; i < list.Count; i++) {
                currentAltitude = list[i].Altitude;
                currentIsPositive = currentAltitude >= 0;

                if (lastIsPositive != currentIsPositive && currentIsPositive) {
                    List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                    step.Add(list[i - 1]);
                    step.Add(list[i]);
                    return new Altitudes(step);
                }

                lastAltitude = currentAltitude;
                lastIsPositive = lastAltitude >= 0;
            }

            // Test for wrap around, e.g. if first is 1 and last is -1 then step is last->first.
            // In this case, the time for the last element is assumed to be first - (diff between 1st and 2nd step).
            // In general, this will be true since a list of altitudes will almost always be generated with the same
            // interval between samples.

            lastAltitude = list[list.Count - 1].Altitude;
            lastIsPositive = lastAltitude >= 0;
            currentAltitude = list[0].Altitude;
            currentIsPositive = currentAltitude >= 0;

            if (lastIsPositive != currentIsPositive && currentIsPositive) {
                List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                AltitudeAtTime aatFirst = list[0];
                AltitudeAtTime aatLast = list[list.Count - 1];

                DateTime time = aatFirst.AtTime.AddMilliseconds(-1 * Solver.GetDeltaTimeMS(altitudes));
                step.Add(new AltitudeAtTime(aatLast.Altitude, aatLast.Azimuth, time));
                step.Add(aatFirst);
                return new Altitudes(step);
            }

            // Altitude does not contain a rising event
            return null;
        }
    }

    public class RiseAboveMinimumFunction : StepFunction {

        private readonly HorizonDefinition horizonDefinition;

        public RiseAboveMinimumFunction(HorizonDefinition horizonDefinition) {
            this.horizonDefinition = horizonDefinition;
        }

        public Altitudes determineStep(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count > 1, "altitudes must have at least two points");

            List<AltitudeAtTime> list = altitudes.AltitudeList;

            // If the first sample is above, there can't be a minimum crossing
            double targetAltitude = horizonDefinition.GetTargetAltitude(list[0]);
            if (list[0].Altitude > targetAltitude) {
                return null;
            }

            for (int i = 1; i < list.Count; i++) {
                targetAltitude = horizonDefinition.GetTargetAltitude(list[i]);

                if (list[i].Altitude > targetAltitude) {
                    List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                    step.Add(list[i - 1]);
                    step.Add(list[i]);
                    return new Altitudes(step);
                }
            }

            return null;
        }
    }

    class TransitFunction : StepFunction {

        public Altitudes determineStep(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count > 2, "altitudes must have at least three points");

            List<AltitudeAtTime> list = altitudes.AltitudeList;
            double maxAltitude = 0;
            int maxIndex = -1;

            for (int i = 0; i < list.Count; i++) {
                double altitude = list[i].Altitude;
                if (altitude > maxAltitude) {
                    maxAltitude = altitude;
                    maxIndex = i;
                }
            }

            if (maxIndex == -1) {
                throw new InvalidOperationException("failed to determine max altitude for transit, altitude never > 0");
            }

            List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);

            // Unlike rising or setting, we can't identify a single interval that contains a maximum.  Instead,
            // we have to find the two interval step that contains the maximum which can then be further refined.
            // If the maximum is the first or last element, then the step needs to wrap around properly.

            // If we wrap, we have to adjust the time of the shifted element so that they remain properly ordered.
            // This uses the existing sample to sample time interval which should be valid since a list of altitudes
            // will almost always be generated with the same interval between samples.

            // If the maximum is first, we need to wrap from the last element to the 2nd to encompass the maximum.
            if (maxIndex == 0) {

                AltitudeAtTime first = list[altitudes.AltitudeList.Count - 1];
                long diff = (long)list[1].AtTime.Subtract(list[0].AtTime).TotalMilliseconds;
                DateTime revisedTime = list[0].AtTime.AddMilliseconds(-1 * diff);
                AltitudeAtTime adjusted = new AltitudeAtTime(first.Altitude, first.Azimuth, revisedTime);

                step.Add(adjusted);
                step.Add(list[1]);

                return new Altitudes(step);
            }

            // If the maximum is last, we need to wrap from the second to the last element to the first to
            // encompass the maximum.  Time is adjusted for the old first to be new second.
            if (maxIndex == altitudes.AltitudeList.Count - 1) {

                AltitudeAtTime first = list[0];
                long diff = (long)list[1].AtTime.Subtract(list[0].AtTime).TotalMilliseconds;
                DateTime revisedTime = list[maxIndex].AtTime.AddMilliseconds(diff);
                AltitudeAtTime adjusted = new AltitudeAtTime(first.Altitude, first.Azimuth, revisedTime);

                step.Add(list[altitudes.AltitudeList.Count - 2]);
                step.Add(adjusted);

                return new Altitudes(step);
            }

            // Otherwise, the maximum was not the first or last
            step.Add(list[maxIndex - 1]);
            step.Add(list[maxIndex + 1]);
            return new Altitudes(step);
        }
    }

    class SetBelowMinimumFunction : StepFunction {

        private readonly HorizonDefinition horizonDefinition;

        public SetBelowMinimumFunction(HorizonDefinition horizonDefinition) {
            this.horizonDefinition = horizonDefinition;
        }

        public Altitudes determineStep(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count > 1, "altitudes must have at least two points");

            List<AltitudeAtTime> list = altitudes.AltitudeList;

            // If rising at the end, we need to find minimum and continue search (backwards) from there.
            int pos = altitudes.AltitudeList.Count - 1;
            if (altitudes.IsRisingAtEnd()) {
                Tuple<int, AltitudeAtTime> min = altitudes.FindMinimumAltitude();
                pos = min.Item1;

            }
            else {
                // If not rising at end and last sample is above, there can't be a minimum crossing
                double targetAltitude = horizonDefinition.GetTargetAltitude(list[altitudes.AltitudeList.Count - 1]);
                if (list[altitudes.AltitudeList.Count - 1].Altitude > targetAltitude) {
                    return null;
                }
            }

            // Walk the list backwards looking for the minimum crossing
            for (int i = pos; i > 0; i--) {
                double targetAltitude = horizonDefinition.GetTargetAltitude(list[i - 1]);

                if (list[i - 1].Altitude > targetAltitude) {
                    List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                    step.Add(list[i - 1]);
                    step.Add(list[i]);
                    return new Altitudes(step);
                }
            }

            return null;
        }
    }

    class SettingFunction : StepFunction {

        public Altitudes determineStep(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.isTrue(altitudes.AltitudeList.Count > 1, "altitudes must have at least two points");

            List<AltitudeAtTime> list = altitudes.AltitudeList;

            double lastAltitude = list[0].Altitude;
            bool lastIsPositive = lastAltitude >= 0;
            double currentAltitude;
            bool currentIsPositive;

            for (int i = 1; i < list.Count; i++) {
                currentAltitude = list[i].Altitude;
                currentIsPositive = currentAltitude >= 0;

                if (lastIsPositive != currentIsPositive && lastIsPositive) {
                    List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                    step.Add(list[i - 1]);
                    step.Add(list[i]);
                    return new Altitudes(step);
                }

                lastAltitude = currentAltitude;
                lastIsPositive = lastAltitude >= 0;
            }

            // Test for wrap around, e.g. if first is -1 and last is 1 then step is last->first.
            // In this case, the time for the last element is assumed to be first - (diff between 1st and 2nd step).
            // In general, this will be true since a list of altitudes will almost always be generated with the same
            // interval between samples.

            lastAltitude = list[list.Count - 1].Altitude;
            lastIsPositive = lastAltitude >= 0;
            currentAltitude = list[0].Altitude;
            currentIsPositive = currentAltitude >= 0;

            if (lastIsPositive != currentIsPositive && lastIsPositive) {
                List<AltitudeAtTime> step = new List<AltitudeAtTime>(2);
                AltitudeAtTime aatFirst = list[0];
                AltitudeAtTime aatLast = list[list.Count - 1];
                DateTime time = aatFirst.AtTime.AddMilliseconds(-1 * Solver.GetDeltaTimeMS(altitudes));

                step.Add(new AltitudeAtTime(aatLast.Altitude, aatLast.Azimuth, time));
                step.Add(aatFirst);
                return new Altitudes(step);
            }

            // Altitude does not contain a setting event
            return null;
        }
    }
}
