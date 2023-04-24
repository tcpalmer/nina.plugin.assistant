using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assistant.NINAPlugin.Sequencer {

    public class ImageGrader {

        public IProfile Profile { get; set; }
        public ImageGraderPreferences Preferences { get; set; }

        public ImageGrader() { }

        public ImageGrader(IProfile profile) {
            this.Profile = profile;
            this.Preferences = GetPreferences();
        }

        public ImageGrader(IProfile profile, ImageGraderPreferences preferences) {
            this.Profile = profile;
            this.Preferences = preferences;
        }

        public bool GradeImage(IPlanTarget planTarget, ImageSavedEventArgs msg) {

            if (!Preferences.GradeRMS && !Preferences.GradeDetectedStars && !Preferences.GradeHFR) {
                TSLogger.Info("image grading: no metrics enabled => accepted");
                return true;
            }

            try {
                if (Preferences.GradeRMS && !GradeRMS(msg)) {
                    TSLogger.Info("image grading: failed guiding RMS => NOT accepted");
                    return false;
                }

                if (!Preferences.GradeDetectedStars && !Preferences.GradeHFR) {
                    TSLogger.Info("image grading: no additional metrics enabled => accepted");
                    return true;
                }

                // Get comparable images to compare against; if we don't have at least 3 to compare against, assume this one is acceptable
                List<AcquiredImage> images = GetSampleImageData(planTarget.DatabaseId, msg.Filter, msg);
                if (images == null) {
                    TSLogger.Info("image grading: not enough matching images => accepted");
                    return true;
                }

                if (Preferences.GradeDetectedStars) {
                    List<double> samples = GetSamples(images, i => { return i.Metadata.DetectedStars; });
                    TSLogger.Info("image grading: detected star count ->");
                    if (!WithinAcceptableVariance(samples, msg.StarDetectionAnalysis.DetectedStars, Preferences.DetectedStarsSigmaFactor)) {
                        TSLogger.Info("image grading: failed detected star count grading => NOT accepted");
                        return false;
                    }
                }

                if (Preferences.GradeHFR) {
                    List<double> samples = GetSamples(images, i => { return i.Metadata.HFR; });
                    TSLogger.Info("image grading: HFR ->");
                    if (!WithinAcceptableVariance(samples, msg.StarDetectionAnalysis.HFR, Preferences.HFRSigmaFactor)) {
                        TSLogger.Info("image grading: failed HFR grading => NOT accepted");
                        return false;
                    }
                }

                TSLogger.Info("image grading: all tests passed => accepted");
                return true;

            }
            catch (Exception e) {
                TSLogger.Error("image grading: exception => NOT accepted");
                TSLogger.Error(e);
                return false;
            }
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
            }
            catch (Exception e) {
                TSLogger.Warning($"image grading: failed to determine RMS error in main camera pixels: {e.Message}\n{e.StackTrace}");
                return true;
            }
        }

        private bool WithinAcceptableVariance(List<double> samples, double newSample, double sigmaFactor) {
            TSLogger.Info($"    samples={SamplesToString(samples)}");
            (double mean, double stddev) = Stats.SampleStandardDeviation(samples);
            double variance = Math.Abs(mean - newSample);
            TSLogger.Info($"    mean={mean} stddev={stddev} sample={newSample} variance={variance} sigmaX={sigmaFactor}");
            return variance <= (stddev * sigmaFactor);
        }

        private List<double> GetSamples(List<AcquiredImage> images, Func<AcquiredImage, double> Sample) {
            List<double> samples = new List<double>();
            images.ForEach(i => samples.Add(Sample(i)));
            return samples;
        }

        public virtual List<AcquiredImage> GetAcquiredImages(int targetId, string filterName) {
            SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
            using (SchedulerDatabaseContext context = database.GetContext()) {
                return context.GetAcquiredImagesForGrading(targetId, filterName);
            }
        }

        public List<AcquiredImage> GetSampleImageData(int targetId, string filterName, ImageSavedEventArgs msg) {
            List<AcquiredImage> rawList = GetAcquiredImages(targetId, filterName);

            // Filter for matching duration/gain/offset/binning
            List<AcquiredImage> list = rawList.Where(i =>
                i.Metadata.ExposureDuration == msg.Duration &&
                i.Metadata.Gain == msg.MetaData.Camera.Gain &&
                i.Metadata.Offset == msg.MetaData.Camera.Offset &&
                i.Metadata.Binning == msg.MetaData.Image.Binning
            ).ToList();

            if (list.Count < 3) {
                return null;
            }

            return list.Take(Preferences.MaxSampleSize).ToList();
        }

        public ImageGraderPreferences GetPreferences() {
            // TODO: read from database
            string profileId = Profile.Id.ToString();
            return new ImageGraderPreferences {
                MaxSampleSize = 10,
                GradeRMS = true,
                RMSPixelThreshold = 8,
                GradeDetectedStars = true,
                DetectedStarsSigmaFactor = 3,
                GradeHFR = true,
                HFRSigmaFactor = 3
            };
        }

        private double GetBinning(ImageSavedEventArgs msg) {
            try {
                string bin = msg.MetaData?.Image?.Binning;
                if (string.IsNullOrEmpty(bin)) {
                    return 1;
                }

                return double.Parse(bin.Substring(0, 1));
            }
            catch (Exception) {
                return 1;
            }
        }

        private string SamplesToString(List<double> samples) {
            StringBuilder sb = new StringBuilder();
            samples.ForEach(s => sb.Append($"{s}, "));
            return sb.ToString();
        }
    }

    // TODO: move to Database/Schema
    public class ImageGraderPreferences {

        public int MaxSampleSize { get; set; }

        public bool GradeRMS { get; set; }
        public double RMSPixelThreshold { get; set; }

        public bool GradeDetectedStars { get; set; }
        public double DetectedStarsSigmaFactor { get; set; }

        public bool GradeHFR { get; set; }
        public double HFRSigmaFactor { get; set; }
    }
}
