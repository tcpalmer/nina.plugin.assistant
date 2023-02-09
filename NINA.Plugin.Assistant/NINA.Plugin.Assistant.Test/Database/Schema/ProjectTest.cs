using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;
using System;

namespace NINA.Plugin.Assistant.Test.Database.Schema {

    [TestFixture]
    public class ProjectTest {

        [Test]
        public void TestClone() {
            DateTime onDate = DateTime.Now.Date;
            Project p1 = new Project("profileId");
            p1.name = "p1N";
            p1.description = "p1D";
            p1.ActiveDate = onDate;
            p1.StartDate = onDate.AddDays(1);
            p1.EndDate = onDate.AddDays(2);
            p1.Priority = ProjectPriority.High;
            p1.State = ProjectState.Inactive;
            AssistantProjectPreferences p1Prefs = new AssistantProjectPreferences();
            p1Prefs.SetDefaults();
            p1Prefs.MinimumAltitude = 10;
            p1Prefs.MinimumTime = 90;
            p1Prefs.UseCustomHorizon = true;
            p1Prefs.AddRuleWeight("a", 1);
            p1Prefs.AddRuleWeight("b", 2);
            p1.preferences = new ProjectPreference(p1Prefs);

            Project p2 = (Project)p1.Clone();

            p2.name.Should().Be("p1N");
            p2.description.Should().Be("p1D");
            p2.ActiveDate.Should().Be(onDate);
            p2.StartDate.Should().Be(onDate.AddDays(1));
            p2.EndDate.Should().Be(onDate.AddDays(2));
            p2.Priority.Should().Be(ProjectPriority.High);
            p2.State.Should().Be(ProjectState.Inactive);
            p2.ProjectPreferences.MinimumAltitude.Should().Be(10);
            p2.ProjectPreferences.MinimumTime.Should().Be(90);
            p2.ProjectPreferences.UseCustomHorizon.Should().Be(true);
            p2.ProjectPreferences.RuleWeights["a"].Should().Be(1);
            p2.ProjectPreferences.RuleWeights["b"].Should().Be(2);

        }

    }
}
