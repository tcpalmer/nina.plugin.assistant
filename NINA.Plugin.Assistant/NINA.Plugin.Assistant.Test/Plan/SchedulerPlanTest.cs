using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using Moq;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class SchedulerPlanTest {

        [Test]
        public void TestPlanExposureNoThrottle() {

            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 5, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(5);

            exposurePlan = GetExposurePlan(10, 10, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 20, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(0);
        }

        [Test]
        public void TestPlanExposureThrottle() {

            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(100).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(100).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(200).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(50).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(200).Should().Be(12);
        }

        [Test]
        public void TestFlatsSessionId() {
            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_1);

            Project project = new Project() {
                CreateDate = DateTime.Now.AddDays(-6),
                FlatsHandling = 7,
                RuleWeights = new List<RuleWeight>(),
                Targets = new List<Target>()
            };

            PlanProject pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(1);

            project.CreateDate = DateTime.Now.AddDays(-7);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(1);

            project.CreateDate = DateTime.Now.AddDays(-8);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(2);

            project.FlatsHandling = Project.FLATS_HANDLING_OFF;
            project.CreateDate = DateTime.Now.AddDays(-1);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(1);
            project.CreateDate = DateTime.Now.AddDays(-2);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(2);

            project.FlatsHandling = Project.FLATS_HANDLING_TARGET_COMPLETION;
            project.CreateDate = DateTime.Now.AddDays(-1);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(1);
            project.CreateDate = DateTime.Now.AddDays(-2);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(2);

            project.FlatsHandling = Project.FLATS_HANDLING_IMMEDIATE;
            project.CreateDate = DateTime.Now.AddDays(-1);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(1);
            project.CreateDate = DateTime.Now.AddDays(-2);
            pp = new PlanProject(profileMock.Object.ActiveProfile, project);
            pp.SessionId.Should().Be(2);
        }

        private ExposurePlan GetExposurePlan(int desired, int accepted, int acquired) {
            return new ExposurePlan {
                Desired = desired,
                Accepted = accepted,
                Acquired = acquired
            };
        }

        private ExposureTemplate GetExposureTemplate() {
            return new ExposureTemplate {
                BinningMode = new BinningMode(1, 1)
            };
        }

    }
}
