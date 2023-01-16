using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Plugin.Assistant.Test.Plan;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Text;

namespace NINA.Plugin.Assistant.Test.Database {

    [TestFixture]
    public class GenerateTestDatabase {

        private AssistantDatabaseInteraction db;

        [SetUp]
        public void SetUp() {
            db = GetDatabase();
        }

        [Test]
        //[Ignore("tbd")]
        public void TomTest1() {
            string profileId = "3c160865-776f-4f72-8a05-5964225ca0fa";
            using (var context = db.GetContext()) {
                try {
                    Project p1 = new Project(profileId);
                    p1.name = "Project: M42";
                    p1.description = "";
                    p1.state = Project.STATE_ACTIVE;
                    p1.ActiveDate = new DateTime(2022, 12, 1);
                    p1.StartDate = p1.ActiveDate;
                    p1.EndDate = new DateTime(2023, 2, 1);

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
                    fp.desired = 5;
                    t1.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.desired = 5;
                    t1.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.desired = 5;
                    t1.filterplans.Add(fp);

                    context.ProjectSet.Add(p1);

                    Project p2 = new Project(profileId);
                    p2.name = "Project: IC1805";
                    p2.description = "";
                    p2.state = Project.STATE_ACTIVE;
                    p2.ActiveDate = new DateTime(2022, 12, 1);
                    p2.StartDate = p2.ActiveDate;
                    p2.EndDate = new DateTime(2023, 2, 1);

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
                    fp.desired = 10;
                    t2.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "OIII");
                    fp.desired = 10;
                    t2.filterplans.Add(fp);
                    fp = new FilterPlan(profileId, "SII");
                    fp.desired = 10;
                    t2.filterplans.Add(fp);

                    context.ProjectSet.Add(p2);

                    var afp = new AssistantFilterPreferences();
                    afp.SetDefaults();
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII", afp));

                    context.SaveChanges();

                    ReadAndDump(profileId, new DateTime(2023, 1, 1));
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

        private AssistantDatabaseInteraction GetDatabase() {
            var testDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"assistantdb.sqlite");
            TestContext.WriteLine($"DB PATH: {testDbPath}");
            return new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));
        }

        private void SetDefaultRuleWeights(AssistantProjectPreferences prefs) {
            Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
            foreach (KeyValuePair<string, IScoringRule> entry in rules) {
                var rule = entry.Value;
                prefs.AddRuleWeight(rule.Name, rule.DefaultWeight);
            }
        }

        private List<IPlanProject> ReadAndDump(string profileId, DateTime forDateTime) {

            List<Project> projects = null;
            List<FilterPreference> filterPrefs = null;

            AssistantDatabaseInteraction database = GetDatabase();
            using (var context = database.GetContext()) {
                try {
                    projects = context.GetActiveProjects(profileId, forDateTime);
                    filterPrefs = context.GetFilterPreferences(profileId);
                }
                catch (Exception ex) {
                    TestContext.WriteLine($"Assistant: exception accessing Assistant: {ex}");
                }
            }

            if (projects == null || projects.Count == 0) {
                return null;
            }

            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            profileMock.SetupProperty(m => m.ActiveProfile.Id, new Guid(profileId));
            List<IPlanProject> planProjects = new List<IPlanProject>();

            Dictionary<string, AssistantFilterPreferences> dict = new Dictionary<string, AssistantFilterPreferences>();
            foreach (FilterPreference filterPref in filterPrefs) {
                dict.Add(filterPref.filterName, filterPref.Preferences);
            }
            Dictionary<string, AssistantFilterPreferences> filterPrefsDictionary = dict;

            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(profileMock.Object.ActiveProfile, project, filterPrefsDictionary);
                planProjects.Add(planProject);
                TestContext.WriteLine($"PROJECT:\n{planProject}");
            }

            return planProjects;
        }
    }
}
