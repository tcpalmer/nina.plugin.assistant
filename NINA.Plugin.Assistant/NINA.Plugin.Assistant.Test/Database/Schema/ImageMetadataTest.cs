using FluentAssertions;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Plugin.Assistant.Test.Plan;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Database.Schema {

    [TestFixture]
    public class ImageMetadataTest {

        [Test]
        public void TestImageMetadata() {
            DateTime saveDate = new DateTime(2023, 1, 17, 13, 2, 3);
            ImageSavedEventArgs msg = PlanMocks.GetImageSavedEventArgs(saveDate, "Ha");
            var sut = new ImageMetadata(msg, 100, 1);

            sut.FileName.Should().Be("\\\\path\\to\\image.fits");
            sut.FilterName.Should().Be("Ha");
            TestUtil.AssertTime(saveDate, sut.ExposureStartTime, saveDate.Hour, saveDate.Minute, saveDate.Second);
            sut.ExposureDuration.Should().Be(60);
            sut.Gain.Should().Be(306);
            sut.Offset.Should().Be(307);
            sut.Binning.Should().Be("1x1");
            sut.ReadoutMode.Should().Be(1);
            sut.ROI.Should().Be(100);
            sut.DetectedStars.Should().Be(100);
            sut.HFR.Should().Be(101);
            sut.HFRStDev.Should().Be(102);
            sut.FWHM.Should().Be(Double.NaN);
            sut.Eccentricity.Should().Be(Double.NaN);
            sut.ADUStDev.Should().Be(0);
            sut.ADUMean.Should().Be(0);
            sut.ADUMedian.Should().Be(0);
            sut.ADUMin.Should().Be(0);
            sut.ADUMax.Should().Be(0);
            sut.GuidingRMS.Should().Be(200);
            sut.GuidingRMSArcSec.Should().Be(200);
            sut.GuidingRMSRA.Should().Be(201);
            sut.GuidingRMSRAArcSec.Should().Be(201);
            sut.GuidingRMSDEC.Should().Be(202);
            sut.GuidingRMSDECArcSec.Should().Be(202);
            sut.FocuserPosition.Should().Be(300);
            sut.FocuserTemp.Should().Be(301);
            sut.RotatorPosition.Should().Be(302);
            sut.PierSide.Should().Be("West");
            sut.CameraTemp.Should().Be(304);
            sut.CameraTargetTemp.Should().Be(305);
            sut.Airmass.Should().Be(303);
        }

    }
}
