using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using FluentAssertions;
using FluentAssertions.Extensions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class PlannerTest {

        [Test]
        public void testFilterForReadyComplete() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", ProjectState.Active);
            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanExposure("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            Assert.IsNull(new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForIncomplete(null));

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForIncomplete(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(2);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            pp = projects[1];
            pp.Name.Should().Be("pp2");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectComplete);
            pt1 = pp.Targets[0];
            pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
            pf1 = pt1.ExposurePlans[1];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
        }

        [Test]
        public void testFilterForIncomplete() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanExposure("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", 10, 12);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForIncomplete(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();

            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            IPlanTarget pt2 = pp.Targets[1];
            pt2.Rejected.Should().BeTrue();
            pt2.RejectedReason.Should().Be(Reasons.TargetAllExposurePlans);

            IPlanExposure pf2 = pt2.ExposurePlans[0];
            pf2.Rejected.Should().BeTrue();
            pf2.RejectedReason.Should().Be(Reasons.FilterComplete);
            IPlanExposure pf3 = pt2.ExposurePlans[1];
            pf3.Rejected.Should().BeTrue();
            pf3.RejectedReason.Should().Be(Reasons.FilterComplete);
        }

        [Test]
        public void testTargetNoExposurePlans() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);
            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForIncomplete(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();

            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeFalse();

            IPlanTarget pt2 = pp.Targets[1];
            pt2.ExposurePlans.Count.Should().Be(0);
            pt2.Rejected.Should().BeTrue();
            pt2.RejectedReason.Should().Be(Reasons.TargetAllExposurePlans);
        }

        [Test]
        public void testFilterForVisibilityNeverRises() {

            // Southern hemisphere location and IC1805
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_2);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("IC1805", TestUtil.IC1805);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForVisibility(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNeverRises);
        }

        [Test]
        public void testFilterForVisibilityNotNow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 6, 17, 18, 0, 0), profileMock.Object).FilterForVisibility(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNotVisible);
        }

        [Test]
        public void testFilterForVisibilityVisible() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 19, 0, 0), profileMock.Object).FilterForVisibility(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
        }

        [Test]
        public void testFilterForVisibilityNotYetVisible() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForVisibility(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectAllTargets);
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeTrue();
            pt1.RejectedReason.Should().Be(Reasons.TargetNotYetVisible);
        }

        [Test]
        public void testFilterForMoonAvoidance() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.StartTime, new DateTime(2023, 12, 25, 18, 9, 0));
            pt.SetupProperty(m => m.EndTime, new DateTime(2023, 12, 26, 5, 17, 0));

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("L", 10, 0);
            pf.SetupProperty(f => f.MoonAvoidanceEnabled, true);
            pf.SetupProperty(f => f.MoonAvoidanceSeparation, 50);
            pf.SetupProperty(f => f.MoonAvoidanceWidth, 7);
            PlanMocks.AddMockPlanFilter(pt, pf);

            pf = PlanMocks.GetMockPlanExposure("Ha", 10, 0);
            pf.SetupProperty(f => f.MoonAvoidanceEnabled, true);
            pf.SetupProperty(f => f.MoonAvoidanceSeparation, 30);
            pf.SetupProperty(f => f.MoonAvoidanceWidth, 7);
            PlanMocks.AddMockPlanFilter(pt, pf);

            PlanMocks.AddMockPlanTarget(pp1, pt);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object);

            projects = new Planner(new DateTime(2023, 12, 25, 18, 0, 0), profileMock.Object).FilterForMoonAvoidance(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();

            IPlanExposure pf1 = pt1.ExposurePlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterMoonAvoidance);

            pf1 = pt1.ExposurePlans[1];
            pf1.Rejected.Should().BeFalse();
        }

        [Test]
        public void testCheckForVisibleNowNoWait() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt1 = PlanMocks.GetMockPlanTarget("T1", TestUtil.M42);
            pt1.SetupProperty(t => t.StartTime, atTime.AddMinutes(-10));
            pt1.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            Mock<IPlanTarget> pt2 = PlanMocks.GetMockPlanTarget("T2", TestUtil.M42);
            pt2.SetupProperty(t => t.StartTime, atTime.AddMinutes(10));
            pt2.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));

            PlanMocks.AddMockPlanTarget(pp, pt1);
            PlanMocks.AddMockPlanTarget(pp, pt2);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp.Object);

            DateTime? wait = new Planner(atTime, profileMock.Object).CheckForVisibleNow(projects);
            wait.Should().BeNull();
        }

        [Test]
        public void testCheckForVisibleNowWait() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt1 = PlanMocks.GetMockPlanTarget("T1", TestUtil.M42);
            pt1.SetupProperty(t => t.StartTime, atTime.AddMinutes(10)); // <- should find this
            pt1.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt1.SetupProperty(t => t.Rejected, true);
            pt1.SetupProperty(t => t.RejectedReason, Reasons.TargetNotYetVisible);
            Mock<IPlanTarget> pt2 = PlanMocks.GetMockPlanTarget("T2", TestUtil.M42);
            pt2.SetupProperty(t => t.StartTime, atTime.AddMinutes(20));
            pt2.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt2.SetupProperty(t => t.Rejected, true);
            pt2.SetupProperty(t => t.RejectedReason, Reasons.TargetNotYetVisible);
            Mock<IPlanTarget> pt3 = PlanMocks.GetMockPlanTarget("T3", TestUtil.M42);
            pt3.SetupProperty(t => t.StartTime, atTime.AddMinutes(5));
            pt3.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            pt3.SetupProperty(t => t.Rejected, true);
            pt3.SetupProperty(t => t.RejectedReason, Reasons.FilterMoonAvoidance);

            PlanMocks.AddMockPlanTarget(pp, pt1);
            PlanMocks.AddMockPlanTarget(pp, pt2);
            PlanMocks.AddMockPlanTarget(pp, pt3);
            List<IPlanProject> projects = PlanMocks.ProjectsList(pp.Object);

            DateTime? wait = new Planner(atTime, profileMock.Object).CheckForVisibleNow(projects);
            wait.Should().BeSameDateAs(atTime.AddMinutes(10));
        }

        [Test]
        public void testSelectTargetByScore() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", ProjectState.Active);
            pt = PlanMocks.GetMockPlanTarget("IC1805", TestUtil.IC1805);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);

            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.Setup(m => m.ScoreTarget(It.Is<IPlanTarget>(t => t.Name.Equals("IC1805")))).Returns(1);

            IPlanTarget selected = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).SelectTargetByScore(projects, scoringEngineMock.Object);
            Assert.IsNotNull(selected);
            selected.Name.Should().Be("IC1805");
        }

        [Test]
        public void testGetTargetTimeWindow() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            int minimumMinutes = 30;
            pp.SetupProperty(p => p.MinimumTime, minimumMinutes);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp, pt);

            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(10));
            pt.SetupProperty(t => t.EndTime, atTime.AddMinutes(50));
            TimeInterval window = new Planner(atTime, profileMock.Object).GetTargetTimeWindow(atTime, pt.Object);
            window.StartTime.Should().BeSameDateAs(23.January(2023).At(18, 10, 0));
            window.EndTime.Should().BeSameDateAs(23.January(2023).At(18, 40, 0));
            window.Duration.Should().Be(minimumMinutes * 60);

            minimumMinutes = 60;
            pp.SetupProperty(p => p.MinimumTime, minimumMinutes);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(-10));
            pt.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            window = new Planner(atTime, profileMock.Object).GetTargetTimeWindow(atTime, pt.Object);
            window.StartTime.Should().BeSameDateAs(23.January(2023).At(18, 0, 0));
            window.EndTime.Should().BeSameDateAs(23.January(2023).At(19, 0, 0));
            window.Duration.Should().Be(minimumMinutes * 60);
        }

        [Test]
        [Ignore("should test in the future")]
        public void testPerfectPlan() {

            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 26);

            List<IPlanProject> projects = new List<IPlanProject> {
                PlanMocks.GetMockPlanProject("pp1", ProjectState.Active).Object,
                PlanMocks.GetMockPlanProject("pp2", ProjectState.Active).Object
            };

            List<SchedulerPlan> plans = Planner.GetPerfectPlan(atTime, profileMock.Object, projects);
            foreach (SchedulerPlan plan in plans) {
                TestContext.WriteLine("PLAN -----------------------------------------------------");
                TestContext.WriteLine(plan.PlanSummary());
            }

        }

        [Test]
        public void testNotEmulator() {
            // prevent commits with emulator on
            Planner.USE_EMULATOR.Should().BeFalse();
        }
    }

}
