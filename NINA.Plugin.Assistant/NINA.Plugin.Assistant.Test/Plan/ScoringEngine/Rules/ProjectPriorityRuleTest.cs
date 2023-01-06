using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.ScoringEngine.Rules;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Plan.ScoringEngine.Rules {

    [TestFixture]
    public class ProjectPriorityRuleTest {

        private Mock<PlanTarget> targetMock = new Mock<PlanTarget>();

        [SetUp]
        public void SetUp() {
            targetMock.Reset();
        }

        [Test]
        public void testProjectPriority() {
            ProjectPriorityRule sut = new ProjectPriorityRule();

            targetMock.SetupProperty(m => m.Project.Priority, Project.PRIORITY_LOW);
            sut.Score(DateTime.Now, targetMock.Object).Should().BeApproximately(0, 0.00001);

            targetMock.SetupProperty(m => m.Project.Priority, Project.PRIORITY_NORMAL);
            sut.Score(DateTime.Now, targetMock.Object).Should().BeApproximately(0.5, 0.00001);

            targetMock.SetupProperty(m => m.Project.Priority, Project.PRIORITY_HIGH);
            sut.Score(DateTime.Now, targetMock.Object).Should().BeApproximately(1, 0.00001);
        }
    }

}
