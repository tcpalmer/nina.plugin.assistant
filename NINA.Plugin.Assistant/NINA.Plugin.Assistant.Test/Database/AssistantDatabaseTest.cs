﻿using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using FluentAssertions;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Plugin.Assistant.Test.Plan;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;

namespace NINA.Plugin.Assistant.Test.Database {

    [TestFixture]
    public class AssistantDatabaseTest {

        private const string profileId = "01234567-abcd-9876-gfed-0123456abcde";
        private static DateTime markDate = DateTime.Now.Date;

        private string testDatabasePath;
        private AssistantDatabaseInteraction db;

        [OneTimeSetUp]
        public void OneTimeSetUp() {

            testDatabasePath = Path.Combine(Path.GetTempPath(), $"assistant-unittest.sqlite");
            if (File.Exists(testDatabasePath)) {
                File.Delete(testDatabasePath);
            }

            db = new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDatabasePath));
            NUnit.Framework.Assert.NotNull(db);
            LoadTestDatabase();
        }

        [Test, Order(1)]
        [NonParallelizable]
        public void TestLoad() {
            using (var context = db.GetContext()) {
                context.GetAllProjects("").Count.Should().Be(0);

                List<Project> projects = context.GetAllProjects(profileId);
                projects.Count.Should().Be(2);

                Project p1 = projects[0];
                p1.Name.Should().Be("Project: M42");
                p1.Targets.Count.Should().Be(1);

                p1.MinimumTime.Should().Be(60);
                p1.MinimumAltitude.Should().BeApproximately(23, 0.001);
                p1.UseCustomHorizon.Should().BeFalse();
                p1.HorizonOffset.Should().BeApproximately(11, 0.001);
                p1.FilterSwitchFrequency.Should().Be(12);
                p1.DitherEvery.Should().Be(14);
                p1.EnableGrader.Should().BeFalse();

                Target t1p1 = p1.Targets[0];
                t1p1.Name = "M42";
                t1p1.RA.Should().BeApproximately(83.82, 0.001);
                t1p1.Dec.Should().BeApproximately(-5.391, 0.001);
                t1p1.Rotation.Should().BeApproximately(0, 0.001);
                t1p1.ROI.Should().BeApproximately(1, 0.001);

                t1p1.FilterPlans.Count.Should().Be(3);
                t1p1.FilterPlans[0].FilterName.Should().Be("Ha");
                t1p1.FilterPlans[1].FilterName.Should().Be("OIII");
                t1p1.FilterPlans[2].FilterName.Should().Be("SII");

                Project p2 = projects[1];
                p2.Name.Should().Be("Project: IC1805");
                p2.Targets.Count.Should().Be(1);

                p2.MinimumTime.Should().Be(90);
                p2.MinimumAltitude.Should().BeApproximately(24, 0.001);
                p2.UseCustomHorizon.Should().BeTrue();
                p2.HorizonOffset.Should().BeApproximately(12, 0.001);
                p2.FilterSwitchFrequency.Should().Be(14);
                p2.DitherEvery.Should().Be(16);
                p2.EnableGrader.Should().BeFalse();

                Target t1p2 = p2.Targets[0];
                t1p2.Name = "IC1805";
                t1p2.RA.Should().BeApproximately(38.175, 0.001);
                t1p2.Dec.Should().BeApproximately(61.45, 0.001);
                t1p2.Rotation.Should().BeApproximately(0, 0.001);
                t1p2.ROI.Should().BeApproximately(1, 0.001);
                t1p2.FilterPlans.Count.Should().Be(3);
                t1p2.FilterPlans[0].FilterName.Should().Be("Ha");
                t1p2.FilterPlans[1].FilterName.Should().Be("OIII");
                t1p2.FilterPlans[2].FilterName.Should().Be("SII");

                context.GetFilterPreferences("").Count.Should().Be(0);
                List<FilterPreference> fPrefs = context.GetFilterPreferences(profileId);
                fPrefs.Count.Should().Be(3);
                fPrefs[0].FilterName.Should().Be("Ha");
                fPrefs[1].FilterName.Should().Be("OIII");
                fPrefs[2].FilterName.Should().Be("SII");
                fPrefs[0].MoonAvoidanceEnabled.Should().BeFalse();
                fPrefs[1].MoonAvoidanceEnabled.Should().BeFalse();
                fPrefs[2].MoonAvoidanceEnabled.Should().BeFalse();

                // Test GetActiveProjects
                projects = context.GetActiveProjects(profileId, markDate);
                projects.Count.Should().Be(1);
                p1 = projects[0];
                p1.Name.Should().Be("Project: M42");

                // Test GetFilterPlan
                AssertFilterPlan(context.GetFilterPlan(t1p1.Id, "Ha"), "Ha", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p1.Id, "OIII"), "OIII", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p1.Id, "SII"), "SII", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p2.Id, "Ha"), "Ha", 20, 5);
                AssertFilterPlan(context.GetFilterPlan(t1p2.Id, "OIII"), "OIII", 20, 5);
                AssertFilterPlan(context.GetFilterPlan(t1p2.Id, "SII"), "SII", 20, 5);
            }
        }

        [Test, Order(2)]
        [NonParallelizable]
        public void TestWriteAcquiredImage() {
            using (var context = db.GetContext()) {
                context.GetAcquiredImages(1, "Ha").Count.Should().Be(0);
                context.GetAcquiredImages(1, "OIII").Count.Should().Be(0);
                context.GetAcquiredImages(1, "SII").Count.Should().Be(0);
                context.GetAcquiredImages(1, "nada").Count.Should().Be(0);

                ImageSavedEventArgs msg = PlanMocks.GetImageSavedEventArgs(markDate.AddDays(1), "Ha");
                context.AcquiredImageSet.Add(new AcquiredImage(1, markDate.AddDays(1), "Ha", new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, markDate.AddDays(1).AddMinutes(1), "Ha", new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, markDate.AddDays(1).AddMinutes(2), "Ha", new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, markDate.AddDays(1).AddMinutes(3), "Ha", new ImageMetadata(msg)));
                context.SaveChanges();

                List<AcquiredImage> ai = context.GetAcquiredImages(1, "Ha");
                ai.Count.Should().Be(4);

                // Confirm descending order
                ai[0].AcquiredDate.Should().BeExactly(markDate.AddDays(1).AddMinutes(3).TimeOfDay);
                ai[1].AcquiredDate.Should().BeExactly(markDate.AddDays(1).AddMinutes(2).TimeOfDay);
                ai[2].AcquiredDate.Should().BeExactly(markDate.AddDays(1).AddMinutes(1).TimeOfDay);
                ai[3].AcquiredDate.Should().BeExactly(markDate.AddDays(1).AddMinutes(0).TimeOfDay);
                ai[0].Metadata.ExposureStartTime.Should().BeExactly(markDate.AddDays(1).AddMinutes(3).TimeOfDay);
                ai[1].Metadata.ExposureStartTime.Should().BeExactly(markDate.AddDays(1).AddMinutes(2).TimeOfDay);
                ai[2].Metadata.ExposureStartTime.Should().BeExactly(markDate.AddDays(1).AddMinutes(1).TimeOfDay);
                ai[3].Metadata.ExposureStartTime.Should().BeExactly(markDate.AddDays(1).AddMinutes(0).TimeOfDay);
            }
        }

        [Test, Order(3)]
        [NonParallelizable]
        public void TestWriteUpdateFilterPlans() {
            using (var context = db.GetContext()) {
                Target target = context.GetTarget(1, 1);
                FilterPlan fp = target.FilterPlans.Where(t => t.FilterName == "Ha").First();
                fp.Acquired += 2;
                fp.Accepted += 1;
                context.SaveChanges();
            }

            using (var context = db.GetContext()) {
                Target target = context.GetTarget(1, 1);
                FilterPlan fp = target.FilterPlans.Where(t => t.FilterName == "Ha").First();
                fp.Desired.Should().Be(3);
                fp.Acquired.Should().Be(2);
                fp.Accepted.Should().Be(1);
            }
        }

        private void AssertFilterPlan(FilterPlan filterPlan, string filterName, int exp, int desired) {
            filterPlan.FilterName.Should().Be(filterName);
            filterPlan.Exposure.Should().Be(exp);
            filterPlan.Desired.Should().Be(desired);
            filterPlan.Acquired.Should().Be(0);
            filterPlan.Accepted.Should().Be(0);
            filterPlan.Gain.Should().Be(100);
            filterPlan.Offset.Should().Be(10);
            filterPlan.bin.Should().Be(1);
            filterPlan.ReadoutMode.Should().Be(-1);
        }

        private void LoadTestDatabase() {
            using (var context = db.GetContext()) {
                try {
                    Project p1 = new Project(profileId);
                    p1.Name = "Project: M42";
                    p1.Description = "";
                    p1.State = ProjectState.Active;
                    p1.ActiveDate = markDate;
                    p1.StartDate = markDate;
                    p1.EndDate = markDate.AddDays(10);
                    p1.MinimumTime = 60;
                    p1.MinimumAltitude = 23;
                    p1.UseCustomHorizon = false;
                    p1.HorizonOffset = 11;
                    p1.FilterSwitchFrequency = 12;
                    p1.DitherEvery = 14;
                    p1.EnableGrader = false;

                    Target t1 = new Target();
                    t1.Name = "M42";
                    t1.RA = TestUtil.M42.RADegrees;
                    t1.Dec = TestUtil.M42.Dec;
                    p1.Targets.Add(t1);

                    FilterPlan fp = new FilterPlan(profileId, "Ha");
                    fp.Desired = 3;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t1.FilterPlans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.Desired = 3;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t1.FilterPlans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.Desired = 3;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t1.FilterPlans.Add(fp);

                    context.ProjectSet.Add(p1);

                    Project p2 = new Project(profileId);
                    p2.Name = "Project: IC1805";
                    p2.Description = "";
                    p2.State = ProjectState.Active;
                    p2.ActiveDate = markDate;
                    p2.StartDate = markDate.AddDays(10);
                    p2.EndDate = markDate.AddDays(20);
                    p2.MinimumTime = 90;
                    p2.MinimumAltitude = 24;
                    p2.UseCustomHorizon = true;
                    p2.HorizonOffset = 12;
                    p2.FilterSwitchFrequency = 14;
                    p2.DitherEvery = 16;
                    p2.EnableGrader = false;

                    Target t2 = new Target();
                    t2.Name = "IC1805";
                    t2.RA = TestUtil.IC1805.RADegrees;
                    t2.Dec = TestUtil.IC1805.Dec;
                    p2.Targets.Add(t2);

                    fp = new FilterPlan(profileId, "Ha");
                    fp.Desired = 5;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t2.FilterPlans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.Desired = 5;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t2.FilterPlans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.Desired = 5;
                    fp.Exposure = 20;
                    fp.Gain = 100;
                    fp.Offset = 10;
                    t2.FilterPlans.Add(fp);

                    context.ProjectSet.Add(p2);

                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII"));

                    context.SaveChanges();
                }
                catch (DbEntityValidationException e) {
                    StringBuilder sb = new StringBuilder();
                    foreach (var eve in e.EntityValidationErrors) {
                        foreach (var dbeve in eve.ValidationErrors) {
                            sb.Append(dbeve.ErrorMessage).Append("\n");
                        }
                    }

                    TestContext.WriteLine($"DB VALIDATION EXCEPTION: {sb.ToString()}");
                    throw e;
                }
                catch (Exception e) {
                    TestContext.WriteLine($"OTHER EXCEPTION: {e.Message}\n{e.ToString()}");
                    throw e;
                }
            }

        }

        /*
        private void SetDefaultRuleWeights(AssistantProjectPreferences prefs) {
            Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
            foreach (KeyValuePair<string, IScoringRule> entry in rules) {
                var rule = entry.Value;
                prefs.AddRuleWeight(rule.Name, rule.DefaultWeight);
            }
        }*/
    }

}
