using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Database.Schema {

    [TestFixture]
    public class TargetTest {

        [Test]
        public void testCoordinatesGetSet() {
            Target sut = new Target();

            sut.RAHours = 5;
            sut.RAMinutes = 10;
            sut.RASeconds = 20;
            sut.DecDegrees = -10;
            sut.DecMinutes = 10;
            sut.DecSeconds = 20;

            sut.Coordinates.ToString().Should().Be("RA: 05:10:20; Dec: -10° 10' 20\"; Epoch: J2000");
            sut.ra.Should().BeApproximately(5.1722, 0.001);
            sut.RA.Should().BeApproximately(5.1722, 0.001);
            sut.dec.Should().BeApproximately(-10.1722, 0.001);
            sut.Dec.Should().BeApproximately(-10.1722, 0.001);
        }

    }
}
