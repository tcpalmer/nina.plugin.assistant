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
    public class PercentCompleteRuleTest {

        [Test]
        public void testPercentComplete0() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);
        }

        [Test]
        public void testPercentComplete60() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 6);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.6, 0.00001);
        }

        [Test]
        public void testPercentComplete100() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 10);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1.0, 0.00001);
        }

        [Test]
        public void testPercentCompleteWithCompletedExpPlans() {
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, true);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("A", 10, 5);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("B", 10, 5);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("C", 10, 10);
            PlanMocks.AddMockPlanFilterToCompleted(targetMock, exposurePlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.66667, 0.0001);
        }

        [Test]
        public void testPercentCompleteGraderOff() {
            PercentCompleteRule sut = new PercentCompleteRule();
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, false);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 14);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 126);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1.0, 0.00001);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 110);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.96, 0.00001);
        }

        [Test]
        public void testPercentCompleteGraderOffCompletedExpPlans() {
            PercentCompleteRule sut = new PercentCompleteRule();
            ProfilePreference profilePreference = new ProfilePreference("abcd-1234");
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            scoringEngineMock.SetupProperty(se => se.ProfilePreference, profilePreference);

            Mock<IPlanProject> projectMock = PlanMocks.GetMockPlanProject("p1", ProjectState.Active);
            projectMock.SetupProperty(m => m.EnableGrader, false);

            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.SetupProperty(m => m.Project, projectMock.Object);
            projectMock.Object.Targets.Add(targetMock.Object);

            Mock<IPlanExposure> exposurePlanMock = PlanMocks.GetMockPlanExposure("", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 14);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);
            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 126);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("C", 10, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 14);
            PlanMocks.AddMockPlanFilterToCompleted(targetMock, exposurePlanMock);

            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1.0, 0.00001);

            exposurePlanMock = PlanMocks.GetMockPlanExposure("", 100, 0);
            exposurePlanMock.SetupProperty(m => m.Acquired, 110);
            PlanMocks.AddMockPlanFilter(targetMock, exposurePlanMock);

            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.97, 0.00001);
        }
    }
}