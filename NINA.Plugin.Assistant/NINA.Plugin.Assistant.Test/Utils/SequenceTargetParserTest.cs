using Assistant.NINAPlugin.Util;
using FluentAssertions;
using NINA.Astrometry;
using NUnit.Framework;
using System.IO;

namespace NINA.Plugin.Assistant.Test.Util {

    [TestFixture]
    public class SequenceTargetParserTest {

        [Test]
        public void TestParse() {
            string tmpFileName = Path.GetTempFileName();
            File.WriteAllText(tmpFileName, J1);

            SequenceTarget st = SequenceTargetParser.GetSequenceTarget(tmpFileName);
            st.TargetName.Should().Be("M 42");
            st.Rotation.Should().BeApproximately(123.4, 0.0001);

            Coordinates c = st.GetCoordinates();
            c.RAString.Should().Be("05:35:17");
            c.DecString.Should().Be("-05° 23' 28\"");
        }

        private string J1 = @"
{
  ""$id"": ""1"",
  ""$type"": ""NINA.Sequencer.Container.DeepSkyObjectContainer, NINA.Sequencer"",
  ""Target"": {
    ""$id"": ""2"",
    ""$type"": ""NINA.Astrometry.InputTarget, NINA.Astrometry"",
    ""Expanded"": true,
    ""TargetName"": ""M 42"",
    ""PositionAngle"": 123.4,
    ""InputCoordinates"": {
      ""$id"": ""3"",
      ""$type"": ""NINA.Astrometry.InputCoordinates, NINA.Astrometry"",
      ""RAHours"": 5,
      ""RAMinutes"": 35,
      ""RASeconds"": 17.0,
      ""NegativeDec"": true,
      ""DecDegrees"": -5,
      ""DecMinutes"": 23,
      ""DecSeconds"": 28.0
    }
  },
";
    }
}