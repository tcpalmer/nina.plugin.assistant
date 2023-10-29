using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Sequencer;
using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace NINA.Plugin.Assistant.Test.Sequencer {

    [TestFixture]
    public class ImageGraderTest {

        private static readonly Guid DefaultProfileId = new Guid("01234567-0000-0000-0000-000000000000");
        private static readonly double[] Samples = new double[] { 483, 500, 545 };
        private static readonly double[] StarSamples = new double[] { 868, 890, 988, 948, 1013, 1094, 1120, 954, 1036, 1056, 1032, 1026, 875, 902, 941, 936, 963, 952, 972, 973, 892, 934, 1029, 1021, 841, 895, 909, 907, 893, 863, 938, 1024, 1083, 1054, 1047, 1076 };
        private static readonly double[] HFRSamples = new double[] { 2.07448275467066, 2.02349004066137, 2.108197156745, 2.12662384039748, 2.05165263000581, 2.07502324955962, 2.16952583092199, 2.74812292176884, 2.21471864267582, 2.16790525026247, 2.28202492295935, 2.19353441335193, 2.20482453213636, 2.26009082458899, 2.08825430049079, 2.32714283239602, 2.20412515283514, 2.05659297769561, 2.49652371605624, 2.49910139887986, 2.46098277861445, 2.43953469526813, 2.14781489293578, 2.06138954948957, 1.86677209593402, 2.03738728912668, 1.92672664010245, 1.90805895084673, 1.84542395463541, 1.78844121147567, 2.0959273790949, 2.0137358806371, 2.07657629250277, 2.20935904300475, 2.21286485602988, 2.20782001742784 };

        [Test]
        public void TestNoGradersEnabled() {
            ImageGrader sut = new ImageGrader(GetMockProfile(0, 0), GetPreferences(0, false, false, 0, false, 0, false, 0));
            (bool accepted, string rejectReason) = sut.GradeImage(null, null);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");
        }

        [Test]
        public void TestGradeRMS() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(0, false, true, 1, false, 0, false, 0);
            ImageGrader sut = new ImageGrader(profile, prefs);
            bool accepted;
            string rejectReason;

            ImageSavedEventArgs msg = GetMockMsg(0.6, 1, "L", 0, 0);
            (accepted, rejectReason) = sut.GradeImage(null, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            msg = GetMockMsg(1, 1, "L", 0, 0);
            (accepted, rejectReason) = sut.GradeImage(null, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_RMS);

            prefs = GetPreferences(0, false, true, 1.35, false, 0, false, 0);
            sut = new ImageGrader(profile, prefs);
            msg = GetMockMsg(0.6, 1, "L", 0, 0, 60, 10, 20, "2x2");
            (accepted, rejectReason) = sut.GradeImage(null, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            prefs = GetPreferences(0, false, true, 1.34, false, 0, false, 0);
            sut = new ImageGrader(profile, prefs);
            msg = GetMockMsg(0.6, 1, "L", 0, 0, 60, 10, 20, "2x2");
            (accepted, rejectReason) = sut.GradeImage(null, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_RMS);
        }

        [Test]
        public void TestGradeStars() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, false, false, 0, true, 2, false, 0);
            List<AcquiredImage> images = GetTestImages(10, 1, "L", 60);
            bool accepted;
            string rejectReason;

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0, 0, "L", 500, 0);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            msg = GetMockMsg(0, 0, "L", 400, 0);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_STARS);
        }

        [Test]
        public void TestGradeStarsAcceptImprovement() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, true, false, 0, true, 2, false, 0);
            List<AcquiredImage> images = GetTestImages(10, 1, "L", 60);
            bool accepted;
            string rejectReason;

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0, 0, "L", 1000, 0); // way outside variance but an improvement
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            msg = GetMockMsg(0, 0, "L", 400, 0);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_STARS);
        }

        [Test]
        public void TestGradeHFR() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, false, false, 0, false, 0, true, 2);
            List<AcquiredImage> images = GetTestImages(10, 1, "L", 60);
            bool accepted;
            string rejectReason;

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0, 0, "L", 0, 1.5);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            msg = GetMockMsg(0, 0, "L", 0, 3);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_HFR);
        }

        [Test]
        public void TestGradeHFRAcceptImprovement() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, true, false, 0, false, 0, true, 2);
            List<AcquiredImage> images = GetTestImages(10, 1, "L", 60);
            bool accepted;
            string rejectReason;

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0, 0, "L", 0, 0.1);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeTrue();
            rejectReason.Should().Be("");

            msg = GetMockMsg(0, 0, "L", 0, 3);
            (accepted, rejectReason) = sut.GradeImage(planTargetMock.Object, msg);
            accepted.Should().BeFalse();
            rejectReason.Should().Be(ImageGrader.REJECT_HFR);
        }

        [Test]
        public void TestGetSampleImageDataNotEnough() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, false, false, 0, true, 2, false, 0);
            List<AcquiredImage> images = GetTestImages(10, 1, "L", 60);

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0.6, 1, "L", 0, 0, 70); // duration mismatch

            List<AcquiredImage> samples = sut.GetSampleImageData(planTargetMock.Object, "L", msg);
            samples.Should().BeNull();
        }

        [Test]
        public void TestGetSampleImageDataSampleSize() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, false, false, 0, true, 2, false, 0); // sample size = 10
            List<AcquiredImage> images = GetTestImages(20, 1, "L");

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0.6, 1, "L", 0, 0);

            List<AcquiredImage> samples = sut.GetSampleImageData(planTargetMock.Object, "L", msg);
            samples.Should().NotBeNull();
            samples.Count.Should().Be(10);
        }

        [Test]
        public void TestGetSampleImageDataParamsChanged() {
            IProfile profile = GetMockProfile(3.8, 700);
            ImageGraderPreferences prefs = GetPreferences(10, false, false, 0, true, 2, false, 0);
            List<AcquiredImage> images = GetTestImages(5, 1, "L", 60, 100, 200, "1x1");
            images.AddRange(GetTestImages(5, 1, "L", 60, 10, 200, "1x1")); // gain changed

            Mock<ImageGrader> mock = new Mock<ImageGrader> { CallBase = true };
            mock.Setup(m => m.GetAcquiredImages(1, "L")).Returns(images);

            ImageGrader sut = mock.Object;
            sut.Profile = profile;
            sut.Preferences = prefs;

            Mock<IPlanTarget> planTargetMock = new Mock<IPlanTarget>();
            planTargetMock.SetupProperty(m => m.DatabaseId, 1);
            planTargetMock.SetupProperty(m => m.ROI, 100);

            ImageSavedEventArgs msg = GetMockMsg(0.6, 1, "L", 0, 0, 60, 100, 200, "1x1");

            List<AcquiredImage> samples = sut.GetSampleImageData(planTargetMock.Object, "L", msg);
            samples.Should().NotBeNull();
            samples.Count.Should().Be(5);
        }

        private IProfile GetMockProfile(double pixelSize, double focalLength) {
            Mock<IProfileService> mock = new Mock<IProfileService>();
            mock.SetupProperty(m => m.ActiveProfile.Id, DefaultProfileId);
            mock.SetupProperty(m => m.ActiveProfile.CameraSettings.PixelSize, pixelSize);
            mock.SetupProperty(m => m.ActiveProfile.TelescopeSettings.FocalLength, focalLength);
            return mock.Object.ActiveProfile;
        }

        private ImageGraderPreferences GetPreferences(int SampleSize, bool AcceptImprovement,
                                                      bool GradeRMS, double RMSPixelThreshold,
                                                      bool GradeDetectedStars, double DetectedStarsSigmaFactor,
                                                      bool GradeHFR, double HFRSigmaFactor) {
            return new ImageGraderPreferences(SampleSize, AcceptImprovement,
                                              GradeRMS, RMSPixelThreshold,
                                              GradeDetectedStars, DetectedStarsSigmaFactor,
                                              GradeHFR, HFRSigmaFactor);
        }

        private ImageSavedEventArgs GetMockMsg(double rmsTotal, double rmsScale, string filter, int detectedStars, double HFR, double duration = 60,
                                               int gain = 10, int offset = 20, string binning = "1x1", double rotation = 0) {
            ImageSavedEventArgs msg = new ImageSavedEventArgs();
            ImageMetaData metadata = new ImageMetaData();
            ImageParameter imageParameter = new ImageParameter();
            CameraParameter cameraParameter = new CameraParameter();

            RMS rms = new RMS { Total = rmsTotal };
            rms.SetScale(rmsScale);

            imageParameter.RecordedRMS = rms;
            imageParameter.Binning = binning;
            metadata.Rotator.Position = rotation;
            metadata.Image = imageParameter;

            cameraParameter.Gain = gain;
            cameraParameter.Offset = offset;
            metadata.Camera = cameraParameter;

            msg.Duration = duration;
            msg.MetaData = metadata;
            msg.Filter = filter;

            Mock<IStarDetectionAnalysis> sdMock = new Mock<IStarDetectionAnalysis>();
            sdMock.SetupProperty(m => m.DetectedStars, detectedStars);
            sdMock.SetupProperty(m => m.HFR, HFR);
            msg.StarDetectionAnalysis = sdMock.Object;

            return msg;
        }

        private List<AcquiredImage> GetTestImages(int count, int targetId, string filterName, double duration = 60, int gain = 10, int offset = 20, string binning = "1x1", double roi = 100, double rotatorPosition = 0) {

            DateTime dateTime = DateTime.Now.Date;
            List<AcquiredImage> images = new List<AcquiredImage>();

            for (int i = 0; i < count; i++) {
                dateTime = dateTime.AddMinutes(5);
                ImageMetadata metaData = new ImageMetadata {
                    ExposureDuration = duration,
                    DetectedStars = 500 + i,
                    HFR = 1 + (double)i / 10,
                    Gain = gain,
                    Offset = offset,
                    Binning = binning,
                    ROI = roi,
                    RotatorPosition = rotatorPosition
                };

                images.Add(new AcquiredImage(DefaultProfileId.ToString(), 0, targetId, dateTime, filterName, true, "", metaData));
            }

            return images.OrderByDescending(i => i.AcquiredDate).ToList();
        }

        [Test]
        [Ignore("test data generation")]
        public void TestGetData() {
            var db = GetDatabase();

            List<double> HaStars = new List<double>();
            List<double> HaHFR = new List<double>();

            using (SchedulerDatabaseContext context = db.GetContext()) {
                List<AcquiredImage> list = context.GetAcquiredImagesForGrading(1, "Ha");
                //list.ForEach(i => TestContext.WriteLine($"{i.AcquiredDate}"));

                HaStars = GetSamples(list, i => { return i.Metadata.DetectedStars; });
                HaHFR = GetSamples(list, i => { return i.Metadata.HFR; });

                //list.ForEach(i => HaStars.Add(i.Metadata.DetectedStars));
                //list.ForEach(i => HaHFR.Add(i.Metadata.HFR));
            }

            TestContext.WriteLine("Ha Stars:");
            HaStars.ForEach(v => TestContext.Write($"{v},"));

            TestContext.WriteLine("Ha HFR:");
            HaHFR.ForEach(v => TestContext.Write($"{v},"));

        }

        private List<double> GetSamples(List<AcquiredImage> images, Func<AcquiredImage, double> Sample) {
            List<double> samples = new List<double>();
            images.ForEach(i => samples.Add(Sample(i)));
            return samples;
        }

        private SchedulerDatabaseInteraction GetDatabase() {
            var testDbPath = Path.Combine("C:\\Users\\Tom\\AppData\\Local\\NINA\\SchedulerPlugin", @"schedulerdb.sqlite");
            TestContext.WriteLine($"DB PATH: {testDbPath}");
            return new SchedulerDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));
        }


    }
}
