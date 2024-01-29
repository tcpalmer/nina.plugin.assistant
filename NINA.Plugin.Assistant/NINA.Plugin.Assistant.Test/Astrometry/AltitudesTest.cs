using Assistant.NINAPlugin.Astrometry.Solver;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    public class AltitudesTest {

        [Test]
        public void TestGetInterval() {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            DateTime dt = DateTime.Now;
            alts.Add(new AltitudeAtTime(1, 180, dt));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddSeconds(1)));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddSeconds(2)));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddSeconds(3)));
            Altitudes sut = new Altitudes(alts);
            sut.GetIntervalSeconds().Should().Be(3);
        }

        [Test]
        public void TestFindMax() {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            DateTime dt = DateTime.Now;
            alts.Add(new AltitudeAtTime(1, 180, dt));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(3)));
            Altitudes altitudes = new Altitudes(alts);

            Tuple<int, AltitudeAtTime> max = altitudes.FindMaximumAltitude();
            max.Item2.Altitude.Should().BeApproximately(3, 0.001);
            max.Item2.AtTime.Should().BeSameDateAs(dt.AddMinutes(2));
            max.Item1.Should().Be(2);

            alts.Clear();
            alts.Add(new AltitudeAtTime(1, 180, dt));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(4, 180, dt.AddMinutes(3)));
            altitudes = new Altitudes(alts);

            max = altitudes.FindMaximumAltitude();
            max.Item2.Altitude.Should().BeApproximately(4, 0.001);
            max.Item2.AtTime.Should().BeSameDateAs(dt.AddMinutes(3));
            max.Item1.Should().Be(3);

            alts.Clear();
            alts.Add(new AltitudeAtTime(4, 180, dt));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(1, 180, dt.AddMinutes(3)));
            altitudes = new Altitudes(alts);

            max = altitudes.FindMaximumAltitude();
            max.Item2.Altitude.Should().BeApproximately(4, 0.001);
            max.Item2.AtTime.Should().BeSameDateAs(dt.AddMinutes(1));
            max.Item1.Should().Be(0);
        }

        [Test]
        public void TestFindMin() {
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();
            DateTime dt = DateTime.Now;
            alts.Add(new AltitudeAtTime(4, 180, dt));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(3)));
            Altitudes altitudes = new Altitudes(alts);

            Tuple<int, AltitudeAtTime> min = altitudes.FindMinimumAltitude();
            min.Item2.Altitude.Should().BeApproximately(2, 0.001);
            min.Item2.AtTime.Should().BeSameDateAs(dt.AddMinutes(2));
            min.Item1.Should().Be(2);

            alts.Clear();
            alts.Add(new AltitudeAtTime(1, 180, dt));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(4, 180, dt.AddMinutes(3)));
            altitudes = new Altitudes(alts);

            min = altitudes.FindMinimumAltitude();
            min.Item2.Altitude.Should().BeApproximately(1, 0.001);
            min.Item2.AtTime.Should().BeSameDateAs(dt);
            min.Item1.Should().Be(0);

            alts.Clear();
            alts.Add(new AltitudeAtTime(4, 180, dt));
            alts.Add(new AltitudeAtTime(3, 180, dt.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(2, 180, dt.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(1, 180, dt.AddMinutes(3)));
            altitudes = new Altitudes(alts);

            min = altitudes.FindMinimumAltitude();
            min.Item2.Altitude.Should().BeApproximately(1, 0.001);
            min.Item2.AtTime.Should().BeSameDateAs(dt.AddMinutes(3));
            min.Item1.Should().Be(3);
        }

        [Test]
        public void TestClipAscendingStart() {
            DateTime now = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(50, 0, now));
            alts.Add(new AltitudeAtTime(60, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(70, 0, now.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(60, 0, now.AddMinutes(3)));
            Altitudes altitudes = new Altitudes(alts);

            Altitudes sut = altitudes.ClipAscendingStart();
            sut.AltitudeList.Count.Should().Be(2);
            sut.AltitudeList[0].Altitude.Should().BeApproximately(70, 0.0001);
            sut.AltitudeList[1].Altitude.Should().BeApproximately(60, 0.0001);

            alts.Clear();
            alts.Add(new AltitudeAtTime(70, 0, now));
            alts.Add(new AltitudeAtTime(60, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(50, 0, now.AddMinutes(2)));
            altitudes = new Altitudes(alts);

            sut = altitudes.ClipAscendingStart();
            sut.AltitudeList.Count.Should().Be(3);
            sut.AltitudeList[0].Altitude.Should().BeApproximately(70, 0.0001);
            sut.AltitudeList[1].Altitude.Should().BeApproximately(60, 0.0001);

            alts.Clear();
            alts.Add(new AltitudeAtTime(50, 0, now));
            alts.Add(new AltitudeAtTime(60, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(70, 0, now.AddMinutes(2)));
            altitudes = new Altitudes(alts);

            var ex = Assert.Throws<ArgumentException>(() => altitudes.ClipAscendingStart());
            Assert.AreEqual("altitude list is unexpectedly always ascending", ex.Message);
        }

        [Test]
        public void TestFindSpan() {
            DateTime now = DateTime.Now;
            List<AltitudeAtTime> alts = new List<AltitudeAtTime>();

            alts.Add(new AltitudeAtTime(2, 0, now));
            alts.Add(new AltitudeAtTime(1, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(0, 0, now.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(-1, 0, now.AddMinutes(3)));
            alts.Add(new AltitudeAtTime(-2, 0, now.AddMinutes(4)));
            Altitudes altitudes = new Altitudes(alts);

            Altitudes sut = altitudes.FindSpan(-0.5, true);
            sut.AltitudeList.Count.Should().Be(2);
            sut.AltitudeList[0].Altitude.Should().BeApproximately(0, 0.0001);
            sut.AltitudeList[1].Altitude.Should().BeApproximately(-1, 0.0001);

            alts.Clear();
            alts.Add(new AltitudeAtTime(-2, 0, now));
            alts.Add(new AltitudeAtTime(-1, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(0, 0, now.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(1, 0, now.AddMinutes(3)));
            alts.Add(new AltitudeAtTime(2, 0, now.AddMinutes(4)));
            altitudes = new Altitudes(alts);

            sut = altitudes.FindSpan(0.5, false);
            sut.AltitudeList.Count.Should().Be(2);
            sut.AltitudeList[0].Altitude.Should().BeApproximately(0, 0.0001);
            sut.AltitudeList[1].Altitude.Should().BeApproximately(1, 0.0001);

            alts.Clear();
            alts.Add(new AltitudeAtTime(-2, 0, now));
            alts.Add(new AltitudeAtTime(-3, 0, now.AddMinutes(1)));
            alts.Add(new AltitudeAtTime(-4, 0, now.AddMinutes(2)));
            alts.Add(new AltitudeAtTime(-3, 0, now.AddMinutes(3)));
            alts.Add(new AltitudeAtTime(-2, 0, now.AddMinutes(4)));
            altitudes = new Altitudes(alts);

            sut = altitudes.FindSpan(-5, true);
            sut.Should().BeNull();
            sut = altitudes.FindSpan(-5, false);
            sut.Should().BeNull();
        }

        [Test]
        public void TestBad() {
            var ex = Assert.Throws<ArgumentException>(() => new Altitudes(null));
            Assert.AreEqual("altitudes cannot be null", ex.Message);

            List<AltitudeAtTime> altitudes = new List<AltitudeAtTime>();
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("altitudes must have at least two values", ex.Message);

            altitudes.Add(new AltitudeAtTime(0, 180, DateTime.Now));
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("altitudes must have at least two values", ex.Message);

            altitudes.Add(new AltitudeAtTime(1, 180, DateTime.Now.AddSeconds(-100)));
            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("startTime must be before endTime", ex.Message);

            altitudes.Clear();

            DateTime dt = DateTime.Now;
            altitudes.Add(new AltitudeAtTime(0.01, 180, dt));
            altitudes.Add(new AltitudeAtTime(1.12, 180, dt.AddMinutes(10)));
            altitudes.Add(new AltitudeAtTime(2.23, 180, dt.AddMinutes(5)));
            altitudes.Add(new AltitudeAtTime(3.45, 180, dt.AddMinutes(20)));

            //TestContext.Out.WriteLine(new Altitudes(altitudes).ToString());

            ex = Assert.Throws<ArgumentException>(() => new Altitudes(altitudes));
            Assert.AreEqual("time is not always increasing", ex.Message);
        }
    }

    public class AltitudeAtTimeTest {

        [Test]
        public void TestOk() {
            DateTime dt = DateTime.Now;
            AltitudeAtTime sut = new AltitudeAtTime(45, 180, dt);

            sut.Altitude.Should().Be(45);
            sut.AtTime.Should().Be(dt);
        }

        [Test]
        public void TestBad() {
            var ex = Assert.Throws<ArgumentException>(() => new AltitudeAtTime(180, 180, DateTime.Now));
            Assert.AreEqual("altitude must be <= 90 and >= -90", ex.Message);

            ex = Assert.Throws<ArgumentException>(() => new AltitudeAtTime(45, 361, DateTime.Now));
            Assert.AreEqual("azimuth must be >= 0 and <= 360", ex.Message);
        }
    }
}