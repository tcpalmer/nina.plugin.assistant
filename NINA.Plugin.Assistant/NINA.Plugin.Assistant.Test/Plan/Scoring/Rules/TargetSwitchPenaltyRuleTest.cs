using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring.Rules {

    [TestFixture]
    public class TargetSwitchPenaltyRuleTest {

        [Test]
        public void testTargetSwitchPenalty1() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.Setup(m => m.Equals(It.IsAny<object>())).Returns(true);

            TargetSwitchPenaltyRule sut = new TargetSwitchPenaltyRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1, 0.00001);
        }

        [Test]
        public void testTargetSwitchPenalty0() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            targetMock.Setup(m => m.Equals(It.IsAny<object>())).Returns(false);

            TargetSwitchPenaltyRule sut = new TargetSwitchPenaltyRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);
        }
    }
}