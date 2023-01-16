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
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            Mock<IPlanFilter> filterPlanMock = PlanMocks.GetMockPlanFilter("", 10, 0);
            PlanMocks.AddMockPlanFilter(targetMock, filterPlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0, 0.00001);
        }

        [Test]
        public void testPercentComplete60() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            Mock<IPlanFilter> filterPlanMock = PlanMocks.GetMockPlanFilter("", 10, 6);
            PlanMocks.AddMockPlanFilter(targetMock, filterPlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(0.6, 0.00001);
        }

        [Test]
        public void testPercentComplete100() {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);
            Mock<IPlanFilter> filterPlanMock = PlanMocks.GetMockPlanFilter("", 10, 10);
            PlanMocks.AddMockPlanFilter(targetMock, filterPlanMock);

            PercentCompleteRule sut = new PercentCompleteRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(1.0, 0.00001);
        }
    }

}
