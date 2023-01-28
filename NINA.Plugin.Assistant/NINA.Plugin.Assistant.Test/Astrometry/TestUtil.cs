using Assistant.NINAPlugin.Astrometry;
using FluentAssertions;
using NINA.Astrometry;
using NINA.Core.Model;
using NUnit.Framework;
using System;
using System.IO;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    public class TestUtil {

        public static readonly ObserverInfo TEST_LOCATION_1, TEST_LOCATION_2, TEST_LOCATION_3, TEST_LOCATION_4, TEST_LOCATION_5, TEST_LOCATION_6;

        public static readonly Coordinates BETELGEUSE = new Coordinates(AstroUtil.HMSToDegrees("5:55:11"), AstroUtil.DMSToDegrees("7:24:30"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates SPICA = new Coordinates(AstroUtil.HMSToDegrees("13:26:25.92"), AstroUtil.DMSToDegrees("-11:17:2.6"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates STAR_NORTH_CIRCP = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"), AstroUtil.DMSToDegrees("80:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates STAR_SOUTH_CIRCP = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"), AstroUtil.DMSToDegrees("-80:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates M42 = new Coordinates(AstroUtil.HMSToDegrees("5:35:17"), AstroUtil.DMSToDegrees("-5:23:28"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates M31 = new Coordinates(AstroUtil.HMSToDegrees("0:42:44"), AstroUtil.DMSToDegrees("41:16:7"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates IC1805 = new Coordinates(AstroUtil.HMSToDegrees("2:32:42"), AstroUtil.DMSToDegrees("61:27:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates C00 = new Coordinates(AstroUtil.HMSToDegrees("0:0:0"), AstroUtil.DMSToDegrees("0:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        public static readonly Coordinates C90 = new Coordinates(AstroUtil.HMSToDegrees("9:0:0"), AstroUtil.DMSToDegrees("0:0:0"), Epoch.J2000, Coordinates.RAType.Degrees);

        static TestUtil() {

            // Northern hemisphere
            TEST_LOCATION_1 = new ObserverInfo();
            TEST_LOCATION_1.Latitude = 35;
            TEST_LOCATION_1.Longitude = -79;
            TEST_LOCATION_1.Elevation = 165;

            // Southern hemisphere
            TEST_LOCATION_2 = new ObserverInfo();
            TEST_LOCATION_2.Latitude = -35;
            TEST_LOCATION_2.Longitude = -80;
            TEST_LOCATION_2.Elevation = 165;

            // Northern hemisphere, above artic circle
            TEST_LOCATION_3 = new ObserverInfo();
            TEST_LOCATION_3.Latitude = 67;
            TEST_LOCATION_3.Longitude = -80;
            TEST_LOCATION_3.Elevation = 165;

            // Northern hemisphere, Pittsboro
            TEST_LOCATION_4 = new ObserverInfo();
            TEST_LOCATION_4.Latitude = 35.72027778;
            TEST_LOCATION_4.Longitude = -79.17638889;
            TEST_LOCATION_4.Elevation = 0;

            // Northern hemisphere, high latitude (Waskaganish, Que, ET)
            // 
            TEST_LOCATION_5 = new ObserverInfo();
            TEST_LOCATION_5.Latitude = 51.48;
            TEST_LOCATION_5.Longitude = -78.75;
            TEST_LOCATION_5.Elevation = 0;

            // Northern hemisphere, high latitude (Sanikiluaq, Nunavut, ET)
            // https://www.timeanddate.com/sun/canada/sanikiluaq
            TEST_LOCATION_6 = new ObserverInfo();
            TEST_LOCATION_6.Latitude = 56.54277778;
            TEST_LOCATION_6.Longitude = -79.225;
            TEST_LOCATION_6.Elevation = 0;
        }

        public static CustomHorizon GetTestHorizon(int num) {
            string horizonDefinition;

            switch (num) {
                case 1: // constant 20°
                    horizonDefinition = $"0 20" + Environment.NewLine
                        + "90 20" + Environment.NewLine
                        + "180 20" + Environment.NewLine
                        + "270 20";
                    break;
                case 2: // up and down
                    horizonDefinition = $"0 20" + Environment.NewLine
                        + "90 30" + Environment.NewLine
                        + "180 40" + Environment.NewLine
                        + "270 30";
                    break;
                case 3: // mine
                    horizonDefinition = $"0 22" + Environment.NewLine
                        + "10 50" + Environment.NewLine
                        + "20 48" + Environment.NewLine
                        + "30 49" + Environment.NewLine
                        + "40 37" + Environment.NewLine
                        + "50 47" + Environment.NewLine
                        + "60 45" + Environment.NewLine
                        + "70 42" + Environment.NewLine
                        + "80 32" + Environment.NewLine
                        + "90 31" + Environment.NewLine
                        + "100 28" + Environment.NewLine
                        + "110 18" + Environment.NewLine
                        + "120 23" + Environment.NewLine
                        + "130 18" + Environment.NewLine
                        + "140 17" + Environment.NewLine
                        + "150 25" + Environment.NewLine
                        + "160 20" + Environment.NewLine
                        + "170 11" + Environment.NewLine
                        + "180.0001 18" + Environment.NewLine
                        + "190 50" + Environment.NewLine
                        + "200 49" + Environment.NewLine
                        + "210 31" + Environment.NewLine
                        + "220 33" + Environment.NewLine
                        + "230 32" + Environment.NewLine
                        + "240 56" + Environment.NewLine
                        + "250 61" + Environment.NewLine
                        + "260 63" + Environment.NewLine
                        + "270 61" + Environment.NewLine
                        + "280 52" + Environment.NewLine
                        + "290 54" + Environment.NewLine
                        + "300 25" + Environment.NewLine
                        + "310 15" + Environment.NewLine
                        + "320 21" + Environment.NewLine
                        + "330 26" + Environment.NewLine
                        + "340 23" + Environment.NewLine
                        + "350 24";
                    break;
                default:
                    throw new NotImplementedException($"custom horizon not implemented: {num}");
            }

            using (var sr = new StringReader(horizonDefinition)) {
                return CustomHorizon.FromReader_Standard(sr);
            }
        }

        public static HorizonDefinition getHD(double minimumAltitude) {
            return new HorizonDefinition(minimumAltitude);
        }

        public static void AssertTime(DateTime expected, DateTime? actual, int hours, int minutes, int seconds) {
            actual.Should().NotBeNull();

            DateTime edt = expected.Date.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);
            DateTime adt = new DateTime(((DateTime)actual).Year, ((DateTime)actual).Month, ((DateTime)actual).Day,
                ((DateTime)actual).Hour, ((DateTime)actual).Minute, ((DateTime)actual).Second);

            bool cond = (edt == adt);
            if (!cond) {
                TestContext.WriteLine($"assertTime failed:");
                TestContext.WriteLine($"  expected: {edt}");
                TestContext.WriteLine($"  actual:   {adt}");
            }

            cond.Should().BeTrue();
        }

    }

}
