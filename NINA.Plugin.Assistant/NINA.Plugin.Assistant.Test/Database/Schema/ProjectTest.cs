using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Database.Schema {

    [TestFixture]
    public class ProjectTest {

        [Test]
        public void TestClone() {
            DateTime onDate = DateTime.Now.Date;
            Project p1 = new Project("profileId");
            p1.Name = "p1N";
            p1.Description = "p1D";
            p1.ActiveDate = onDate;
            p1.StartDate = onDate.AddDays(1);
            p1.EndDate = onDate.AddDays(2);
            p1.Priority = ProjectPriority.High;
            p1.State = ProjectState.Inactive;
            p1.MinimumAltitude = 10;
            p1.MinimumTime = 90;
            p1.UseCustomHorizon = true;

            Dictionary<string, double> rw = new Dictionary<string, double>();
            rw.Add("a", 1);
            rw.Add("b", 2);
            p1.RuleWeights = rw;

            Project p2 = (Project)p1.Clone();

            p2.Name.Should().Be("p1N");
            p2.Description.Should().Be("p1D");
            p2.ActiveDate.Should().Be(onDate);
            p2.StartDate.Should().Be(onDate.AddDays(1));
            p2.EndDate.Should().Be(onDate.AddDays(2));
            p2.Priority.Should().Be(ProjectPriority.High);
            p2.State.Should().Be(ProjectState.Inactive);
            p2.MinimumAltitude.Should().Be(10);
            p2.MinimumTime.Should().Be(90);
            p2.UseCustomHorizon.Should().Be(true);
            p2.RuleWeights["a"].Should().Be(1);
            p2.RuleWeights["b"].Should().Be(2);
        }

    }
}
