using NINA.Astrometry;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class TargetCircumstancesTest {

        //[Test]
        public void TargetCircumstances() {
            ObserverInfo location = new ObserverInfo { Latitude = 35.852934, Longitude = -79.163632 };

            double ra = AstroUtil.HMSToDegrees("5:35:18.57");
            double dec = AstroUtil.DMSToDegrees("-5:23:31.5");
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            TestContext.WriteLine($"Coords: {coordinates}");

            //var sut = new TargetCircumstances(coordinates, location, DateTime.Now, DateTime.Now);
            //TestContext.WriteLine($"TC:\n{sut}");
        }

    }
}
