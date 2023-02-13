using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Core.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text;

namespace NINA.Plugin.Assistant.Test.Database {

    [TestFixture]
    public class DbPlayTest {

        private AssistantDatabaseInteraction db;

        [SetUp]
        public void SetUp() {
            var testDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"assistantdb.sqlite");
            TestContext.WriteLine($"DB PATH: {testDbPath}");
            db = new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));
        }

        //[Test]
        public void GenTestDB() {
            string profileId = "3c160865-776f-4f72-8a05-5964225ca0fa";
            using (var context = db.GetContext()) {
                try {
                    Project p1 = new Project(profileId);
                    p1.Name = "M 42";
                    p1.Description = "test project 1";
                    p1.State = ProjectState.Active;
                    p1.ActiveDate = DateTime.Now;
                    p1.StartDate = DateTime.Now;
                    p1.EndDate = DateTime.Now.AddDays(100);

                    // TODO: set project pref fields
                    //p1Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    //p1.preferences = new ProjectPreferenceOLD(p1Prefs);

                    Target t1 = new Target();
                    t1.Name = "M 42";
                    t1.ra = AstroUtil.HMSToDegrees("5:35:17");
                    t1.dec = AstroUtil.DMSToDegrees("-5:23:28");
                    p1.Targets.Add(t1);

                    t1.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "Ha", Desired = 5 });
                    t1.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "OIII", Desired = 5 });
                    t1.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "SII", Desired = 5 });

                    context.ProjectSet.Add(p1);

                    ////

                    Project p2 = new Project(profileId);
                    p2.Name = "Sh2 240";
                    p2.Description = "test project 2";
                    p2.State = ProjectState.Active;
                    p2.ActiveDate = DateTime.Now;
                    p2.StartDate = DateTime.Now;
                    p2.EndDate = DateTime.Now.AddDays(100);

                    // TODO: set project pref fields
                    //p2Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    //p2.preferences = new ProjectPreferenceOLD(p2Prefs);

                    Target t2 = new Target();
                    t2.Name = "Sh2 240";
                    t2.ra = AstroUtil.HMSToDegrees("5:41:6");
                    t2.dec = AstroUtil.DMSToDegrees("28:5:0");
                    p2.Targets.Add(t2);

                    t2.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "R", Desired = 5 });
                    t2.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "G", Desired = 5 });
                    t2.FilterPlans.Add(new FilterPlan { ProfileId = profileId, FilterName = "B", Desired = 5 });

                    context.ProjectSet.Add(p2);

                    ///

                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "R"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "G"));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "B"));

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
                }
                catch (Exception e) {
                    TestContext.WriteLine($"OTHER EXCEPTION: {e.Message}\n{e.ToString()}");
                }
            }
        }

        //[Test]
        public void testPlay() {

            Logger.SetLogLevel(Core.Enum.LogLevelEnum.TRACE);
            string profileId1 = Guid.NewGuid().ToString();
            string profileId2 = Guid.NewGuid().ToString();

            using (var context = db.GetContext()) {
                try {
                    Project p = new Project(profileId1);
                    p.Name = "M 42";
                    p.Description = "first project";
                    p.State = ProjectState.Active;
                    p.ActiveDate = DateTime.Now.AddDays(1);
                    p.InactiveDate = DateTime.Now.AddDays(2);
                    p.StartDate = DateTime.Now.AddDays(3);
                    p.EndDate = DateTime.Now.AddDays(4);

                    Target t = new Target();
                    t.Name = "M 42: Frame 1";
                    t.ra = 4.56;
                    t.dec = -10.23;
                    p.Targets.Add(t);

                    FilterPlan ep1 = new FilterPlan();
                    ep1.FilterName = "Ha";
                    FilterPlan ep2 = new FilterPlan();
                    ep2.FilterName = "OIII";

                    t.FilterPlans.Add(ep1);
                    t.FilterPlans.Add(ep2);

                    t = new Target();
                    t.Name = "M 42: Frame 2";
                    t.ra = 4.78;
                    t.dec = -10.54;
                    p.Targets.Add(t);

                    ep1 = new FilterPlan();
                    ep1.FilterName = "Ha";
                    ep2 = new FilterPlan();
                    ep2.FilterName = "OIII";

                    t.FilterPlans.Add(ep1);
                    t.FilterPlans.Add(ep2);

                    context.ProjectSet.Add(p);

                    //AssistantFilterPreferences ap = new AssistantFilterPreferencesOLD();
                    //ap.MoonAvoidanceEnabled = true;
                    //ap.MoonAvoidanceSeparation = 55;
                    //ap.MoonAvoidanceWidth = 7;
                    FilterPreference fpref = new FilterPreference(Guid.NewGuid().ToString(), "Ha");
                    context.FilterPreferencePlanSet.Add(fpref);

                    p.EnableGrader = true;
                    p.MinimumAltitude = 22;
                    p.RuleWeights = new Dictionary<string, double> {
                        { "foo", 0.3 },
                        { "bar", 0.8 },
                        { "foo", 0.4 }
                    };

                    context.SaveChanges();

                    List<Project> projects = context.ProjectSet.Include("targets.filterplans").Include("preferences").ToList();
                    TestContext.WriteLine($"num projects: {projects.Count}");

                    foreach (Project project in projects) {
                        TestContext.WriteLine($"project: {project.Name}");
                        TestContext.WriteLine($"    {project.CreateDate}");
                        TestContext.WriteLine($"    {project.ActiveDate}");
                        TestContext.WriteLine($"    {project.InactiveDate}");
                        TestContext.WriteLine($"    {project.StartDate}");
                        TestContext.WriteLine($"    {project.EndDate}");

                        List<Target> targets = project.Targets;
                        TestContext.WriteLine($"num targets: {targets.Count}");

                        foreach (Target target in targets) {
                            TestContext.WriteLine($"     target: {target.Name}");
                            TestContext.WriteLine($"project pid: {target.Project.ProfileId}");
                            List<FilterPlan> filterPlans = target.FilterPlans;

                            foreach (FilterPlan filterPlan in filterPlans) {
                                TestContext.WriteLine($"     exp plan: {filterPlan.FilterName} {filterPlan.Exposure}");
                            }
                        }
                    }

                    List<FilterPreference> filterPreferences = context.FilterPreferencePlanSet.ToList();
                    foreach (FilterPreference preference in filterPreferences) {
                        TestContext.WriteLine($"filter pref:\n{preference}");
                    }

                    TestContext.WriteLine("----------------");
                    List<Project> pq = context.GetActiveProjects(profileId1, DateTime.Now.AddDays(3).AddHours(1));
                    TestContext.WriteLine($"pq for {profileId1}: {pq.Count}");
                    //pq = context.GetActiveProjects(profileId2, DateTime.Now);
                    //TestContext.WriteLine($"pq for {profileId2}: {pq.Count}");

                    pq = context.GetAllProjects(profileId1);
                    List<Target> targets2 = pq[0].Targets;
                    TestContext.WriteLine($"num targets: {targets2.Count}");

                    foreach (Target target in targets2) {
                        TestContext.WriteLine($"     target: {target.Name}");
                        TestContext.WriteLine($"project pid: {target.Project.ProfileId}");
                        List<FilterPlan> filterPlans = target.FilterPlans;

                        foreach (FilterPlan filterPlan in filterPlans) {
                            TestContext.WriteLine($"     exp plan: {filterPlan.FilterName} {filterPlan.Exposure}");
                        }
                    }

                }
                catch (DbEntityValidationException e) {
                    StringBuilder sb = new StringBuilder();
                    foreach (var eve in e.EntityValidationErrors) {
                        foreach (var dbeve in eve.ValidationErrors) {
                            sb.Append(dbeve.ErrorMessage).Append("\n");
                        }
                    }

                    TestContext.WriteLine($"DB VALIDATION EXCEPTION: {sb.ToString()}");
                }
                catch (Exception e) {
                    TestContext.WriteLine($"OTHER EXCEPTION: {e.Message}\n{e.ToString()}");
                }
            }
        }

    }
}
