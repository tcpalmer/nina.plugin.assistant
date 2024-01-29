using Assistant.NINAPlugin.Astrometry;
using NINA.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class DSORiseAndSetTest {

        //[Test]
        public void DSORiseAndSetBasic() {
            ObserverInfo location = new ObserverInfo { Latitude = 35.852934, Longitude = -79.163632 };

            double ra = AstroUtil.HMSToDegrees("5:35:18.57");
            double dec = AstroUtil.DMSToDegrees("-5:23:31.5");
            Coordinates coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Degrees);
            TestContext.WriteLine($"Coords: {coordinates}");

            var rs = new DSORiseAndSet(DateTime.Now, location, coordinates);

            TestContext.WriteLine($"Start: {DateTime.Now}");
            rs.Calculate().Wait();
            TestContext.WriteLine($"End:   {DateTime.Now}");
            TestContext.WriteLine($"RISE: {rs.Rise}");
            TestContext.WriteLine($"SET:  {rs.Set}");
        }
    }
}