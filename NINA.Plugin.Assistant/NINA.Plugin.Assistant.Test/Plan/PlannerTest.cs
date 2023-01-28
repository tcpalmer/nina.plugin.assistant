using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", Project.STATE_ACTIVE);
            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanFilter("OIII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanFilter("SII", 10, 10);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForIncomplete(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(2);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
            IPlanFilter pf1 = pt1.FilterPlans[0];
            pf1.Rejected.Should().BeFalse();

            pp = projects[1];
            pp.Name.Should().Be("pp2");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectComplete);
            pt1 = pp.Targets[0];
            pf1 = pt1.FilterPlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
            pf1 = pt1.FilterPlans[1];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterComplete);
        }

        [Test]
        public void testFilterForVisibilityNeverRises() {

            // Southern hemisphere location and IC1805
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_2);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            pt.SetupProperty(m => m.StartTime, new DateTime(2023, 12, 25, 18, 9, 0));
            pt.SetupProperty(m => m.EndTime, new DateTime(2023, 12, 26, 5, 17, 0));

            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("L", 10, 0);
            AssistantFilterPreferences afp = pf.Object.Preferences;
            afp.MoonAvoidanceEnabled = true;
            afp.MoonAvoidanceSeparation = 50;
            afp.MoonAvoidanceWidth = 7;
            PlanMocks.AddMockPlanFilter(pt, pf);

            pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
            afp = pf.Object.Preferences;
            afp.MoonAvoidanceEnabled = true;
            afp.MoonAvoidanceSeparation = 30;
            afp.MoonAvoidanceWidth = 7;
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

            IPlanFilter pf1 = pt1.FilterPlans[0];
            pf1.Rejected.Should().BeTrue();
            pf1.RejectedReason.Should().Be(Reasons.FilterMoonAvoidance);

            pf1 = pt1.FilterPlans[1];
            pf1.Rejected.Should().BeFalse();
        }

        [Test]
        public void testCheckForVisibleNowNoWait() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 23, 18, 0, 0);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
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

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
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

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", Project.STATE_ACTIVE);
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

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            int minimumMinutes = 30;
            pp.Object.Preferences = GetProjectPreferences(minimumMinutes);
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
            pp.Object.Preferences = GetProjectPreferences(minimumMinutes);
            pt.SetupProperty(t => t.StartTime, atTime.AddMinutes(-10));
            pt.SetupProperty(t => t.EndTime, atTime.AddMinutes(120));
            window = new Planner(atTime, profileMock.Object).GetTargetTimeWindow(atTime, pt.Object);
            window.StartTime.Should().BeSameDateAs(23.January(2023).At(18, 0, 0));
            window.EndTime.Should().BeSameDateAs(23.January(2023).At(19, 0, 0));
            window.Duration.Should().Be(minimumMinutes * 60);
        }

        private AssistantProjectPreferences GetProjectPreferences(int minimumMinutes) {
            AssistantProjectPreferences app = new AssistantProjectPreferences();
            app.SetDefaults();
            app.MinimumTime = minimumMinutes;

            Dictionary<string, IScoringRule> allRules = ScoringRule.GetAllScoringRules();
            foreach (KeyValuePair<string, IScoringRule> item in allRules) {
                app.RuleWeights[item.Key] = 1;
            }

            return app;
        }
    }

}
