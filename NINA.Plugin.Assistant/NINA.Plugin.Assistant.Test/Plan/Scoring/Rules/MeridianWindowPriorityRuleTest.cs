using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring.Rules {

    [TestFixture]
    public class MeridianWindowPriorityRuleTest {

        [Test]
        public void testMeridianWindowPriority() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = new Mock<IPlanTarget>().SetupAllProperties();
            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp", ProjectState.Active);
            targetMock.SetupProperty(m => m.Project, pp.Object);

            MeridianWindowPriorityRule sut = new MeridianWindowPriorityRule();

            pp.SetupProperty(m => m.MeridianWindow, 0);
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);

            pp.SetupProperty(m => m.MeridianWindow, 60);
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1, 0.00001);
        }
    }

}
