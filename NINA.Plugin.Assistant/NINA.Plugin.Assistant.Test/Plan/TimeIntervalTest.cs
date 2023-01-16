using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class TimeIntervalTest {

        [Test]
        public void TestOk() {
            TimeInterval sut = new TimeInterval(DateTime.Now, DateTime.Now.AddSeconds(1));
            sut.Duration.Should().Be(1);
        }

        [Test]
        public void TestTotalTimeSpan() {
            DateTime now = DateTime.Now;
            TimeInterval ti1 = new TimeInterval(now, now.AddHours(1));
            TimeInterval ti2 = new TimeInterval(now.AddHours(-1), now.AddHours(1));
            TimeInterval ti3 = new TimeInterval(now, now.AddHours(2));

            TimeInterval sut = TimeInterval.GetTotalTimeSpan(ti1, ti2, ti3);
            sut.StartTime.Should().Be(ti2.StartTime);
            sut.EndTime.Should().Be(ti3.EndTime);
        }

        //[Test]
        public void TestBad() {
            var ex = Assert.Throws<ArgumentException>(() => new TimeInterval(DateTime.Now, DateTime.Now.AddSeconds(-1)));
            Assert.AreEqual("startTime must be before endTime", ex.Message);
        }
    }
}
