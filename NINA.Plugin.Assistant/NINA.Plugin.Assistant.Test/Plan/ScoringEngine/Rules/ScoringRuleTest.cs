using Assistant.NINAPlugin.Plan.ScoringEngine.Rules;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan.ScoringEngine.Rules {

    [TestFixture]
    public class ScoringRuleTest {

        [Test]
        public void testGetAllScoringRules() {
            Dictionary<string, ScoringRule> list = ScoringRule.GetAllScoringRules();
            list.Should().NotBeEmpty();
            list.Should().ContainKey(ProjectPriorityRule.RULE_NAME);
            list[ProjectPriorityRule.RULE_NAME].Name.Should().Be(ProjectPriorityRule.RULE_NAME);
        }
    }

}
