using Assistant.NINAPlugin.Util;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Util {

    public class UtilsTest {

        [Test]
        [TestCase(0, "0h 0m")]
        [TestCase(32, "0h 32m")]
        [TestCase(61, "1h 1m")]
        [TestCase(719, "11h 59m")]
        public void TestMtoHM(int min, string expected) {
            Utils.MtoHM(min).Should().Be(expected);
        }

        [Test]
        [TestCase(null, 0)]
        [TestCase("", 0)]
        [TestCase("0h 0m", 0)]
        [TestCase("0h 32m", 32)]
        [TestCase("1h 1m", 61)]
        [TestCase("11h 59m", 719)]
        public void TestHMtoM(string hm, int expected) {
            Utils.HMtoM(hm).Should().Be(expected);
        }

        [Test]
        public void TestMidpoint() {
            DateTime start = DateTime.Now;
            DateTime mid = Utils.GetMidpointTime(start, start.AddHours(1));
            mid.Should().Be(start.AddMinutes(30));
        }
    }

}
