using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Database.Schema {

    [TestFixture]
    public class TargetTest {

        [Test]
        [TestCase(5, 10, 20, -10, 10, 20, 5.1722, -10.1722, "RA: 05:10:20; Dec: -10° 10' 20\"; Epoch: J2000")]
        [TestCase(5, 10, 20.9, -10, 10, 20.9, 5.1722, -10.1722, "RA: 05:10:21; Dec: -10° 10' 21\"; Epoch: J2000")]
        public void testCoordinatesGetSet(int raH, int raM, double raS,
                                          int decD, int decM, double decS,
                                          double expectedRA, double expectedDec, string expectedFmt) {
            Target sut = new Target();
            sut.Enabled.Should().Be(true);

            sut.RAHours = raH;
            sut.RAMinutes = raM;
            sut.RASeconds = raS;
            sut.DecDegrees = decD;
            sut.DecMinutes = decM;
            sut.DecSeconds = decS;

            sut.Coordinates.ToString().Should().Be(expectedFmt);
            sut.ra.Should().BeApproximately(expectedRA, 0.001);
            sut.RA.Should().BeApproximately(expectedRA, 0.001);
            sut.RASeconds.Should().Be(raS);
            sut.dec.Should().BeApproximately(expectedDec, 0.001);
            sut.Dec.Should().BeApproximately(expectedDec, 0.001);
            sut.DecSeconds.Should().Be(decS);
        }
    }
}