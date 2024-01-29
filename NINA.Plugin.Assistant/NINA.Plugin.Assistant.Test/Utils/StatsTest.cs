using Assistant.NINAPlugin.Util;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;

namespace NINA.Plugin.Assistant.Test.Util {

    [TestFixture]
    public class StatsTest {

        [Test]
        public void TestSampleStandardDeviation() {
            Action act = () => Stats.SampleStandardDeviation(null);
            act.Should().Throw<Exception>().Where(e => e.Message == "must have >= 3 samples");

            double[] samples = new double[] { 483, 500 };
            act = () => Stats.SampleStandardDeviation(samples.ToList());
            act.Should().Throw<Exception>().Where(e => e.Message == "must have >= 3 samples");

            samples = new double[] { 483, 500, 545 };
            (double mean, double stddev) = Stats.SampleStandardDeviation(samples.ToList());
            mean.Should().BeApproximately(509.3333, 0.001);
            stddev.Should().BeApproximately(32.0364, 0.001);
        }
    }
}