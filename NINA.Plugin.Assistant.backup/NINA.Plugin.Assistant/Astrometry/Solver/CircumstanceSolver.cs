using Assistant.NINAPlugin.Util;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Astrometry.Solver {

    public class CircumstanceSolver : Solver {

        public CircumstanceSolver(IAltitudeRefiner refiner) : base(refiner) { }

        public CircumstanceSolver(IAltitudeRefiner refiner, long maxFinalTimeStep) : base(refiner, maxFinalTimeStep) { }

        /// <summary>
        /// Find the rising time of the object to the specified accuracy.  The value returned will be the second sample in
        /// the final interval found where altitude goes positive and the sample interval is less than the defined maximum.
        /// This ensures that the value returned has a positive altitude.  If it is too great, decrease the maximum allowed
        /// interval and try again.
        /// 
        /// It is assumed that the set of sample altitudes does span the rising time(even if that step is wrapped in the set,
        /// from the last to the first point), otherwise null will be returned.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        public AltitudeAtTime FindRising(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");

            // Find step where altitude goes from - to +
            Altitudes targetStep = new RisingFunction().determineStep(altitudes);

            // List of altitudes may not contain a rising event
            if (targetStep == null) {
                return null;
            }

            long interval = GetStepInterval(targetStep);

            // If the step is already refined to the desired time span, we're done
            if (interval <= maxFinalStepTime) {
                return targetStep.AltitudeList[1];
            }

            // Generate a new set of samples between the target step samples and continue
            return FindRising(refiner.Refine(targetStep, 10));
        }

        /// <summary>
        /// Find the time when the object rises above a minimum value.  It is assumed that the set of sample altitudes does
        /// span this time (otherwise null will be returned).  The argument altitudes typically sample from the rising time
        /// to the transit to ensure sample coverage. The minimum value must also be less that the object's transit altitude,
        /// otherwise the minimum will never be reached.
        /// 
        /// A circumpolar object where lat + (dec-min) > 90 is always above the minimum(at least for Northern hemisphere).
        /// This case should be detected before calling this method.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <param name="horizonDefinition"></param>
        /// <returns></returns>
        public AltitudeAtTime FindRiseAboveMinimum(Altitudes altitudes, HorizonDefinition horizonDefinition) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.notNull(horizonDefinition, "horizonDefinition cannot be null");

            // Ensure the span does contain the crossing
            List<AltitudeAtTime> altitudeAtTimes = altitudes.AltitudeList;
            double targetAltitude = horizonDefinition.GetTargetAltitude(altitudeAtTimes[0]);
            if (altitudeAtTimes[0].Altitude > targetAltitude) {
                return null;
            }

            targetAltitude = horizonDefinition.GetTargetAltitude(altitudeAtTimes[altitudeAtTimes.Count - 1]);
            if (altitudeAtTimes[altitudeAtTimes.Count - 1].Altitude < targetAltitude) {
                return null;
            }

            // Find step where altitude crosses the minimum
            Altitudes targetStep = new RiseAboveMinimumFunction(horizonDefinition).determineStep(altitudes);

            // List of altitudes may not contain a crossing event (should never happen under normal circumstances
            if (targetStep == null) {
                return null;
            }

            long interval = GetStepInterval(targetStep);

            // If the step is already refined to the desired time span, we're done
            if (interval <= maxFinalStepTime) {
                return targetStep.AltitudeList[1];
            }

            // Generate a new set of samples between the target step samples and continue
            return FindRiseAboveMinimum(refiner.Refine(targetStep, 10), horizonDefinition);
        }

        /// <summary>
        /// Find the transit time(meridian crossing) of the object to the specified accuracy. The value returned will be
        /// the second sample in the final interval found where altitude reaches a maximum and the sample interval is
        /// less than the defined maximum interval.This ensures that the value returned has actually passed the meridian.
        /// If it is too far after, decrease the maximum allowed interval and try again.
        /// 
        /// It is assumed that the set of sample altitudes does span the transit time (even if that step is wrapped in the
        /// set, from the last to the first point), otherwise null will be returned.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        public AltitudeAtTime FindTransit(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");

            // Find step where altitude crosses the maximum
            Altitudes targetStep = new TransitFunction().determineStep(altitudes);

            // List of altitudes may not contain a transit event
            if (targetStep == null) {
                return null;
            }

            long interval = GetStepInterval(targetStep);

            // If the step is already refined to the desired time span, we're done
            if (interval <= maxFinalStepTime) {
                return targetStep.AltitudeList[1];
            }

            // Generate a new set of samples between the target step samples and continue
            return FindTransit(refiner.Refine(targetStep, 10));
        }

        /// <summary>
        /// Find the time when the object sets below a minimum value. It is assumed that the set of sample altitudes does
        /// span this time (otherwise null will be returned).  The argument altitudes typically sample from the transit time
        /// to setting to ensure sample coverage. The minimum value must also be less that the object's transit altitude,
        /// otherwise the minimum will never be reached.
        /// 
        /// A circumpolar object where lat + (dec-min) > 90 is always above the minimum(at least for Northern hemisphere).
        /// This case should be detected before calling this method.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <param name="horizonDefinition"></param>
        /// <returns></returns>
        public AltitudeAtTime FindSetBelowMinimum(Altitudes altitudes, HorizonDefinition horizonDefinition) {
            Assert.notNull(altitudes, "altitudes cannot be null");
            Assert.notNull(horizonDefinition, "horizonDefinition cannot be null");

            // Ensure the span does contain the crossing
            List<AltitudeAtTime> altitudeAtTimes = altitudes.AltitudeList;
            double targetAltitude = horizonDefinition.GetTargetAltitude(altitudeAtTimes[0]);
            if (altitudeAtTimes[0].Altitude < targetAltitude) {
                return null;
            }

            targetAltitude = horizonDefinition.GetTargetAltitude(altitudeAtTimes[altitudeAtTimes.Count - 1]);
            if (altitudeAtTimes[altitudeAtTimes.Count - 1].Altitude > targetAltitude) {
                return null;
            }

            // Find step where altitude crosses the minimum
            Altitudes targetStep = new SetBelowMinimumFunction(horizonDefinition).determineStep(altitudes);

            // List of altitudes may not contain a crossing event (should never happen under normal circumstances
            if (targetStep == null) {
                return null;
            }

            long interval = GetStepInterval(targetStep);

            // If the step is already refined to the desired time span, we're done
            if (interval <= maxFinalStepTime) {
                return targetStep.AltitudeList[0];
            }

            // Generate a new set of samples between the target step samples and continue
            return FindSetBelowMinimum(refiner.Refine(targetStep, 10), horizonDefinition);
        }

        /// <summary>
        /// Find the setting time of the object to the specified accuracy. The value returned will be the first sample in
        /// the final interval found where altitude goes negative and the sample interval is less than the defined maximum.
        /// This ensures that the value returned has a positive altitude. If it is too great, decrease the maximum allowed
        /// interval and try again.
        /// 
        /// It is assumed that the set of sample altitudes does span the setting time(even if that step is wrapped in the set,
        /// from the last to the first point), otherwise null will be returned.
        /// </summary>
        /// <param name="altitudes"></param>
        /// <returns></returns>
        public AltitudeAtTime FindSetting(Altitudes altitudes) {
            Assert.notNull(altitudes, "altitudes cannot be null");

            // Find step where altitude goes from + to -
            Altitudes targetStep = new SettingFunction().determineStep(altitudes);

            // List of altitudes may not contain a setting event
            if (targetStep == null) {
                return null;
            }

            long interval = GetStepInterval(targetStep);

            // If the step is already refined to the desired time span, we're done
            if (interval <= maxFinalStepTime) {
                return targetStep.AltitudeList[0];
            }

            // Generate a new set of samples between the target step samples and continue
            return FindSetting(refiner.Refine(targetStep, 10));
        }
    }

}
