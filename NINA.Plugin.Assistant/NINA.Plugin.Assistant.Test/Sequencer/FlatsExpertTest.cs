using Assistant.NINAPlugin.Sequencer;
using FluentAssertions;
using NINA.Core.Model.Equipment;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Sequencer {

    [TestFixture]
    public class FlatsExpertTest {

        [Test]
        public void TestFlatsSpec() {
            FlatSpec sut = new FlatSpec("Ha", 10, 20, new BinningMode(2, 2), 0, 123.4, 89);
            sut.FilterName.Should().Be("Ha");
            sut.Gain.Should().Be(10);
            sut.Offset.Should().Be(20);
            sut.BinningMode.X.Should().Be(2);
            sut.ReadoutMode.Should().Be(0);
            sut.Rotation.Should().Be(123.4);
            sut.ROI.Should().Be(89);

            sut.Key.Should().Be("Ha_10_20_2x2_0_123.4_89");
        }
    }
}
