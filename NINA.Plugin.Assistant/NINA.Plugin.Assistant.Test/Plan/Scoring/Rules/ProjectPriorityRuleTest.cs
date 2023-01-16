using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring.Rules {

    [TestFixture]
    public class ProjectPriorityRuleTest {

        [Test]
        public void testProjectPriority() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = new Mock<IPlanTarget>().SetupAllProperties();
            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", Project.STATE_ACTIVE);
            targetMock.SetupProperty(m => m.Project, pp.Object);

            ProjectPriorityRule sut = new ProjectPriorityRule();

            pp.SetupProperty(m => m.Priority, Project.PRIORITY_LOW);
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);

            pp.SetupProperty(m => m.Priority, Project.PRIORITY_NORMAL);
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.5, 0.00001);

            pp.SetupProperty(m => m.Priority, Project.PRIORITY_HIGH);
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1, 0.00001);
        }
    }

}
