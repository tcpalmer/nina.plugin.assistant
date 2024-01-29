using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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

            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(false, startTime, selectedTarget, null);

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

            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(false, startTime, selectedTarget, null);

            TimeSpan precision = TimeSpan.FromSeconds(1);
            stopTime.Should().BeCloseTo(startTime.AddHours(3), precision);
        }

        [Test]
        public void testSmartWindow1() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);
            TimeSpan precision = TimeSpan.FromSeconds(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;

            Mock<IPlanTarget> selected = PlanMocks.GetMockPlanTarget("pt1", TestUtil.M42);
            selected.Object.EndTime = startTime.AddMinutes(120);
            PlanMocks.AddMockPlanTarget(pp, selected);

            List<IPlanProject> projects = new List<IPlanProject> { pp.Object };

            // Without smart, it's just the minimum
            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(false, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(startTime.AddMinutes(30), precision);

            // Otherwise, it's the end time
            stopTime = new PlanStopTimeExpert().GetStopTime(true, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(selected.Object.EndTime, precision);
        }

        [Test]
        public void testSmartWindow2() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);
            TimeSpan precision = TimeSpan.FromSeconds(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;

            Mock<IPlanTarget> selected = PlanMocks.GetMockPlanTarget("pt1", TestUtil.M42);
            selected.Object.EndTime = startTime.AddMinutes(120);
            PlanMocks.AddMockPlanTarget(pp, selected);

            Mock<IPlanTarget> other = PlanMocks.GetMockPlanTarget("pt2", TestUtil.M42);
            other.Object.StartTime = startTime.AddMinutes(10);
            other.Object.EndTime = other.Object.StartTime.AddMinutes(180);
            other.Object.Rejected = true;
            other.Object.RejectedReason = Reasons.TargetLowerScore;
            PlanMocks.AddMockPlanTarget(pp, other);

            List<IPlanProject> projects = new List<IPlanProject> { pp.Object };

            // Valid future concurrent so just use minimum
            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(true, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(startTime.AddMinutes(30), precision);
        }

        [Test]
        public void testSmartWindow3() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);
            TimeSpan precision = TimeSpan.FromSeconds(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;

            Mock<IPlanTarget> selected = PlanMocks.GetMockPlanTarget("pt1", TestUtil.M42);
            selected.Object.EndTime = startTime.AddMinutes(120);
            PlanMocks.AddMockPlanTarget(pp, selected);

            Mock<IPlanTarget> other1 = PlanMocks.GetMockPlanTarget("pt2", TestUtil.M42);
            other1.Object.StartTime = startTime.AddMinutes(10);
            other1.Object.EndTime = other1.Object.StartTime.AddMinutes(20);
            other1.Object.Rejected = true;
            other1.Object.RejectedReason = Reasons.TargetLowerScore;
            PlanMocks.AddMockPlanTarget(pp, other1);

            Mock<IPlanTarget> other2 = PlanMocks.GetMockPlanTarget("pt3", TestUtil.M42);
            other2.Object.StartTime = selected.Object.EndTime.AddMinutes(30);
            other2.Object.EndTime = other2.Object.StartTime.AddMinutes(90);
            other2.Object.Rejected = true;
            other2.Object.RejectedReason = Reasons.TargetNotYetVisible;
            PlanMocks.AddMockPlanTarget(pp, other2);

            List<IPlanProject> projects = new List<IPlanProject> { pp.Object };

            // Future concurrent that can't be completed so skipped for next target that doesn't start until after selected ends
            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(true, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(other2.Object.StartTime, precision);
        }

        [Test]
        public void testSmartWindow4() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);
            TimeSpan precision = TimeSpan.FromSeconds(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;

            Mock<IPlanTarget> selected = PlanMocks.GetMockPlanTarget("pt1", TestUtil.M42);
            selected.Object.EndTime = startTime.AddMinutes(120);
            PlanMocks.AddMockPlanTarget(pp, selected);

            Mock<IPlanTarget> other2 = PlanMocks.GetMockPlanTarget("pt3", TestUtil.M42);
            other2.Object.StartTime = selected.Object.EndTime.AddMinutes(30);
            other2.Object.EndTime = other2.Object.StartTime.AddMinutes(90);
            other2.Object.Rejected = true;
            other2.Object.RejectedReason = Reasons.TargetNotYetVisible;
            PlanMocks.AddMockPlanTarget(pp, other2);

            List<IPlanProject> projects = new List<IPlanProject> { pp.Object };

            // Future non-concurrent, use that target's start time
            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(true, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(other2.Object.StartTime, precision);
        }

        [Test]
        public void testSmartWindow5() {
            DateTime startTime = DateTime.Now.Date.AddHours(1);
            TimeSpan precision = TimeSpan.FromSeconds(1);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            pp.Object.MinimumTime = 30;

            Mock<IPlanTarget> selected = PlanMocks.GetMockPlanTarget("pt1", TestUtil.M42);
            selected.Object.EndTime = startTime.AddMinutes(120);
            PlanMocks.AddMockPlanTarget(pp, selected);

            List<IPlanProject> projects = new List<IPlanProject> { pp.Object };

            // Nothing in the future, use selected end time
            DateTime stopTime = new PlanStopTimeExpert().GetStopTime(true, startTime, selected.Object, projects);
            stopTime.Should().BeCloseTo(selected.Object.EndTime, precision);
        }
    }
}