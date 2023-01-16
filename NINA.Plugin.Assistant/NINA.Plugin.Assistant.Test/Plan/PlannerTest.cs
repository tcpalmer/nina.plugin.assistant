using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using FluentAssertions;
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
        public void testFilterForReadyInactive() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", Project.STATE_INACTIVE);
            Mock<IPlanProject> pp3 = PlanMocks.GetMockPlanProject("pp3", Project.STATE_DRAFT);
            Mock<IPlanProject> pp4 = PlanMocks.GetMockPlanProject("pp4", Project.STATE_CLOSED);

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object, pp3.Object, pp4.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForReady(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(4);

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
            pp.RejectedReason.Should().Be(Reasons.ProjectNotActive);

            pp = projects[2];
            pp.Name.Should().Be("pp3");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectNotActive);

            pp = projects[3];
            pp.Name.Should().Be("pp4");
            pp.Rejected.Should().BeTrue();
            pp.RejectedReason.Should().Be(Reasons.ProjectNotActive);
        }

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

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForReady(projects);
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
        public void testFilterForReadyDates() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);

            Mock<IPlanProject> pp1 = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp1, pt);

            Mock<IPlanProject> pp2 = PlanMocks.GetMockPlanProject("pp2", Project.STATE_ACTIVE);
            pp2.SetupProperty(m => m.StartDate, new DateTime(2024, 1, 1));
            pp2.SetupProperty(m => m.EndDate, new DateTime(2025, 1, 1));
            pt = PlanMocks.GetMockPlanTarget("M31", TestUtil.M31);
            pf = PlanMocks.GetMockPlanFilter("OIII", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp2, pt);

            List<IPlanProject> projects = PlanMocks.ProjectsList(pp1.Object, pp2.Object);

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForReady(projects);
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
            pp.RejectedReason.Should().Be(Reasons.ProjectDates);
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

            projects = new Planner(new DateTime(2023, 12, 17, 18, 0, 0), profileMock.Object).FilterForVisibility(projects);
            Assert.IsNotNull(projects);
            projects.Count.Should().Be(1);

            IPlanProject pp = projects[0];
            pp.Name.Should().Be("pp1");
            pp.Rejected.Should().BeFalse();
            IPlanTarget pt1 = pp.Targets[0];
            pt1.Rejected.Should().BeFalse();
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
        [Ignore("TBD")]
        public void testPlanExposures() {
            // TODO: waiting for implementation to be more fleshed out
        }
    }

}
