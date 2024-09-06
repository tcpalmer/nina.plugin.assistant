using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using Moq;
using NINA.Astrometry;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class MoonAvoidanceExpertTest {

        [Test]
        public void testClassicRelaxOff() {
            IPlanTarget planTarget = GetPlanTarget();
            IPlanExposure planExposure = GetPlanExposure(true, 120, 14, 0, 5, -15);
            DateTime atTime = DateTime.Now;

            // Full moon, sep angle too small
            MoonAvoidanceExpert sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 14, SeparationAngle = 20 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeTrue();

            // Full moon, sep angle big enough
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 14, SeparationAngle = 120 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();

            // Moon age=10, sep angle too small
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 10, SeparationAngle = 107 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeTrue();

            // Moon age=10, sep angle big enough
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 10, SeparationAngle = 108 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();
        }

        [Test]
        public void testClassicNotRelaxZone() {
            IPlanTarget planTarget = GetPlanTarget();
            IPlanExposure planExposure = GetPlanExposure(true, 120, 14, 2, 5, -15);
            DateTime atTime = DateTime.Now;

            // Full moon, sep angle too small
            MoonAvoidanceExpert sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 14, SeparationAngle = 20 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeTrue();

            // Full moon, sep angle big enough
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 14, SeparationAngle = 120 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();

            // Moon age=10, sep angle too small
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 10, SeparationAngle = 107 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeTrue();

            // Moon age=10, sep angle big enough
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 20, MoonAge = 10, SeparationAngle = 108 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();
        }

        [Test]
        public void testClassicRelaxZone() {
            IPlanTarget planTarget = GetPlanTarget();
            IPlanExposure planExposure = GetPlanExposure(true, 120, 14, 2, 5, -15);
            DateTime atTime = DateTime.Now;

            // With altitude 0, separation of 112 is now OK at full
            MoonAvoidanceExpert sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = 0, MoonAge = 14, SeparationAngle = 112 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();

            // Less than min altitude, don't reject
            sut = new MoonAvoidanceExpertMock(TestUtil.TEST_LOCATION_1) { Altitude = -16, MoonAge = 14, SeparationAngle = 5 };
            sut.IsRejected(atTime, planTarget, planExposure).Should().BeFalse();
        }

        [Test]
        public void testNotMocked() {
            MoonAvoidanceExpert sut = new MoonAvoidanceExpert(TestUtil.TEST_LOCATION_1);
            IPlanTarget planTarget = GetPlanTarget();
            planTarget.Coordinates = TestUtil.M42;
            // Jan 17, 2024 8pm:
            //   moon is first quarter (age 6.85 at midpoint time), distance to M42 is ~62 degrees
            //   moon alt is ~53 degrees at highest point in plan (setting)
            planTarget.StartTime = new DateTime(2024, 1, 17, 20, 0, 0);
            planTarget.EndTime = new DateTime(2024, 1, 17, 20, 30, 0);
            planTarget.Project = PlanMocks.GetMockPlanProject("", ProjectState.Active).Object;

            IPlanExposure planExposure = GetPlanExposure(true, 90, 12, 0, 5, -15);
            sut.IsRejected(planTarget.StartTime, planTarget, planExposure).Should().BeTrue();

            planExposure = GetPlanExposure(true, 90, 11, 0, 5, -15);
            sut.IsRejected(planTarget.StartTime, planTarget, planExposure).Should().BeFalse();
        }

        private IPlanTarget GetPlanTarget() {
            Mock<IPlanTarget> pt = new Mock<IPlanTarget>();
            pt.SetupAllProperties();
            pt.SetupProperty(m => m.Name, "T1");

            return pt.Object;
        }

        private IPlanExposure GetPlanExposure(bool avoidanceEnabled, double separation, int width, double relaxScale, double relaxMaxAlt, double relaxMinAlt) {
            Mock<IPlanExposure> pe = new Mock<IPlanExposure>();
            pe.SetupAllProperties();
            pe.SetupProperty(m => m.MoonAvoidanceEnabled, avoidanceEnabled);
            pe.SetupProperty(m => m.MoonAvoidanceSeparation, separation);
            pe.SetupProperty(m => m.MoonAvoidanceWidth, width);
            pe.SetupProperty(m => m.MoonRelaxScale, relaxScale);
            pe.SetupProperty(m => m.MoonRelaxMaxAltitude, relaxMaxAlt);
            pe.SetupProperty(m => m.MoonRelaxMinAltitude, relaxMinAlt);
            pe.SetupProperty(m => m.FilterName, "FLT");
            return pe.Object;
        }
    }

    internal class MoonAvoidanceExpertMock : MoonAvoidanceExpert {
        public double Altitude { get; set; }
        public double MoonAge { get; set; }
        public double SeparationAngle { get; set; }

        public MoonAvoidanceExpertMock(ObserverInfo observerInfo) : base(observerInfo) {
            Altitude = 0;
            MoonAge = 0;
            SeparationAngle = 0;
        }

        public override DateTime GetMoonEvaluationTime(DateTime atTime, IPlanTarget planTarget) {
            return DateTime.Now;
        }

        public override double GetRelaxationMoonAltitude(DateTime evalTime) {
            return Altitude;
        }

        public override double GetMoonAge(DateTime atTime) {
            return MoonAge;
        }

        public override double GetMoonSeparationAngle(ObserverInfo location, DateTime atTime, Coordinates coordinates) {
            return SeparationAngle;
        }
    }
}