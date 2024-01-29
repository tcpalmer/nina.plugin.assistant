using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Astrometry.Solver;
using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry.Solver {

    public class ImagingDayTest {

        [Test]
        public void TestIsEverAboveMinimumAltitude() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(10));

            Altitudes samplePositions = imagingDay.SamplePositions;
            samplePositions.Should().NotBeNull();
            samplePositions.AltitudeList.Count.Should().Be(74);

            // Betelgeuse is up in mid Oct - but not above 63
            imagingDay.IsEverAboveMinimumAltitude().Should().BeTrue();
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(63));
            imagingDay.IsEverAboveMinimumAltitude().Should().BeFalse();

            // Spica is not up at all in Oct
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA, TestUtil.getHD(10));
            imagingDay.IsEverAboveMinimumAltitude().Should().BeFalse();

            start = new DateTime(2022, 6, 25, 21, 0, 0);
            end = new DateTime(2022, 6, 26, 5, 0, 0);

            // Betelgeuse is not up in June
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(20));
            imagingDay.IsEverAboveMinimumAltitude().Should().BeFalse();

            // Spica is up in June - but not above 44
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA, TestUtil.getHD(20));
            imagingDay.IsEverAboveMinimumAltitude().Should().BeTrue();
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA, TestUtil.getHD(44));
            imagingDay.IsEverAboveMinimumAltitude().Should().BeFalse();
        }

        [Test]
        public void TestIsEverAboveCustomHorizon() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = new HorizonDefinition(TestUtil.GetTestHorizon(2), 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, hd);
            imagingDay.IsEverAboveMinimumAltitude().Should().BeTrue();

            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.SPICA, hd);
            imagingDay.IsEverAboveMinimumAltitude().Should().BeFalse();
        }

        [Test]
        public void TestTransitTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(0));
            DateTime transitTime = imagingDay.GetTransitTime();
            TestUtil.AssertTime(end, transitTime, 5, 31, 39);
        }

        [Test]
        public void TestTransitTimeNorth() {
            DateTime start = new DateTime(2023, 7, 25, 18, 0, 0);
            DateTime end = new DateTime(2023, 7, 26, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_4, TestUtil.B150, TestUtil.getHD(0));
            DateTime transitTime = imagingDay.GetTransitTime();
            TestUtil.AssertTime(end, transitTime, 1, 53, 6);
        }

        [Test]
        public void TestGetRiseAboveMinimumTime() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(20));
            DateTime riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime();
            TestUtil.AssertTime(end, riseAboveMinimumTime, 0, 49, 29);

            // Betelgeuse doesn't rise above 20 on 8/7 until 5:28am
            start = new DateTime(2022, 8, 7, 18, 0, 0);
            end = new DateTime(2022, 8, 8, 5, 20, 0);

            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(20));
            riseAboveMinimumTime = imagingDay.GetRiseAboveMinimumTime();
            riseAboveMinimumTime.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestGetSetBelowMinimumTimeDescending() {
            // This tests the case where the target has an anti-meridian in the time span which leads to
            // altitude dropping below minimum but then later rising above.

            DateTime start = new DateTime(2022, 3, 15, 20, 18, 51);
            DateTime end = new DateTime(2022, 3, 16, 6, 31, 10);
            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.M31, TestUtil.getHD(5));
            DateTime setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime();
            TestUtil.AssertTime(start, setBelowMinimumTime, 22, 16, 59);

            start = new DateTime(2022, 3, 15, 20, 18, 37);
            end = new DateTime(2022, 3, 16, 6, 31, 30);
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.IC1805, TestUtil.getHD(10));
            setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime();
            TestUtil.AssertTime(end, setBelowMinimumTime, 2, 3, 26);
        }

        [Test]
        public void TestGetRiseAboveTimeCustomHorizon() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = new HorizonDefinition(TestUtil.GetTestHorizon(1), 0);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, hd);

            // Constant 20° custom horizon should equal regular calculation
            DateTime timeWithHorizon = imagingDay.GetRiseAboveMinimumTime();
            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, TestUtil.getHD(20));
            DateTime timeWithMinAlt = imagingDay.GetRiseAboveMinimumTime();

            timeWithHorizon.Should().BeSameDateAs(timeWithMinAlt);
        }

        [Test]
        public void TestGetSetBelowMinimumTime() {
            DateTime start = new DateTime(2022, 3, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 3, 16, 6, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, hd);
            DateTime setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime();
            TestUtil.AssertTime(end, setBelowMinimumTime, 0, 19, 8);

            // Betelgeuse doesn't set below minimum during this interval
            start = new DateTime(2022, 10, 15, 18, 0, 0);
            end = new DateTime(2022, 10, 16, 6, 0, 0);

            imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, hd);
            setBelowMinimumTime = imagingDay.GetSetBelowMinimumTime();
            setBelowMinimumTime.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestCircumpolarNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.STAR_NORTH_CIRCP, hd);

            imagingDay.GetRiseAboveMinimumTime().Should().Be(DateTime.MinValue);

            TestUtil.AssertTime(start, imagingDay.GetTransitTime(), 23, 37, 26);
            imagingDay.GetSetBelowMinimumTime().Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestCircumpolarSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_2, TestUtil.STAR_SOUTH_CIRCP, hd);

            imagingDay.GetRiseAboveMinimumTime().Should().Be(DateTime.MinValue);
            TestUtil.AssertTime(start, imagingDay.GetTransitTime(), 23, 41, 26);
            imagingDay.GetSetBelowMinimumTime().Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestNeverRisesNorth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.STAR_SOUTH_CIRCP, hd);

            imagingDay.GetRiseAboveMinimumTime().Should().Be(DateTime.MinValue);
            imagingDay.GetTransitTime().Should().Be(DateTime.MinValue);
            imagingDay.GetSetBelowMinimumTime().Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestNeverRisesSouth() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_2, TestUtil.STAR_NORTH_CIRCP, hd);

            imagingDay.GetRiseAboveMinimumTime().Should().Be(DateTime.MinValue);
            imagingDay.GetTransitTime().Should().Be(DateTime.MinValue);
            imagingDay.GetSetBelowMinimumTime().Should().Be(DateTime.MinValue);
        }

        [Test]
        public void TestSetBelowMinIssue() {
            DateTime start = new DateTime(2022, 12, 23, 19, 0, 0);
            DateTime end = new DateTime(2022, 12, 24, 5, 0, 0);
            HorizonDefinition hd = TestUtil.getHD(20);

            ImagingDay imagingDay = new ImagingDay(start, end, TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, hd);

            TestUtil.AssertTime(start, imagingDay.GetRiseAboveMinimumTime(), 19, 18, 12);
            TestUtil.AssertTime(end, imagingDay.GetTransitTime(), 0, 0, 21);
            TestUtil.AssertTime(end, imagingDay.GetSetBelowMinimumTime(), 4, 42, 29);
        }

        [Test]
        public void TestBad() {
            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, null, null, null));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, oi, null, null));
            ex.Message.Should().Be("target cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new ImagingDay(dt, dt, oi, TestUtil.BETELGEUSE, null));
            ex.Message.Should().Be("horizonDefinition cannot be null");
        }
    }
}