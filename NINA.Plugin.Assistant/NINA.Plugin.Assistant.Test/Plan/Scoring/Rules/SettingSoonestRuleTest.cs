using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring.Rules {

    [TestFixture]
    public class SettingSoonestRuleTest {

        [Test]
        [TestCase(20, 0.6667)]
        [TestCase(21, 0.6250)]
        [TestCase(22, 0.5833)]
        [TestCase(23, 0.5416)]
        [TestCase(24, 0.5000)]
        [TestCase(25, 0.4583)]
        [TestCase(26, 0.4166)]
        [TestCase(27, 0.3750)]
        [TestCase(28, 0.3333)]
        [TestCase(29, 0.2916)]
        [TestCase(30, 0.2500)]
        public void testSettingSoonest(int hours, double expected) {
            Mock<IScoringEngine> scoringEngineMock = PlanMocks.GetMockScoringEnging();
            Mock<IPlanTarget> targetMock = PlanMocks.GetMockPlanTarget("", TestUtil.SPICA);

            targetMock.Object.EndTime = DateTime.Now.Date.AddHours(hours);
            SettingSoonestRule sut = new SettingSoonestRule();
            sut.Score(scoringEngineMock.Object, targetMock.Object).Should().BeApproximately(expected, 0.01);
        }
    }
}