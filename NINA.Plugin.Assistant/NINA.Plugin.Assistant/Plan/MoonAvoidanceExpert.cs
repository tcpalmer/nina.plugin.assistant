using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using System;

namespace Assistant.NINAPlugin.Plan {

    public class MoonAvoidanceExpert {
        private ObserverInfo observerInfo;

        public MoonAvoidanceExpert(ObserverInfo observerInfo) {
            this.observerInfo = observerInfo;
        }

        public MoonAvoidanceExpert(IProfile activeProfile) {
            this.observerInfo = new ObserverInfo {
                Latitude = activeProfile.AstrometrySettings.Latitude,
                Longitude = activeProfile.AstrometrySettings.Longitude,
                Elevation = activeProfile.AstrometrySettings.Elevation,
            };
        }

        // TODO: use AVERAGE ALTITIDE for relaxation

        public bool IsRejected(IPlanTarget planTarget, IPlanExposure planExposure) {
            double planMoonAltitude = GetPlanMaxMoonAltitude(planTarget);

            if (!planExposure.MoonAvoidanceEnabled) {
                return false;
            }

            // Avoidance is completely off if the moon is below the relaxation min altitude and relaxation applies
            if (planMoonAltitude <= planExposure.MoonRelaxMinAltitude && planExposure.MoonRelaxScale > 0) {
                return false;
            }

            double moonSeparationParameter = planExposure.MoonAvoidanceSeparation;
            double moonWidthParameter = planExposure.MoonAvoidanceWidth;

            // If moon altitude is in the relaxation zone, then modulate the separation and width parameters
            if (planMoonAltitude <= planExposure.MoonRelaxMaxAltitude && planExposure.MoonRelaxScale > 0) {
                moonSeparationParameter = moonSeparationParameter + (planExposure.MoonRelaxScale * (planMoonAltitude - planExposure.MoonRelaxMaxAltitude));
                moonWidthParameter = moonWidthParameter * ((planMoonAltitude - planExposure.MoonRelaxMinAltitude) / (planExposure.MoonRelaxMaxAltitude - planExposure.MoonRelaxMinAltitude));
            }

            // If the separation was relaxed into oblivion, avoidance is off
            if (moonSeparationParameter <= 0) {
                TSLogger.Warning($"moon avoidance separation was relaxed below zero, avoidance off");
                return false;
            }

            // Determine avoidance
            DateTime midPointTime = Utils.GetMidpointTime(planTarget.StartTime, planTarget.EndTime);
            double moonAge = GetMoonAge(midPointTime);
            double moonSeparation = GetMoonSeparationAngle(observerInfo, midPointTime, planTarget.Coordinates);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge,
                moonSeparationParameter, moonWidthParameter);

            bool rejected = moonSeparation < moonAvoidanceSeparation;
            TSLogger.Debug($"moon avoidance {planTarget.Name}/{planExposure.FilterName} rejected={rejected}, midpoint={midPointTime}, moonSep={moonSeparation}, moonAvoidSep={moonAvoidanceSeparation}");

            return rejected;
        }

        public virtual double GetPlanMaxMoonAltitude(IPlanTarget planTarget) {
            double altitudeAtStart = AstroUtil.GetMoonAltitude(planTarget.StartTime, observerInfo);
            double altitudeAtEnd = AstroUtil.GetMoonAltitude(planTarget.EndTime, observerInfo);
            return Math.Max(altitudeAtStart, altitudeAtEnd);
        }

        public virtual double GetMoonAge(DateTime atTime) {
            return AstrometryUtils.GetMoonAge(atTime);
        }

        public virtual double GetMoonSeparationAngle(ObserverInfo location, DateTime atTime, Coordinates coordinates) {
            return AstrometryUtils.GetMoonSeparationAngle(observerInfo, atTime, coordinates);
        }
    }
}