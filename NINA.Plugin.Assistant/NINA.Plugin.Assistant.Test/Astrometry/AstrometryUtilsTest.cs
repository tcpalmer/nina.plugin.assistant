using Assistant.NINAPlugin.Astrometry;
using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Utility.Converters;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    public class AstrometryUtilsTest {

        [Test]
        [TestCase("5:55:11", "7:24:30", 2022, 10, 27, 22, 25, 0, 0.0792, 80.9998)] // Betelgeuse rise
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 4, 46, 20, 62.4049, 181.0032)] // Betelgeuse transit
        [TestCase("5:55:11", "7:24:30", 2022, 10, 28, 11, 5, 36, -0.2597, 279.2407)] // Betelgeuse set
        public void TestGetHorizontalCoordinates(string ra, string dec, int yr, int mon, int day, int hh, int mm, int ss, double expectedAlt, double expectedAz) {
            DateTime atTime = new DateTime(yr, mon, day, hh, mm, ss);
            Coordinates coordinates = new Coordinates(AstroUtil.HMSToDegrees(ra), AstroUtil.DMSToDegrees(dec), Epoch.J2000, Coordinates.RAType.Degrees);

            HorizontalCoordinate hc = AstrometryUtils.GetHorizontalCoordinates(TestUtil.TEST_LOCATION_1, coordinates, atTime);
            hc.Altitude.Should().BeApproximately(expectedAlt, 0.001);
            hc.Azimuth.Should().BeApproximately(expectedAz, 0.001);
        }

        [Test]
        [TestCase(1, 2, 37, 0.503)] // 1st quarter
        [TestCase(8, 6, 2, 1.0)] // full (also a TLE)
        [TestCase(16, 8, 27, 0.508)] // 3rd quarter
        [TestCase(23, 17, 57, 0.0)] // new
        public void TestGetMoonIllumination(int day, int hour, int min, double expected) {
            DateTime dateTime = new DateTime(2022, 11, day, hour, min, 0);
            double moonIllumination = AstrometryUtils.GetMoonIllumination(dateTime);
            moonIllumination.Should().BeApproximately(expected, 0.001);
        }

        [Test]
        [TestCase(9, 0, 7, 37.1192)]
        [TestCase(12, 0, 4, 20.155)]
        [TestCase(16, 0, 7, 55.375)]
        public void TestGetMoonSeparationAngle(int day, int hour, int min, double expected) {
            DateTime dateTime = new DateTime(2022, 11, day, hour, min, 0);
            double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(TestUtil.TEST_LOCATION_1, dateTime, TestUtil.BETELGEUSE);
            moonSeparation.Should().BeApproximately(expected, 0.01);
        }

        [Test]
        [TestCase(1, 61.0143)]
        [TestCase(2, 65.5239)]
        [TestCase(3, 70.3303)]
        [TestCase(4, 75.4108)]
        [TestCase(5, 80.7246)]
        [TestCase(6, 86.2074)]
        [TestCase(7, 91.7675)]
        [TestCase(8, 97.2829)]
        [TestCase(9, 102.6005)]
        [TestCase(10, 107.5406)]
        [TestCase(11, 111.9054)]
        [TestCase(12, 115.4940)]
        [TestCase(13, 118.1219)]
        [TestCase(14, 119.6425)]
        [TestCase(15, 119.9663)]
        [TestCase(16, 119.0738)]
        [TestCase(17, 117.0185)]
        [TestCase(18, 113.9185)]
        [TestCase(19, 109.9411)]
        [TestCase(20, 105.2810)]
        [TestCase(21, 100.1398)]
        [TestCase(22, 94.7085)]
        [TestCase(23, 89.1549)]
        [TestCase(24, 83.6178)]
        [TestCase(25, 78.2047)]
        [TestCase(26, 72.9939)]
        [TestCase(27, 68.0382)]
        [TestCase(28, 63.3693)]
        [TestCase(29, 59.0026)]
        public void TestGetMoonAvoidanceLorentzianSeparation(int moonAge, double expected) {
            // These test cases duplicate http://bobdenny.com/ar/RefDocs/HelpFiles/ACPScheduler81Help/Constraints.htm
            double separation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge, 120, 14);
            separation.Should().BeApproximately(expected, 0.001);
        }

        [Test]
        [TestCase(11, 1, 2, 37, 7.91)]
        [TestCase(11, 8, 6, 2, 14.744)]
        [TestCase(11, 16, 8, 27, 22.606)]
        [TestCase(11, 23, 17, 57, 29.4829)]
        public void TestMoonAge(int month, int day, int hour, int min, double expected) {
            DateTime dateTime = new DateTime(2022, month, day, hour, min, 0);
            double moonAge = AstrometryUtils.GetMoonAge(dateTime);
            moonAge.Should().BeApproximately(expected, 0.001);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, -56, false)]
        [TestCase(0, 80, true)]
        public void TestRisesAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TestUtil.TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -40, true)]
        [TestCase(0, -46, false)]
        [TestCase(0, 70, true)]
        public void TestRisesAtLocationNorthHemisphereMinAlt(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_1, coordinates, 10).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -50, true)]
        [TestCase(0, 56, false)]
        [TestCase(0, -80, true)]
        public void TestRisesAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocation(TestUtil.TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -40, true)]
        [TestCase(0, 46, false)]
        [TestCase(0, -70, true)]
        public void TestRisesAtLocationSouthHemisphereMinAlt(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.RisesAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_2, coordinates, 10).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 56, true)]
        [TestCase(0, 54, false)]
        public void TestCircumpolarAtLocationNorthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TestUtil.TEST_LOCATION_1, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -56, true)]
        [TestCase(0, -54, false)]
        public void TestCircumpolarAtLocationSouthHemisphere(double ra, double dec, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocation(TestUtil.TEST_LOCATION_2, coordinates).Should().Be(expected);
        }

        [Test]
        [TestCase(0, 66, 10, true)]
        [TestCase(0, 56, 10, false)]
        public void TestCircumpolarAtLocationWithMinimumAltNorthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_1, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        [TestCase(0, -66, 10, true)]
        [TestCase(0, -56, 10, false)]
        public void TestCircumpolarAtLocationWithMinimumAltSouthHemisphere(double ra, double dec, double minAlt, bool expected) {
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            AstrometryUtils.CircumpolarAtLocationWithMinimumAltitude(TestUtil.TEST_LOCATION_2, coordinates, minAlt).Should().Be(expected);
        }

        [Test]
        public void TestIsAbovePolarCircle() {
            AstrometryUtils.IsAbovePolarCircle(TestUtil.TEST_LOCATION_1).Should().BeFalse();
            AstrometryUtils.IsAbovePolarCircle(TestUtil.TEST_LOCATION_3).Should().BeTrue();
        }

        [Test]
        public void TestBad() {

            DateTime dt = DateTime.Now;

            var ex = Assert.Throws<ArgumentException>(() => AstrometryUtils.GetHorizontalCoordinates(null, null, dt));
            ex.Message.Should().Be("location cannot be null");

            ObserverInfo oi = new ObserverInfo();
            ex = Assert.Throws<ArgumentException>(() => AstrometryUtils.GetHorizontalCoordinates(oi, null, dt));
            ex.Message.Should().Be("coordinates cannot be null");
        }

        [Test]
        [TestCase(16, 344)]
        [TestCase(344, 16)]
        [TestCase(0, 0)]
        [TestCase(736, 344)]
        [TestCase(-16, 16)]
        public void TestEM(double rotation, double expected) {
            AstrometryUtils.ConvertRotation(rotation).Should().BeApproximately(expected, 0.001);
        }

    }

}
