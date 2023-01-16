using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Astrometry {

    [TestFixture]
    public class NighttimeCircumstancesTest {

        [Test]
        public void testLocal() {

            var sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_4, new DateTime(2023, 1, 11, 1, 2, 3));
            Assert.That(sut.CivilTwilightStart, Is.EqualTo(new DateTime(2023, 1, 11, 17, 22, 38)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightStart, Is.EqualTo(new DateTime(2023, 1, 11, 17, 50, 38)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightStart, Is.EqualTo(new DateTime(2023, 1, 11, 18, 22, 11)).Within(1).Seconds);
            Assert.That(sut.NighttimeStart, Is.EqualTo(new DateTime(2023, 1, 11, 18, 53, 0)).Within(1).Seconds);
            Assert.That(sut.NighttimeEnd, Is.EqualTo(new DateTime(2023, 1, 12, 5, 56, 21)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightEnd, Is.EqualTo(new DateTime(2023, 1, 12, 6, 27, 7)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightEnd, Is.EqualTo(new DateTime(2023, 1, 12, 6, 58, 38)).Within(1).Seconds);
            Assert.That(sut.CivilTwilightEnd, Is.EqualTo(new DateTime(2023, 1, 12, 7, 26, 35)).Within(1).Seconds);

            Tuple<DateTime, DateTime> span = sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_CIVIL);
            Assert.That(span.Item1, Is.EqualTo(new DateTime(2023, 1, 11, 17, 22, 38)).Within(1).Seconds);
            Assert.That(span.Item2, Is.EqualTo(new DateTime(2023, 1, 12, 7, 26, 35)).Within(1).Seconds);

            span = sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_NAUTICAL);
            Assert.That(span.Item1, Is.EqualTo(new DateTime(2023, 1, 11, 17, 50, 38)).Within(1).Seconds);
            Assert.That(span.Item2, Is.EqualTo(new DateTime(2023, 1, 12, 6, 58, 38)).Within(1).Seconds);

            span = sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_ASTRO);
            Assert.That(span.Item1, Is.EqualTo(new DateTime(2023, 1, 11, 18, 22, 11)).Within(1).Seconds);
            Assert.That(span.Item2, Is.EqualTo(new DateTime(2023, 1, 12, 6, 27, 7)).Within(1).Seconds);

            span = sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE);
            Assert.That(span.Item1, Is.EqualTo(new DateTime(2023, 1, 11, 18, 53, 0)).Within(1).Seconds);
            Assert.That(span.Item2, Is.EqualTo(new DateTime(2023, 1, 12, 5, 56, 21)).Within(1).Seconds);
        }

        [Test]
        public void testHighLatitude() {

            // TEST_LOCATION_5 is Waskaganish, Quebec at lat=51.48.  See https://www.timeanddate.com/sun/@6176565

            DateTime dt = new DateTime(2023, 1, 11);
            var sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_5, new DateTime(2023, 1, 11, 1, 2, 3));
            TestUtil.AssertTime(dt, sut.CivilTwilightStart, 16, 29, 20);
            TestUtil.AssertTime(dt, sut.NauticalTwilightStart, 17, 8, 18);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightStart, 17, 50, 30);
            TestUtil.AssertTime(dt, sut.NighttimeStart, 18, 30, 40);
            dt = dt.AddDays(1);
            TestUtil.AssertTime(dt, sut.NighttimeEnd, 6, 15, 4);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightEnd, 6, 55, 12);
            TestUtil.AssertTime(dt, sut.NauticalTwilightEnd, 7, 37, 16);
            TestUtil.AssertTime(dt, sut.CivilTwilightEnd, 8, 16, 8);

            // TEST_LOCATION_6 is Sanikiluaq, Nunavut at lat=56.54.  See https://www.timeanddate.com/sun/canada/sanikiluaq

            dt = new DateTime(2023, 5, 1);
            sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_6, dt);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightStart, 22, 48, 4);
            dt = dt.AddDays(1);
            TestUtil.AssertTime(dt, sut.NighttimeStart, 0, 51, 2);
            TestUtil.AssertTime(dt, sut.NighttimeEnd, 1, 36, 12);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightEnd, 3, 39, 8);

            // Nighttime is lost here ...
            dt = new DateTime(2023, 5, 2);
            sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_6, dt);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightStart, 22, 51, 30);
            dt = dt.AddDays(1);
            sut.NighttimeStart.Should().BeNull();
            sut.NighttimeEnd.Should().BeNull();
            sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE).Should().BeNull();
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightEnd, 3, 35, 30);

            dt = new DateTime(2023, 5, 27);
            sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_6, dt);
            dt = dt.AddDays(1);
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightStart, 1, 2, 30);
            sut.NighttimeStart.Should().BeNull();
            sut.NighttimeEnd.Should().BeNull();
            sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE).Should().BeNull();
            TestUtil.AssertTime(dt, sut.AstronomicalTwilightEnd, 1, 25, 25);

            // ... and astro is lost here
            dt = new DateTime(2023, 5, 28);
            sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_6, dt);
            TestUtil.AssertTime(dt, sut.NauticalTwilightStart, 22, 47, 30);
            sut.AstronomicalTwilightStart.Should().BeNull();
            sut.NighttimeStart.Should().BeNull();
            sut.NighttimeEnd.Should().BeNull();
            sut.AstronomicalTwilightEnd.Should().BeNull();
            sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE).Should().BeNull();
            sut.GetTwilightSpan(AssistantFilterPreferences.TWILIGHT_INCLUDE_ASTRO).Should().BeNull();
            dt = dt.AddDays(1);
            TestUtil.AssertTime(dt, sut.NauticalTwilightEnd, 3, 40, 35);
        }

        [Test]
        public void testLeapDays() {
            // Spring
            var sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_4, new DateTime(2023, 3, 11, 1, 2, 3));
            Assert.That(sut.CivilTwilightStart, Is.EqualTo(new DateTime(2023, 3, 11, 18, 20, 36)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightStart, Is.EqualTo(new DateTime(2023, 3, 11, 18, 46, 5)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightStart, Is.EqualTo(new DateTime(2023, 3, 11, 19, 15, 42)).Within(1).Seconds);
            Assert.That(sut.NighttimeStart, Is.EqualTo(new DateTime(2023, 3, 11, 19, 45, 29)).Within(1).Seconds);
            Assert.That(sut.NighttimeEnd, Is.EqualTo(new DateTime(2023, 3, 12, 6, 7, 11)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightEnd, Is.EqualTo(new DateTime(2023, 3, 12, 6, 36, 56)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightEnd, Is.EqualTo(new DateTime(2023, 3, 12, 7, 6, 29)).Within(1).Seconds);
            Assert.That(sut.CivilTwilightEnd, Is.EqualTo(new DateTime(2023, 3, 12, 7, 31, 56)).Within(1).Seconds);

            // Fall
            sut = new NighttimeCircumstances(TestUtil.TEST_LOCATION_4, new DateTime(2023, 11, 4, 1, 2, 3));
            Assert.That(sut.CivilTwilightStart, Is.EqualTo(new DateTime(2023, 11, 4, 18, 18, 35)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightStart, Is.EqualTo(new DateTime(2023, 11, 4, 18, 45, 8)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightStart, Is.EqualTo(new DateTime(2023, 11, 4, 19, 15, 26)).Within(1).Seconds);
            Assert.That(sut.NighttimeStart, Is.EqualTo(new DateTime(2023, 11, 4, 19, 45, 19)).Within(1).Seconds);
            Assert.That(sut.NighttimeEnd, Is.EqualTo(new DateTime(2023, 11, 5, 5, 15, 35)).Within(1).Seconds);
            Assert.That(sut.AstronomicalTwilightEnd, Is.EqualTo(new DateTime(2023, 11, 5, 5, 45, 31)).Within(1).Seconds);
            Assert.That(sut.NauticalTwilightEnd, Is.EqualTo(new DateTime(2023, 11, 5, 6, 15, 52)).Within(1).Seconds);
            Assert.That(sut.CivilTwilightEnd, Is.EqualTo(new DateTime(2023, 11, 5, 6, 42, 29)).Within(1).Seconds);
        }

        [Test]
        public void testPolarCircleLocation() {
            var ex = Assert.Throws<ArgumentException>(() => new NighttimeCircumstances(TestUtil.TEST_LOCATION_3, DateTime.Now));
            ex.Message.Should().Be("locations cannot be above a polar circle");
        }
    }
}
