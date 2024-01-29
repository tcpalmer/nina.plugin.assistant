using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Namotion.Reflection;
using NINA.Image.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageGrader {
        public static readonly string REJECT_RMS = "Guiding RMS";
        public static readonly string REJECT_STARS = "Star Count";
        public static readonly string REJECT_HFR = "HFR";
        public static readonly string REJECT_FWHM = "FWHM";
        public static readonly string REJECT_ECCENTRICITY = "Eccentricity";

        public IProfile Profile { get; set; }
        public ImageGraderPreferences Preferences { get; set; }
        private bool enableGradeRMS;

        public ImageGrader() {
        }

        public ImageGrader(IProfile profile) {
            this.Profile = profile;
            this.Preferences = GetPreferences(profile);
            enableGradeRMS = EnableGradeRMS(Preferences.EnableGradeRMS);
        }

        public ImageGrader(IProfile profile, ImageGraderPreferences preferences) {
            this.Profile = profile;
            this.Preferences = preferences;
            enableGradeRMS = EnableGradeRMS(Preferences.EnableGradeRMS);
        }

        public (bool, string) GradeImage(IPlanTarget planTarget, ImageSavedEventArgs msg) {
            if (!enableGradeRMS && NoGradingMetricsEnabled()) {
                TSLogger.Info("image grading: no metrics enabled => accepted");
                return (true, "");
            }

            try {
                if (enableGradeRMS && !GradeRMS(msg)) {
                    TSLogger.Info("image grading: failed guiding RMS => NOT accepted");
                    return (false, REJECT_RMS);
                }

                if (NoGradingMetricsEnabled()) {
                    TSLogger.Info("image grading: no additional metrics enabled => accepted");
                    return (true, "");
                }

                // Get comparable images to compare against; if we don't have at least 3 to compare against, assume this one is acceptable
                List<AcquiredImage> images = GetSampleImageData(planTarget, msg.Filter, msg);
                if (images == null) {
                    TSLogger.Info("image grading: not enough matching images => accepted");
                    return (true, "");
                }

                if (Preferences.EnableGradeStars) {
                    List<double> samples = GetSamples(images, i => { return i.Metadata.DetectedStars; });
                    TSLogger.Info("image grading: detected star count ->");
                    int detectedStars = msg.StarDetectionAnalysis.DetectedStars;
                    if (detectedStars == 0 || !WithinAcceptableVariance(samples, detectedStars, Preferences.DetectedStarsSigmaFactor, true)) {
                        TSLogger.Info("image grading: failed detected star count grading => NOT accepted");
                        return (false, REJECT_STARS);
                    }
                }

                if (Preferences.EnableGradeHFR) {
                    List<double> samples = GetSamples(images, i => { return i.Metadata.HFR; });
                    TSLogger.Info("image grading: HFR ->");
                    double hfr = msg.StarDetectionAnalysis.HFR;
                    if (NearZero(hfr) || !WithinAcceptableVariance(samples, hfr, Preferences.HFRSigmaFactor, false)) {
                        TSLogger.Info("image grading: failed HFR grading => NOT accepted");
                        return (false, REJECT_HFR);
                    }
                }

                if (Preferences.EnableGradeFWHM) {
                    double fwhm = GetHocusFocusMetric(msg.StarDetectionAnalysis, "FWHM");
                    if (Double.IsNaN(fwhm)) {
                        TSLogger.Warning("image grading: FWHM grading is enabled but image doesn't have FWHM metric.  Is Hocus Focus installed, enabled, and configured for star detection?");
                    } else {
                        List<double> samples = GetSamples(images, i => { return i.Metadata.FWHM; });
                        if (SamplesHaveData(samples)) {
                            TSLogger.Info("image grading: FWHM ->");
                            if (NearZero(fwhm) || !WithinAcceptableVariance(samples, fwhm, Preferences.FWHMSigmaFactor, false)) {
                                TSLogger.Info("image grading: failed FWHM grading => NOT accepted");
                                return (false, REJECT_FWHM);
                            }
                        } else {
                            TSLogger.Warning("All comparison samples for FWHM don't have valid data, skipping FWHM grading");
                        }
                    }
                }

                if (Preferences.EnableGradeEccentricity) {
                    double eccentricity = GetHocusFocusMetric(msg.StarDetectionAnalysis, "Eccentricity");
                    if (eccentricity == Double.NaN) {
                        TSLogger.Warning("image grading: eccentricity grading is enabled but image doesn't have eccentricity metric.  Is Hocus Focus installed, enabled, and configured for star detection?");
                    } else {
                        List<double> samples = GetSamples(images, i => { return i.Metadata.Eccentricity; });
                        if (SamplesHaveData(samples)) {
                            TSLogger.Info("image grading: eccentricity ->");
                            if (NearZero(eccentricity) || !WithinAcceptableVariance(samples, eccentricity, Preferences.EccentricitySigmaFactor, false)) {
                                TSLogger.Info("image grading: failed eccentricity grading => NOT accepted");
                                return (false, REJECT_ECCENTRICITY);
                            }
                        } else {
                            TSLogger.Warning("All comparison samples for eccentricity don't have valid data, skipping eccentricity grading");
                        }
                    }
                }

                TSLogger.Info("image grading: all tests passed => accepted");
                return (true, "");
            } catch (Exception e) {
                TSLogger.Error("image grading: exception => NOT accepted");
                TSLogger.Error(e);
                return (false, "exception");
            }
        }

        private bool NoGradingMetricsEnabled() {
            if (!Preferences.EnableGradeStars && !Preferences.EnableGradeHFR &&
                !Preferences.EnableGradeFWHM && !Preferences.EnableGradeEccentricity) {
                return true;
            }

            return false;
        }

        public bool SamplesHaveData(List<double> samples) {
            foreach (double sample in samples) {
                if (sample <= 0 || Double.IsNaN(sample)) {
                    return false;
                }
            }

            return true;
        }

        public bool NearZero(double value) {
            return Math.Abs(value) <= 0.001;
        }

        private bool EnableGradeRMS(bool enableGradeRMS) {
            // Disable RMS grading if running as a sync client since no guiding data will be available
            if (enableGradeRMS && SyncManager.Instance.IsRunning && !SyncManager.Instance.IsServer) {
                return false;
            }

            return enableGradeRMS;
        }

        private bool GradeRMS(ImageSavedEventArgs msg) {
            if (msg.MetaData?.Image?.RecordedRMS == null) {
                TSLogger.Info("image grading: guiding RMS not available");
                return true;
            }

            double guidingRMSArcSecs = msg.MetaData.Image.RecordedRMS.Total * msg.MetaData.Image.RecordedRMS.Scale;
            if (guidingRMSArcSecs <= 0) {
                TSLogger.Info("image grading: guiding RMS not valid for grading");
                return true;
            }

            try {
                double pixelSize = Profile.CameraSettings.PixelSize;
                double focalLenth = Profile.TelescopeSettings.FocalLength;
                double binning = GetBinning(msg);

                double cameraArcSecsPerPixel = (pixelSize / focalLenth) * 206.265 * binning;
                double cameraRMSPerPixel = guidingRMSArcSecs * cameraArcSecsPerPixel;

                TSLogger.Info($"image grading: RMS pixelSize={pixelSize} focalLength={focalLenth} bin={binning} cameraArcSecsPerPixel={cameraArcSecsPerPixel} cameraRMSPerPixel={cameraRMSPerPixel}");
                return (cameraRMSPerPixel > Preferences.RMSPixelThreshold) ? false : true;
            } catch (Exception e) {
                TSLogger.Warning($"image grading: failed to determine RMS error in main camera pixels: {e.Message}\n{e.StackTrace}");
                return true;
            }
        }

        private bool WithinAcceptableVariance(List<double> samples, double newSample, double sigmaFactor, bool positiveImprovement) {
            TSLogger.Info($"    samples={SamplesToString(samples)}");
            (double mean, double stddev) = Stats.SampleStandardDeviation(samples);

            if (Preferences.AcceptImprovement) {
                if (positiveImprovement && newSample > mean) {
                    TSLogger.Info($"    mean={mean} sample={newSample} (acceptable: improved)");
                    return true;
                }
                if (!positiveImprovement && newSample < mean) {
                    TSLogger.Info($"    mean={mean} sample={newSample} (acceptable: improved)");
                    return true;
                }
            }

            double variance = Math.Abs(mean - newSample);
            TSLogger.Info($"    mean={mean} stddev={stddev} sample={newSample} variance={variance} sigmaX={sigmaFactor}");
            return variance <= (stddev * sigmaFactor);
        }

        private List<double> GetSamples(List<AcquiredImage> images, Func<AcquiredImage, double> Sample) {
            List<double> samples = new List<double>();
            images.ForEach(i => samples.Add(Sample(i)));
            return samples;
        }

        public virtual double GetHocusFocusMetric(IStarDetectionAnalysis starDetectionAnalysis, string propertyName) {
            return starDetectionAnalysis.HasProperty(propertyName) ?
                (Double)starDetectionAnalysis.GetType().GetProperty(propertyName).GetValue(starDetectionAnalysis) :
                Double.NaN;
        }

        public virtual List<AcquiredImage> GetAcquiredImages(int targetId, string filterName) {
            SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
            using (SchedulerDatabaseContext context = database.GetContext()) {
                return context.GetAcquiredImagesForGrading(targetId, filterName);
            }
        }

        public List<AcquiredImage> GetSampleImageData(IPlanTarget planTarget, string filterName, ImageSavedEventArgs msg) {
            TSLogger.Info($"image grading: comparing against like images, filter={filterName}, exp={msg.Duration}, gain={msg.MetaData.Camera.Gain}, offset={msg.MetaData.Camera.Offset}, bin={msg.MetaData.Image.Binning}, roi={planTarget.ROI}");
            List<AcquiredImage> rawList = GetAcquiredImages(planTarget.DatabaseId, filterName);

            /* Filter for matching:
             *   duration
             *   gain/offset/binning
             *   rotator position (NOT YET)
             *   ROI
             *   acquisition profile ID
             */

            string profileId = Profile.Id.ToString();
            List<AcquiredImage> list = rawList.Where(i =>
                i.ProfileId == profileId &&
                i.Metadata.ExposureDuration == msg.Duration &&
                i.Metadata.Gain == msg.MetaData.Camera.Gain &&
                i.Metadata.Offset == msg.MetaData.Camera.Offset &&
                i.Metadata.Binning == msg.MetaData.Image.Binning &&
                i.Metadata.ROI == planTarget.ROI
            //i.Metadata.RotatorPosition == planTarget.Rotation -> disabled for now
            ).ToList();

            if (list.Count < 3) {
                return null;
            }

            return list.Take(Preferences.MaxGradingSampleSize).ToList();
        }

        private ImageGraderPreferences GetPreferences(IProfile profile) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                ProfilePreference profilePreference = context.GetProfilePreference(profile.Id.ToString());
                if (profilePreference == null) {
                    profilePreference = new ProfilePreference(profile.Id.ToString());
                }

                return new ImageGraderPreferences(profilePreference);
            }
        }

        private double GetBinning(ImageSavedEventArgs msg) {
            try {
                string bin = msg.MetaData?.Image?.Binning;
                if (string.IsNullOrEmpty(bin)) {
                    return 1;
                }

                return double.Parse(bin.Substring(0, 1));
            } catch (Exception) {
                return 1;
            }
        }

        private string SamplesToString(List<double> samples) {
            StringBuilder sb = new StringBuilder();
            samples.ForEach(s => sb.Append($"{s}, "));
            return sb.ToString();
        }
    }

    public class ImageGraderPreferences {
        public int MaxGradingSampleSize { get; private set; }
        public bool AcceptImprovement { get; private set; }
        public bool EnableGradeRMS { get; private set; }
        public double RMSPixelThreshold { get; private set; }
        public bool EnableGradeStars { get; private set; }
        public double DetectedStarsSigmaFactor { get; private set; }
        public bool EnableGradeHFR { get; private set; }
        public double HFRSigmaFactor { get; private set; }

        public bool EnableGradeFWHM { get; private set; }
        public double FWHMSigmaFactor { get; private set; }
        public bool EnableGradeEccentricity { get; private set; }
        public double EccentricitySigmaFactor { get; private set; }

        public ImageGraderPreferences(ProfilePreference profilePreference) {
            MaxGradingSampleSize = profilePreference.MaxGradingSampleSize;
            AcceptImprovement = profilePreference.AcceptImprovement;
            EnableGradeRMS = profilePreference.EnableGradeRMS;
            RMSPixelThreshold = profilePreference.RMSPixelThreshold;
            EnableGradeStars = profilePreference.EnableGradeStars;
            DetectedStarsSigmaFactor = profilePreference.DetectedStarsSigmaFactor;
            EnableGradeHFR = profilePreference.EnableGradeHFR;
            HFRSigmaFactor = profilePreference.HFRSigmaFactor;
            EnableGradeFWHM = profilePreference.EnableGradeFWHM;
            FWHMSigmaFactor = profilePreference.FWHMSigmaFactor;
            EnableGradeEccentricity = profilePreference.EnableGradeEccentricity;
            EccentricitySigmaFactor = profilePreference.EccentricitySigmaFactor;
        }

        public ImageGraderPreferences(
                                    int MaxGradingSampleSize, bool AcceptImprovement,
                                    bool EnableGradeRMS, double RMSPixelThreshold,
                                    bool EnableGradeStars, double DetectedStarsSigmaFactor,
                                    bool EnableGradeHFR, double HFRSigmaFactor,
                                    bool EnableGradeFWHM, double FWHMSigmaFactor,
                                    bool EnableGradeEccentricity, double EccentricitySigmaFactor) {
            this.MaxGradingSampleSize = MaxGradingSampleSize;
            this.AcceptImprovement = AcceptImprovement;
            this.EnableGradeRMS = EnableGradeRMS;
            this.RMSPixelThreshold = RMSPixelThreshold;
            this.EnableGradeStars = EnableGradeStars;
            this.DetectedStarsSigmaFactor = DetectedStarsSigmaFactor;
            this.EnableGradeHFR = EnableGradeHFR;
            this.HFRSigmaFactor = HFRSigmaFactor;
            this.EnableGradeFWHM = EnableGradeFWHM;
            this.FWHMSigmaFactor = FWHMSigmaFactor;
            this.EnableGradeEccentricity = EnableGradeEccentricity;
            this.EccentricitySigmaFactor = EccentricitySigmaFactor;
        }
    }
}