using Assistant.NINAPlugin.Astrometry;
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

        public bool IsRejected(DateTime atTime, IPlanTarget planTarget, IPlanExposure planExposure) {
            // We evaluate avoidance halfway through a target's minimum plan time
            DateTime evaluationTime = GetMoonEvaluationTime(atTime, planTarget);
            double moonAltitude = GetRelaxationMoonAltitude(evaluationTime);

            if (!planExposure.MoonAvoidanceEnabled) {
                return false;
            }

            // Avoidance is completely off if the moon is below the relaxation min altitude and relaxation applies
            if (moonAltitude <= planExposure.MoonRelaxMinAltitude && planExposure.MoonRelaxScale > 0) {
                TSLogger.Info($"moon avoidance off: moon altitude ({moonAltitude}) is below relax min altitude ({planExposure.MoonRelaxMinAltitude})");
                return false;
            }

            // Avoidance is absolute regardless of moon phase or separation if Moon Must Be Down is enabled
            if (moonAltitude >= planExposure.MoonRelaxMinAltitude && planExposure.MoonDownEnabled)
            {
                TSLogger.Info($"moon avoidance absolute: moon altitude ({moonAltitude}) is above relax min altitude ({planExposure.MoonRelaxMinAltitude}) with Moon Must Be Down enabled");
                return true;
            }

            double moonSeparationParameter = planExposure.MoonAvoidanceSeparation;
            double moonWidthParameter = planExposure.MoonAvoidanceWidth;

            // If moon altitude is in the relaxation zone, then modulate the separation and width parameters
            if (moonAltitude <= planExposure.MoonRelaxMaxAltitude && planExposure.MoonRelaxScale > 0) {
                moonSeparationParameter = moonSeparationParameter + (planExposure.MoonRelaxScale * (moonAltitude - planExposure.MoonRelaxMaxAltitude));
                moonWidthParameter = moonWidthParameter * ((moonAltitude - planExposure.MoonRelaxMinAltitude) / (planExposure.MoonRelaxMaxAltitude - planExposure.MoonRelaxMinAltitude));
            }

            // If the separation was relaxed into oblivion, avoidance is off
            if (moonSeparationParameter <= 0) {
                TSLogger.Warning($"moon avoidance separation was relaxed below zero, avoidance off");
                return false;
            }

            // Determine avoidance
            double moonAge = GetMoonAge(evaluationTime);
            double moonSeparation = GetMoonSeparationAngle(observerInfo, evaluationTime, planTarget.Coordinates);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge,
                moonSeparationParameter, moonWidthParameter);

            bool rejected = moonSeparation < moonAvoidanceSeparation;
            TSLogger.Debug($"moon avoidance {planTarget.Name}/{planExposure.FilterName} rejected={rejected}, eval time={evaluationTime}, moon alt={moonAltitude}, moonSep={moonSeparation}, moonAvoidSep={moonAvoidanceSeparation}");

            return rejected;
        }

        public virtual DateTime GetMoonEvaluationTime(DateTime atTime, IPlanTarget planTarget) {
            return atTime.AddSeconds(planTarget.Project.MinimumTime * 60 / 2);
        }

        public virtual double GetRelaxationMoonAltitude(DateTime evaluationTime) {
            return AstroUtil.GetMoonAltitude(evaluationTime, observerInfo);
        }

        public virtual double GetMoonAge(DateTime atTime) {
            return AstrometryUtils.GetMoonAge(atTime);
        }

        public virtual double GetMoonSeparationAngle(ObserverInfo location, DateTime atTime, Coordinates coordinates) {
            return AstrometryUtils.GetMoonSeparationAngle(observerInfo, atTime, coordinates);
        }
    }
}