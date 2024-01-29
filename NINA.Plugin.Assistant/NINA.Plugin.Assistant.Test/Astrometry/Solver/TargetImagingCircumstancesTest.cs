using Assistant.NINAPlugin.Astrometry;
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
        public void Test3() {
            DateTime start = new DateTime(2023, 12, 18, 18, 0, 0);
            DateTime end = new DateTime(2023, 12, 19, 6, 0, 0);

            // With my horizon and M42, this tests the old 'SW tree' problem
            HorizonDefinition hd = new HorizonDefinition(TestUtil.GetTestHorizon(3), 0, 0);
            TargetImagingCircumstances tic = new TargetImagingCircumstances(TestUtil.TEST_LOCATION_1, TestUtil.M42, start, end, hd);
            tic.Analyze();

            TestUtil.AssertTime(start, tic.RiseAboveMinimumTime, 19, 49, 12);
            TestUtil.AssertTime(end, tic.TransitTime, 0, 1, 7);
            TestUtil.AssertTime(end, tic.SetBelowMinimumTime, 0, 26, 33);
        }

        [Test]
        public void TestClipped() {
            DateTime start = new DateTime(2023, 1, 17, 18, 58, 0);
            DateTime end = new DateTime(2023, 1, 18, 5, 55, 0);

            TargetImagingCircumstances tic = new TargetImagingCircumstances(TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, start, end, TestUtil.getHD(0));
            tic.Analyze();

            tic.RiseAboveMinimumTime.Should().Be(DateTime.MinValue);
            TestUtil.AssertTime(start, tic.RiseAboveMinimumTimeClipped, start.Hour, start.Minute, start.Second);
            TestUtil.AssertTime(end, tic.SetBelowMinimumTime, 4, 41, 53);

            start = new DateTime(2022, 11, 22, 18, 35, 0);
            end = new DateTime(2022, 11, 23, 5, 30, 0);

            tic = new TargetImagingCircumstances(TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE, start, end, TestUtil.getHD(0));
            tic.Analyze();

            TestUtil.AssertTime(start, tic.RiseAboveMinimumTime, 19, 42, 22);
            tic.SetBelowMinimumTime.Should().Be(DateTime.MinValue);
            TestUtil.AssertTime(end, tic.SetBelowMinimumTimeClipped, end.Hour, end.Minute, end.Second);
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