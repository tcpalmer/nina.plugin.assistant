using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring.Rules {

    [TestFixture]
    public class MosaicCompletionRuleTest {

        [Test]
        public void testNotMosaicProject() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            MosaicCompletionRule sut = new MosaicCompletionRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);
        }

        [Test]
        public void testOnePanelProject() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.IsMosaic, true);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            MosaicCompletionRule sut = new MosaicCompletionRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);
        }

        [Test]
        public void testOverAverage() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.IsMosaic, true);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock1 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock1.SetupProperty(m => m.Project, projectMock.Object);
            targetMock1.SetupProperty(m => m.DatabaseId, 0);
            projectMock.Object.Targets.Add(targetMock1.Object);
            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 9);
            PlanMocks.AddMockPlanFilter(targetMock1, exposurePlanMock);

            Mock<IPlanTarget> targetMock2 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock2.SetupProperty(m => m.Project, projectMock.Object);
            targetMock2.SetupProperty(m => m.DatabaseId, 1);
            projectMock.Object.Targets.Add(targetMock2.Object);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 6);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);

            MosaicCompletionRule sut = new MosaicCompletionRule();
            sut.Score(scoringEngineMock.Object, targetMock1.Object).Should().BeApproximately(0, 0.00001);
        }

        [Test]
        public void testBelowAverage() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.IsMosaic, true);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock1 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock1.SetupProperty(m => m.Project, projectMock.Object);
            targetMock1.SetupProperty(m => m.DatabaseId, 0);
            projectMock.Object.Targets.Add(targetMock1.Object);
            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 6);
            PlanMocks.AddMockPlanFilter(targetMock1, exposurePlanMock);

            Mock<IPlanTarget> targetMock2 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock2.SetupProperty(m => m.Project, projectMock.Object);
            targetMock2.SetupProperty(m => m.DatabaseId, 1);
            projectMock.Object.Targets.Add(targetMock2.Object);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 9);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);

            MosaicCompletionRule sut = new MosaicCompletionRule();
            sut.Score(scoringEngineMock.Object, targetMock1.Object).Should().BeApproximately(0.3, 0.00001);
        }

        [Test]
        public void testFourPanels() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.IsMosaic, true);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock1 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock1.SetupProperty(m => m.Project, projectMock.Object);
            targetMock1.SetupProperty(m => m.DatabaseId, 0);
            projectMock.Object.Targets.Add(targetMock1.Object);
            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 2);
            PlanMocks.AddMockPlanFilter(targetMock1, exposurePlanMock);

            Mock<IPlanTarget> targetMock2 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock2.SetupProperty(m => m.Project, projectMock.Object);
            targetMock2.SetupProperty(m => m.DatabaseId, 1);
            projectMock.Object.Targets.Add(targetMock2.Object);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 2);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);

            Mock<IPlanTarget> targetMock3 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock3.SetupProperty(m => m.Project, projectMock.Object);
            targetMock3.SetupProperty(m => m.DatabaseId, 2);
            projectMock.Object.Targets.Add(targetMock3.Object);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 2);
            PlanMocks.AddMockPlanFilter(targetMock3, exposurePlanMock);

            Mock<IPlanTarget> targetMock4 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock4.SetupProperty(m => m.Project, projectMock.Object);
            targetMock4.SetupProperty(m => m.DatabaseId, 3);
            projectMock.Object.Targets.Add(targetMock4.Object);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 3);
            PlanMocks.AddMockPlanFilter(targetMock4, exposurePlanMock);

            MosaicCompletionRule sut = new MosaicCompletionRule();
            sut.Score(scoringEngineMock.Object, targetMock1.Object).Should().BeApproximately(0.0333333, 0.00001);
        }

        [Test]
        public void testGradingOff() {
            MosaicCompletionRule sut = new MosaicCompletionRule();
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.IsMosaic, true);
            projectMock.SetupProperty(m => m.EnableGrader, false);

            Mock<IPlanTarget> targetMock1 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock1.SetupProperty(m => m.Project, projectMock.Object);
            targetMock1.SetupProperty(m => m.DatabaseId, 0);
            projectMock.Object.Targets.Add(targetMock1.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 10);
            PlanMocks.AddMockPlanFilter(targetMock1, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 14);
            PlanMocks.AddMockPlanFilter(targetMock1, exposurePlanMock);


            Mock<IPlanTarget> targetMock2 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock2.SetupProperty(m => m.Project, projectMock.Object);
            targetMock2.SetupProperty(m => m.DatabaseId, 1);
            projectMock.Object.Targets.Add(targetMock2.Object);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 160);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 7);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 10);
            PlanMocks.AddMockPlanFilter(targetMock2, exposurePlanMock);


            Mock<IPlanTarget> targetMock3 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock3.SetupProperty(m => m.Project, projectMock.Object);
            targetMock3.SetupProperty(m => m.DatabaseId, 2);
            projectMock.Object.Targets.Add(targetMock3.Object);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 130);
            PlanMocks.AddMockPlanFilter(targetMock3, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 14);
            PlanMocks.AddMockPlanFilter(targetMock3, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 18);
            PlanMocks.AddMockPlanFilter(targetMock3, exposurePlanMock);

            // Potential (targetMock1) completion > average of the others
            sut.Score(scoringEngineMock.Object, targetMock1.Object).Should().BeApproximately(0, 0.00001);

            Mock<IPlanTarget> targetMock4 = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock4.SetupProperty(m => m.Project, projectMock.Object);
            targetMock4.SetupProperty(m => m.DatabaseId, 2);
            projectMock.Object.Targets.Add(targetMock4.Object);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 30);
            PlanMocks.AddMockPlanFilter(targetMock4, exposurePlanMock);

            // Potential (targetMock4) completion < average of the others
            sut.Score(scoringEngineMock.Object, targetMock4.Object).Should().BeApproximately(0.322222, 0.00001);
        }
    }

}
