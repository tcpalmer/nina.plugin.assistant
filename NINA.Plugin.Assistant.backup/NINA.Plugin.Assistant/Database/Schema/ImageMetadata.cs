﻿using Assistant.NINAPlugin.Util;
using NINA.Core.Enum;
using NINA.Image.ImageData;
using NINA.WPF.Base.Interfaces.Mediator;
using System;
using System.Text;
using System.Web;

namespace Assistant.NINAPlugin.Database.Schema {

    public class ImageMetadata {

        public string FileName { get; set; }
        public string FilterName { get; set; }
        public DateTime ExposureStartTime { get; set; }
        public double ExposureDuration { get; set; }

        public int Gain { get; set; }
        public int Offset { get; set; }
        public string Binning { get; set; }

        public int DetectedStars { get; set; }
        public double HFR { get; set; }
        public double HFRStDev { get; set; }

        public double ADUStDev { get; set; }
        public double ADUMean { get; set; }
        public double ADUMedian { get; set; }
        public int ADUMin { get; set; }
        public int ADUMax { get; set; }

        public double GuidingRMS { get; set; }
        public double GuidingRMSArcSec { get; set; }
        public double GuidingRMSRA { get; set; }
        public double GuidingRMSRAArcSec { get; set; }
        public double GuidingRMSDEC { get; set; }
        public double GuidingRMSDECArcSec { get; set; }

        public int? FocuserPosition { get; set; }
        public double FocuserTemp { get; set; }
        public double RotatorPosition { get; set; }
        public string PierSide { get; set; }

        public double CameraTemp { get; set; }
        public double CameraTargetTemp { get; set; }
        public double Airmass { get; set; }

        public ImageMetadata() { }

        public ImageMetadata(ImageSavedEventArgs msg) {
            Assert.notNull(msg, "msg cannot be null");

            FileName = msg.PathToImage.LocalPath;
            FilterName = msg.Filter;
            ExposureStartTime = msg.MetaData.Image.ExposureStart;
            ExposureDuration = msg.Duration;
            Binning = msg.MetaData.Image.Binning?.ToString();

            Gain = msg.MetaData.Camera.Gain;
            Offset = msg.MetaData.Camera.Offset;

            ADUStDev = msg.Statistics.StDev;
            ADUMean = msg.Statistics.Mean;
            ADUMedian = msg.Statistics.Median;
            ADUMin = msg.Statistics.Min;
            ADUMax = msg.Statistics.Max;

            DetectedStars = msg.StarDetectionAnalysis.DetectedStars;
            HFR = msg.StarDetectionAnalysis.HFR;
            HFRStDev = msg.StarDetectionAnalysis.HFRStDev;

            GuidingRMS = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Total);
            GuidingRMSArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Total);
            GuidingRMSRA = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.RA);
            GuidingRMSRAArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.RA);
            GuidingRMSDEC = GetGuidingMetric(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Dec);
            GuidingRMSDECArcSec = GetGuidingMetricArcSec(msg.MetaData.Image, msg.MetaData.Image?.RecordedRMS?.Dec);

            FocuserPosition = msg.MetaData.Focuser.Position;
            FocuserTemp = msg.MetaData.Focuser.Temperature;
            RotatorPosition = msg.MetaData.Rotator.Position;
            PierSide = GetPierSide(msg.MetaData.Telescope.SideOfPier);

            CameraTemp = msg.MetaData.Camera.Temperature;
            CameraTargetTemp = msg.MetaData.Camera.SetPoint;
            Airmass = msg.MetaData.Telescope.Airmass;
        }

        private string GetImageFilePath(Uri imageUri) {
            return HttpUtility.UrlDecode(imageUri.AbsolutePath);
        }

        private double GetGuidingMetric(ImageParameter image, double? metric) {
            return (double)((image.RecordedRMS != null && metric != null) ? metric : 0);
        }

        private double GetGuidingMetricArcSec(ImageParameter image, double? metric) {
            return (double)((image.RecordedRMS != null && metric != null) ? (metric * image.RecordedRMS.Scale) : 0);
        }

        private string GetPierSide(PierSide sideOfPier) {
            switch (sideOfPier) {
                case NINA.Core.Enum.PierSide.pierEast: return "East";
                case NINA.Core.Enum.PierSide.pierWest: return "West";
                default: return "n/a";
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FileName: {FileName}");
            sb.AppendLine($"FilterName: {FilterName}");
            sb.AppendLine($"ExposureStartTime: {ExposureStartTime}");
            sb.AppendLine($"ExposureDuration: {ExposureDuration}");
            sb.AppendLine($"Gain: {Gain}");
            sb.AppendLine($"Offset: {Offset}");
            sb.AppendLine($"Binning: {Binning}");
            sb.AppendLine($"DetectedStars: {DetectedStars}");
            sb.AppendLine($"HFR: {HFR}");
            sb.AppendLine($"HFRStDev: {HFRStDev}");
            sb.AppendLine($"ADUStDev: {ADUStDev}");
            sb.AppendLine($"ADUMean: {ADUMean}");
            sb.AppendLine($"ADUMedian: {ADUMedian}");
            sb.AppendLine($"ADUMin: {ADUMin}");
            sb.AppendLine($"ADUMax: {ADUMax}");
            sb.AppendLine($"GuidingRMS: {GuidingRMS}");
            sb.AppendLine($"GuidingRMSArcSec: {GuidingRMSArcSec}");
            sb.AppendLine($"GuidingRMSRA: {GuidingRMSRA}");
            sb.AppendLine($"GuidingRMSRAArcSec: {GuidingRMSRAArcSec}");
            sb.AppendLine($"GuidingRMSDEC: {GuidingRMSDEC}");
            sb.AppendLine($"GuidingRMSDECArcSec: {GuidingRMSDECArcSec}");
            sb.AppendLine($"FocuserPosition: {FocuserPosition}");
            sb.AppendLine($"FocuserTemp: {FocuserTemp}");
            sb.AppendLine($"RotatorPosition: {RotatorPosition}");
            sb.AppendLine($"PierSide: {PierSide}");
            sb.AppendLine($"CameraTemp: {CameraTemp}");
            sb.AppendLine($"CameraTargetTemp: {CameraTargetTemp}");
            sb.AppendLine($"Airmass: {Airmass}");
            return sb.ToString();
        }
    }
}
