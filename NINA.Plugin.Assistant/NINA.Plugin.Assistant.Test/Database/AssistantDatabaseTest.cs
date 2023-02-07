using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
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
            Assert.NotNull(db);
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
                p1.name.Should().Be("Project: M42");
                p1.targets.Count.Should().Be(1);
                ProjectPreference p1p = p1.preferences;
                p1p.Should().NotBeNull();
                p1p.Preferences.Should().NotBeNull();
                p1p.Preferences.MinimumAltitude.Should().BeApproximately(10, 0.001);
                Target t1p1 = p1.targets[0];
                t1p1.name = "M42";
                t1p1.ra.Should().BeApproximately(83.82, 0.001);
                t1p1.dec.Should().BeApproximately(-5.391, 0.001);
                t1p1.rotation.Should().BeApproximately(0, 0.001);
                t1p1.roi.Should().BeApproximately(1, 0.001);

                t1p1.filterplans.Count.Should().Be(3);
                t1p1.filterplans[0].filterName.Should().Be("Ha");
                t1p1.filterplans[1].filterName.Should().Be("OIII");
                t1p1.filterplans[2].filterName.Should().Be("SII");

                Project p2 = projects[1];
                p2.name.Should().Be("Project: IC1805");
                p2.targets.Count.Should().Be(1);
                ProjectPreference p2p = p2.preferences;
                p2p.Should().NotBeNull();
                p2p.Preferences.Should().NotBeNull();
                p2p.Preferences.MinimumAltitude.Should().BeApproximately(10, 0.001);

                Target t1p2 = p2.targets[0];
                t1p2.name = "IC1805";
                t1p2.ra.Should().BeApproximately(38.175, 0.001);
                t1p2.dec.Should().BeApproximately(61.45, 0.001);
                t1p2.rotation.Should().BeApproximately(0, 0.001);
                t1p2.roi.Should().BeApproximately(1, 0.001);
                t1p2.filterplans.Count.Should().Be(3);
                t1p2.filterplans[0].filterName.Should().Be("Ha");
                t1p2.filterplans[1].filterName.Should().Be("OIII");
                t1p2.filterplans[2].filterName.Should().Be("SII");

                context.GetFilterPreferences("").Count.Should().Be(0);
                List<FilterPreference> fPrefs = context.GetFilterPreferences(profileId);
                fPrefs.Count.Should().Be(3);
                fPrefs[0].filterName.Should().Be("Ha");
                fPrefs[1].filterName.Should().Be("OIII");
                fPrefs[2].filterName.Should().Be("SII");
                fPrefs[0].Preferences.MoonAvoidanceEnabled.Should().BeFalse();
                fPrefs[1].Preferences.MoonAvoidanceEnabled.Should().BeFalse();
                fPrefs[2].Preferences.MoonAvoidanceEnabled.Should().BeFalse();

                // Test GetActiveProjects
                projects = context.GetActiveProjects(profileId, markDate);
                projects.Count.Should().Be(1);
                p1 = projects[0];
                p1.name.Should().Be("Project: M42");

                // Test GetFilterPlan
                AssertFilterPlan(context.GetFilterPlan(t1p1.id, "Ha"), "Ha", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p1.id, "OIII"), "OIII", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p1.id, "SII"), "SII", 20, 3);
                AssertFilterPlan(context.GetFilterPlan(t1p2.id, "Ha"), "Ha", 20, 5);
                AssertFilterPlan(context.GetFilterPlan(t1p2.id, "OIII"), "OIII", 20, 5);
                AssertFilterPlan(context.GetFilterPlan(t1p2.id, "SII"), "SII", 20, 5);
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
                FilterPlan fp = target.filterplans.Where(t => t.filterName == "Ha").First();
                fp.acquired += 2;
                fp.accepted += 1;
                context.SaveChanges();
            }

            using (var context = db.GetContext()) {
                Target target = context.GetTarget(1, 1);
                FilterPlan fp = target.filterplans.Where(t => t.filterName == "Ha").First();
                fp.desired.Should().Be(3);
                fp.acquired.Should().Be(2);
                fp.accepted.Should().Be(1);
            }
        }

        private void AssertFilterPlan(FilterPlan filterPlan, string filterName, int exp, int desired) {
            filterPlan.filterName.Should().Be(filterName);
            filterPlan.exposure.Should().Be(exp);
            filterPlan.desired.Should().Be(desired);
            filterPlan.acquired.Should().Be(0);
            filterPlan.accepted.Should().Be(0);
            filterPlan.gain.Should().Be(100);
            filterPlan.offset.Should().Be(10);
            filterPlan.bin.Should().Be(1);
            filterPlan.readoutMode.Should().Be(-1);
        }

        private void LoadTestDatabase() {
            using (var context = db.GetContext()) {
                try {
                    Project p1 = new Project(profileId);
                    p1.name = "Project: M42";
                    p1.description = "";
                    p1.state = (int)ProjectState.Active;
                    p1.ActiveDate = markDate;
                    p1.StartDate = markDate;
                    p1.EndDate = markDate.AddDays(10);

                    AssistantProjectPreferences p1Prefs = new AssistantProjectPreferences();
                    p1Prefs.SetDefaults();
                    p1Prefs.MinimumAltitude = 10;
                    SetDefaultRuleWeights(p1Prefs);
                    p1.preferences = new ProjectPreference(p1Prefs);

                    Target t1 = new Target();
                    t1.name = "M42";
                    t1.ra = TestUtil.M42.RADegrees;
                    t1.dec = TestUtil.M42.Dec;
                    p1.targets.Add(t1);

                    FilterPlan fp = new FilterPlan(profileId, "Ha");
                    fp.desired = 3;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t1.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.desired = 3;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t1.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.desired = 3;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t1.filterplans.Add(fp);

                    context.ProjectSet.Add(p1);

                    Project p2 = new Project(profileId);
                    p2.name = "Project: IC1805";
                    p2.description = "";
                    p2.state = (int)ProjectState.Active;
                    p2.ActiveDate = markDate;
                    p2.StartDate = markDate.AddDays(10);
                    p2.EndDate = markDate.AddDays(20);

                    AssistantProjectPreferences p2Prefs = new AssistantProjectPreferences();
                    p2Prefs.SetDefaults();
                    p2Prefs.MinimumAltitude = 10;
                    SetDefaultRuleWeights(p2Prefs);
                    p2.preferences = new ProjectPreference(p2Prefs);

                    Target t2 = new Target();
                    t2.name = "IC1805";
                    t2.ra = TestUtil.IC1805.RADegrees;
                    t2.dec = TestUtil.IC1805.Dec;
                    p2.targets.Add(t2);

                    fp = new FilterPlan(profileId, "Ha");
                    fp.desired = 5;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t2.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.desired = 5;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t2.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.desired = 5;
                    fp.exposure = 20;
                    fp.gain = 100;
                    fp.offset = 10;
                    t2.filterplans.Add(fp);

                    context.ProjectSet.Add(p2);

                    var afp = new AssistantFilterPreferences();
                    afp.SetDefaults();
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII", afp));

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

        private void SetDefaultRuleWeights(AssistantProjectPreferences prefs) {
            Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
            foreach (KeyValuePair<string, IScoringRule> entry in rules) {
                var rule = entry.Value;
                prefs.AddRuleWeight(rule.Name, rule.DefaultWeight);
            }
        }
    }

}
