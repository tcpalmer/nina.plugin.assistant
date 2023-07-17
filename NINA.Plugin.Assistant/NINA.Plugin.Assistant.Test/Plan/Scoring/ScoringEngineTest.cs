using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using FluentAssertions;
using Moq;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan.Scoring {

    [TestFixture]
    public class ScoringEngineTest {

        [Test]
        public void TestOneRule() {
            Mock<IProfile> profileMock = new Mock<IProfile>();

            Dictionary<string, double> ruleWeights = new Dictionary<string, double>();
            ruleWeights.Add("TestRule1", 75);
            List<IScoringRule> rules = new List<IScoringRule>();
            rules.Add(new TestRule("TestRule1", 0.5));

            ScoringEngine sut = new ScoringEngine(profileMock.Object, DateTime.Now, null);
            sut.RuleWeights = ruleWeights;
            sut.Rules = rules;

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            sut.ScoreTarget(pt.Object).Should().BeApproximately(0.375, 0.00001);
        }

        [Test]
        public void TestTwoRules() {
            Logger.SetLogLevel(LogLevelEnum.DEBUG);
            Mock<IProfile> profileMock = new Mock<IProfile>();

            Dictionary<string, double> ruleWeights = new Dictionary<string, double>();
            ruleWeights.Add("TestRule1", 100);
            ruleWeights.Add("TestRule2", 50);
            List<IScoringRule> rules = new List<IScoringRule>();
            rules.Add(new TestRule("TestRule1", 1));
            rules.Add(new TestRule("TestRule2", 0.5));

            ScoringEngine sut = new ScoringEngine(profileMock.Object, DateTime.Now, null);
            sut.RuleWeights = ruleWeights;
            sut.Rules = rules;

            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            sut.ScoreTarget(pt.Object).Should().BeApproximately(1.25, 0.00001);
        }
    }

    class TestRule : ScoringRule {
        private string name;
        private double score;

        public TestRule(string name, double score) {
            this.name = name;
            this.score = score;
        }

        public override string Name { get { return name; } }
        public override double DefaultWeight { get { return 1; } }

        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            return score;
        }
    }

}
