using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Astrometry.Solver;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Astrometry.Solver {

    public class CircumstanceSolverTest {

        [Test]
        [TestCase(-10, -5, 60, double.MinValue)] // No rising
        [TestCase(1, 10, 60, double.MinValue)] // No rising
        [TestCase(-45, 45, 21600, 0.00307)]
        public void TestFindRising(double altStart, double altEnd, int secs, double expectedAlt) {
            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            double az = 180;

            alts.Add(new AltitudeAtTime(altStart, az, start));
            alts.Add(new AltitudeAtTime(altEnd, az, start.AddSeconds(secs)));
            AltitudeAtTime aat = new CircumstanceSolver(new TestAltitudeRefiner()).FindRising(new Altitudes(alts));

            if (expectedAlt == double.MinValue) {
                aat.Should().BeNull();
            } else {
                aat.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            }
        }

        [Test]
        public void TestFindRiseAboveMinimum() {
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());
            HorizonDefinition hd = TestUtil.getHD(0);

            var ex = Assert.Throws<ArgumentException>(() => cs.FindRiseAboveMinimum(null, hd));
            ex.Message.Should().Be("altitudes cannot be null");

            DateTime start = DateTime.Now.Date;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(0, 180, start));
            alts.Add(new AltitudeAtTime(10, 180, start.AddSeconds(60)));

            alts.Clear();
            alts.Add(new AltitudeAtTime(10, 180, start));
            alts.Add(new AltitudeAtTime(50, 180, start.AddSeconds(60)));

            // min outside span
            cs.FindRiseAboveMinimum(new Altitudes(alts), TestUtil.getHD(5)).Should().BeNull();
            cs.FindRiseAboveMinimum(new Altitudes(alts), TestUtil.getHD(55)).Should().BeNull();

            alts.Clear();
            alts.Add(new AltitudeAtTime(1, 180, start));
            alts.Add(new AltitudeAtTime(10, 180, start.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(20, 180, start.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(30, 180, start.AddMinutes(3)));

            AltitudeAtTime aat = cs.FindRiseAboveMinimum(new Altitudes(alts), TestUtil.getHD(15));
            aat.Altitude.Should().BeApproximately(15.0413, 0.001);

            aat.AtTime.Hour.Should().Be(0);
            aat.AtTime.Minute.Should().Be(1);
            aat.AtTime.Second.Should().Be(30);
        }

        [Test]
        public void TestFindTransit1() {
            // Note that you can't test transit with the test refiner since it can't refine a wrapped interval properly

            DSORefiner refiner = new DSORefiner(TestUtil.TEST_LOCATION_1, TestUtil.BETELGEUSE);
            DateTime day = new DateTime(2022, 10, 12).Date;

            Altitudes generated = refiner.GetHourlyAltitudesForDay(day);
            AltitudeAtTime aat = new CircumstanceSolver(refiner, 1).FindTransit(generated);
            aat.Altitude.Should().BeApproximately(62.4083, 0.001);

            DateTime at = aat.AtTime;
            DateTime actual = new DateTime(at.Year, at.Month, at.Day, at.Hour, at.Minute, 0);
            DateTime expected = new DateTime(2022, 10, 12, 5, 47, 0);
            actual.Should().Be(expected);

            var ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(new TestAltitudeRefiner(), 1).FindTransit(null));
            ex.Message.Should().Be("altitudes cannot be null");
        }

        [Test]
        public void TestFindTransit2() {
            // Note that you can't test transit with the test refiner since it can't refine a wrapped interval properly

            DSORefiner refiner = new DSORefiner(TestUtil.TEST_LOCATION_4, TestUtil.IC1805);
            DateTime day = new DateTime(2023, 5, 1).Date;

            Altitudes generated = refiner.GetHourlyAltitudesForDay(day);
            AltitudeAtTime aat = new CircumstanceSolver(refiner, 1).FindTransit(generated);
            aat.Altitude.Should().BeApproximately(64.27, 0.001);

            DateTime at = aat.AtTime;
            DateTime actual = new DateTime(at.Year, at.Month, at.Day, at.Hour, at.Minute, 0);
            DateTime expected = new DateTime(2023, 5, 1, 13, 11, 0);
            actual.Should().Be(expected);

            var ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(new TestAltitudeRefiner(), 1).FindTransit(null));
            ex.Message.Should().Be("altitudes cannot be null");
        }

        [Test]
        public void TestFindSetBelowMinimum() {
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());

            var ex = Assert.Throws<ArgumentException>(() => cs.FindSetBelowMinimum(null, TestUtil.getHD(0)));
            ex.Message.Should().Be("altitudes cannot be null");

            DateTime start = DateTime.Now.Date;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(50, 180, start));
            alts.Add(new AltitudeAtTime(10, 180, start.AddSeconds(60)));

            // min outside span
            cs.FindSetBelowMinimum(new Altitudes(alts), TestUtil.getHD(55)).Should().BeNull();
            cs.FindSetBelowMinimum(new Altitudes(alts), TestUtil.getHD(5)).Should().BeNull();

            alts.Clear();
            alts.Add(new AltitudeAtTime(60, 180, start));
            alts.Add(new AltitudeAtTime(50, 180, start.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(40, 180, start.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(30, 180, start.AddMinutes(3)));

            AltitudeAtTime aat = cs.FindSetBelowMinimum(new Altitudes(alts), TestUtil.getHD(55));

            aat.Altitude.Should().BeApproximately(55.0413, 0.001);
            aat.AtTime.Hour.Should().Be(0);
            aat.AtTime.Minute.Should().Be(0);
            aat.AtTime.Second.Should().Be(29);
        }

        [Test]
        public void TestFindSetting() {
            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner());

            // No setting present
            alts.Add(new AltitudeAtTime(10, 180, start));
            alts.Add(new AltitudeAtTime(5, 180, start.AddSeconds(60)));
            cs.FindSetting(new Altitudes(alts)).Should().BeNull();

            // No setting present
            alts.Clear();
            alts.Add(new AltitudeAtTime(-1, 180, start));
            alts.Add(new AltitudeAtTime(-10, 180, start.AddSeconds(60)));
            cs.FindSetting(new Altitudes(alts)).Should().BeNull();

            // Good test
            alts.Clear();
            alts.Add(new AltitudeAtTime(45, 180, start));
            alts.Add(new AltitudeAtTime(-45, 180, start.AddHours(6)));
            AltitudeAtTime aat = cs.FindSetting(new Altitudes(alts));
            aat.Altitude.Should().BeApproximately(0.00307, 0.001);

            var ex = Assert.Throws<ArgumentException>(() => cs.FindSetting(null));
            ex.Message.Should().Be("altitudes cannot be null");
        }

        [Test]
        public void TestGetStepInterval() {
            CircumstanceSolver cs = new CircumstanceSolver(new TestAltitudeRefiner(), 1);

            var ex = Assert.Throws<ArgumentException>(() => cs.GetStepInterval(null));
            ex.Message.Should().Be("step cannot be null");

            DateTime start = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(-1, 180, start));
            alts.Add(new AltitudeAtTime(1, 180, start.AddSeconds(10)));
            alts.Add(new AltitudeAtTime(2, 180, start.AddSeconds(20)));

            ex = Assert.Throws<ArgumentException>(() => cs.GetStepInterval(new Altitudes(alts)));
            ex.Message.Should().Be("altitudes must have exactly two points");

            alts.Clear();
            alts.Add(new AltitudeAtTime(-1, 180, start));
            alts.Add(new AltitudeAtTime(1, 180, start.AddSeconds(60)));

            cs.GetStepInterval(new Altitudes(alts)).Should().Be(60);
        }

        [Test]
        public void TestBad() {
            var ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(null, 0));
            ex.Message.Should().Be("refiner cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(null, 0));
            ex.Message.Should().Be("refiner cannot be null");

            ex = Assert.Throws<ArgumentException>(() => new CircumstanceSolver(new TestAltitudeRefiner(), 0));
            ex.Message.Should().Be("max final time step must be >= 1");
        }
    }
}