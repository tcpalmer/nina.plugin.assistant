using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class PlanStopTimeExpertTest {

        [Test]
        public void testMeridianWindowOff() {
            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;
            pp.Object.MeridianWindow = 0;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("pt", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp, pt);

            DateTime startTime = DateTime.Now.Date.AddHours(1);
            IPlanTarget selectedTarget = pt.Object;

            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(startTime, selectedTarget, null);

            TimeSpan precision = TimeSpan.FromSeconds(1);
            stopTime.Should().BeCloseTo(startTime.AddMinutes(30), precision);
        }

        [Test]
        public void testMeridianWindowOn() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;
            pp.Object.MeridianWindow = 60;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("pt", TestUtil.M42);
            pt.Object.MeridianWindow = new TimeInterval(startTime.AddMinutes(-10), startTime.AddHours(3));
            PlanMocks.AddMockPlanTarget(pp, pt);

            IPlanTarget selectedTarget = pt.Object;

            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(startTime, selectedTarget, null);

            TimeSpan precision = TimeSpan.FromSeconds(1);
            stopTime.Should().BeCloseTo(startTime.AddHours(3), precision);
        }
    }

}
