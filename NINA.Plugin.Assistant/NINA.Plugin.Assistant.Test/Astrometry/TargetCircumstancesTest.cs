using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Plan;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class TargetCircumstancesTest {

        [Test]
        public void TargetCircumstances() {
            TimeInterval twilightSpan = new TimeInterval(new DateTime(2023, 1, 16, 18, 50, 0), new DateTime(2023, 1, 17, 5, 50, 0));
            var sut = new TargetCircumstances(TestUtil.M42, TestUtil.TEST_LOCATION_4, new HorizonDefinition(10), twilightSpan);
        }

    }
}
