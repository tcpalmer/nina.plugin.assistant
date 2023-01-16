using Assistant.NINAPlugin.Astrometry.Solver;
using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry.Solver {

    public class TargetImagingCircumstancesTest {

        [Test]
        public void Test1() {
            DateTime start = new DateTime(2022, 10, 15, 18, 0, 0);
            DateTime end = new DateTime(2022, 10, 16, 6, 0, 0);

            TargetImagingCircumstances tic = new TargetImagingCircumstances(TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, start, end, TestUtil.getHD(20));
            tic.Analyze();

            TestUtil.AssertTime(end, tic.RiseAboveMinimumTime, 0, 49, 29);
            TestUtil.AssertTime(end, tic.TransitTime, 5, 31, 39);
            tic.SetBelowMinimumTime.Should().Be(DateTime.MinValue);
        }

        [Test]
        public void Test2() {
            DateTime start = new DateTime(2022, 12, 23, 19, 0, 0);
            DateTime end = new DateTime(2022, 12, 24, 5, 0, 0);

            TargetImagingCircumstances tic = new TargetImagingCircumstances(TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, start, end, TestUtil.getHD(20));
            tic.Analyze();

            TestUtil.AssertTime(start, tic.RiseAboveMinimumTime, 19, 18, 12);
            TestUtil.AssertTime(end, tic.TransitTime, 0, 0, 21);
            TestUtil.AssertTime(end, tic.SetBelowMinimumTime, 4, 42, 29);
        }

        [Test]
        public void TestBad() {
            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(null, null, dt, dt, TestUtil.getHD(0)));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(oi, null, dt, dt, TestUtil.getHD(0)));
            ex.Message.Should().Be("target cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new TargetImagingCircumstances(oi, TestUtil.BETELGEUSE, dt, dt.AddMinutes(-1), TestUtil.getHD(0)));
            ex.Message.Should().Be("startTime must be before endTime");
        }
    }

}
