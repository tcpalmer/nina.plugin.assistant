using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class PerfectPlanTest {

        [Test, Order(1)]
        [NonParallelizable]
        public void testPerfectPlan() {
            //Logger.SetLogLevel(LogLevelEnum.DEBUG);

            Mock<IProfileService> profileMock = PlanMocks.GetMockProfileService(TestUtil.TEST_LOCATION_4);
            DateTime atTime = new DateTime(2023, 1, 26);

            List<IPlanProject> projects = LoadFromDatabase(profileMock.Object, atTime, 1);
            List<AssistantPlan> plans = Planner.GetPerfectPlan(atTime, profileMock.Object, projects);
            foreach (AssistantPlan plan in plans) {
                TestContext.WriteLine("PLAN -----------------------------------------------------");
                TestContext.WriteLine(plan.PlanSummary());
            }
        }

        private List<IPlanProject> LoadFromDatabase(IProfileService profileService, DateTime atTime, int testDatabase) {
            AssistantDatabaseInteraction db = InitDatabase();

            switch (testDatabase) {
                case 1:
                    LoadTestDatabaseOne(db, profileService, atTime);
                    break;
            }

            return new AssistantPlanLoader().LoadActiveProjects(db.GetContext(), profileService.ActiveProfile, atTime);
        }

        private AssistantDatabaseInteraction InitDatabase() {
            string testDatabasePath = Path.Combine(Path.GetTempPath(), $"assistant-unittest.sqlite");
            if (File.Exists(testDatabasePath)) {
                File.Delete(testDatabasePath);
            }

            return new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDatabasePath));
        }

        private void LoadTestDatabaseOne(AssistantDatabaseInteraction db, IProfileService profileService, DateTime atTime) {
            using (var context = db.GetContext()) {
                try {
                    string profileId = profileService.ActiveProfile.Id.ToString();

                    /*
                     * C00: just a few images needed and wrap quickly
                     * What's behavior to get to next target?
                     * Wait a bit (testing stop tracking)
                     * C01:  
                     */

                    Project p1 = new Project(profileId);
                    p1.name = "Project: C00";
                    p1.description = "";
                    p1.state = (int)ProjectState.Active;
                    p1.ActiveDate = atTime.AddDays(-1);
                    p1.StartDate = atTime;
                    p1.EndDate = atTime.AddDays(100);

                    AssistantProjectPreferences p1Prefs = new AssistantProjectPreferences();
                    p1Prefs.SetDefaults();
                    p1Prefs.FilterSwitchFrequency = 0;
                    p1Prefs.MinimumAltitude = 0;
                    SetDefaultRuleWeights(p1Prefs);
                    p1.preferences = new ProjectPreference(p1Prefs);

                    Target t1 = new Target();
                    t1.name = "C00";
                    t1.ra = TestUtil.C00.RA;
                    t1.dec = TestUtil.C00.Dec;
                    p1.targets.Add(t1);

                    t1.filterplans.Add(GetFilterPlan(profileId, "Lum", 5, 0, 60));
                    t1.filterplans.Add(GetFilterPlan(profileId, "Red", 5, 0, 60));
                    t1.filterplans.Add(GetFilterPlan(profileId, "Green", 5, 0, 60));
                    t1.filterplans.Add(GetFilterPlan(profileId, "Blue", 5, 0, 60));

                    Project p2 = new Project(profileId);
                    p2.name = "Project: C90";
                    p2.description = "";
                    p2.state = (int)ProjectState.Active;
                    p2.ActiveDate = atTime.AddDays(-1);
                    p2.StartDate = atTime;
                    p2.EndDate = atTime.AddDays(100);

                    AssistantProjectPreferences p2Prefs = new AssistantProjectPreferences();
                    p2Prefs.SetDefaults();
                    p2Prefs.FilterSwitchFrequency = 0;
                    p2Prefs.MinimumAltitude = 0;
                    SetDefaultRuleWeights(p2Prefs);
                    p2.preferences = new ProjectPreference(p2Prefs);

                    Target t2 = new Target();
                    t2.name = "C90";
                    t2.ra = TestUtil.C90.RA;
                    t2.dec = TestUtil.C90.Dec;
                    p2.targets.Add(t2);

                    t2.filterplans.Add(GetFilterPlan(profileId, "Ha", 5, 0, 90));
                    t2.filterplans.Add(GetFilterPlan(profileId, "OIII", 5, 0, 90));
                    t2.filterplans.Add(GetFilterPlan(profileId, "SII", 5, 0, 90));

                    context.ProjectSet.Add(p1);
                    context.ProjectSet.Add(p2);

                    var afp = new AssistantFilterPreferences();
                    afp.SetDefaults();
                    afp.TwilightLevel = TwilightLevel.Nautical;
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Lum", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Red", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Green", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Blue", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII", afp));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII", afp));

                    context.SaveChanges();
                }
                catch (Exception e) {
                    TestContext.Error.WriteLine($"failed to create test database: {e.Message}\n{e.ToString()}");
                    throw e;
                }
            }
        }

        private FilterPlan GetFilterPlan(string profileId, string filterName, int desired, int accepted, int exposure) {
            FilterPlan fp = new FilterPlan(profileId, filterName);
            fp.desired = desired;
            fp.accepted = accepted;
            fp.exposure = exposure;
            fp.gain = 100;
            fp.offset = 10;
            return fp;
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
