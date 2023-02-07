using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
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
                    p1.name = "M 42";
                    p1.description = "test project 1";
                    p1.state = (int)ProjectState.Active;
                    p1.ActiveDate = DateTime.Now;
                    p1.StartDate = DateTime.Now;
                    p1.EndDate = DateTime.Now.AddDays(100);

                    AssistantProjectPreferences p1Prefs = new AssistantProjectPreferences {
                        MinimumAltitude = 20,
                        MinimumTime = 30,
                    };

                    p1Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    p1.preferences = new ProjectPreference(p1Prefs);

                    Target t1 = new Target();
                    t1.name = "M 42";
                    t1.ra = AstroUtil.HMSToDegrees("5:35:17");
                    t1.dec = AstroUtil.DMSToDegrees("-5:23:28");
                    p1.targets.Add(t1);

                    t1.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "Ha", desired = 5 });
                    t1.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "OIII", desired = 5 });
                    t1.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "SII", desired = 5 });

                    context.ProjectSet.Add(p1);

                    ////

                    Project p2 = new Project(profileId);
                    p2.name = "Sh2 240";
                    p2.description = "test project 2";
                    p2.state = (int)ProjectState.Active;
                    p2.ActiveDate = DateTime.Now;
                    p2.StartDate = DateTime.Now;
                    p2.EndDate = DateTime.Now.AddDays(100);

                    AssistantProjectPreferences p2Prefs = new AssistantProjectPreferences {
                        MinimumAltitude = 20,
                        MinimumTime = 30,
                    };

                    p2Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    p2.preferences = new ProjectPreference(p2Prefs);

                    Target t2 = new Target();
                    t2.name = "Sh2 240";
                    t2.ra = AstroUtil.HMSToDegrees("5:41:6");
                    t2.dec = AstroUtil.DMSToDegrees("28:5:0");
                    p2.targets.Add(t2);

                    t2.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "R", desired = 5 });
                    t2.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "G", desired = 5 });
                    t2.filterplans.Add(new FilterPlan { profileId = profileId, filterName = "B", desired = 5 });

                    context.ProjectSet.Add(p2);

                    ///

                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "Ha", new AssistantFilterPreferences()));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "OIII", new AssistantFilterPreferences()));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "SII", new AssistantFilterPreferences()));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "R", new AssistantFilterPreferences()));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "G", new AssistantFilterPreferences()));
                    context.FilterPreferencePlanSet.Add(new FilterPreference(profileId, "B", new AssistantFilterPreferences()));

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
                    p.name = "M 42";
                    p.description = "first project";
                    p.state = (int)ProjectState.Active;
                    p.ActiveDate = DateTime.Now.AddDays(1);
                    p.InactiveDate = DateTime.Now.AddDays(2);
                    p.StartDate = DateTime.Now.AddDays(3);
                    p.EndDate = DateTime.Now.AddDays(4);

                    Target t = new Target();
                    t.name = "M 42: Frame 1";
                    t.ra = 4.56;
                    t.dec = -10.23;
                    p.targets.Add(t);

                    FilterPlan ep1 = new FilterPlan();
                    ep1.filterName = "Ha";
                    FilterPlan ep2 = new FilterPlan();
                    ep2.filterName = "OIII";

                    t.filterplans.Add(ep1);
                    t.filterplans.Add(ep2);

                    t = new Target();
                    t.name = "M 42: Frame 2";
                    t.ra = 4.78;
                    t.dec = -10.54;
                    p.targets.Add(t);

                    ep1 = new FilterPlan();
                    ep1.filterName = "Ha";
                    ep2 = new FilterPlan();
                    ep2.filterName = "OIII";

                    t.filterplans.Add(ep1);
                    t.filterplans.Add(ep2);

                    context.ProjectSet.Add(p);

                    AssistantFilterPreferences ap = new AssistantFilterPreferences();
                    ap.MoonAvoidanceEnabled = true;
                    ap.MoonAvoidanceSeparation = 55;
                    ap.MoonAvoidanceWidth = 7;
                    FilterPreference fpref = new FilterPreference(Guid.NewGuid().ToString(), "Ha", ap);
                    context.FilterPreferencePlanSet.Add(fpref);

                    AssistantProjectPreferences pp = new AssistantProjectPreferences();
                    pp.EnableGrader = true;
                    pp.MinimumAltitude = 22;
                    pp.AddRuleWeight("foo", 0.3);
                    pp.AddRuleWeight("bar", 0.8);
                    pp.AddRuleWeight("foo", 0.4);
                    ProjectPreference ppref = new ProjectPreference(pp);
                    p.preferences = ppref;

                    context.SaveChanges();

                    List<Project> projects = context.ProjectSet.Include("targets.filterplans").Include("preferences").ToList();
                    TestContext.WriteLine($"num projects: {projects.Count}");

                    foreach (Project project in projects) {
                        TestContext.WriteLine($"project: {project.name}");
                        TestContext.WriteLine($"    {project.CreateDate}");
                        TestContext.WriteLine($"    {project.ActiveDate}");
                        TestContext.WriteLine($"    {project.InactiveDate}");
                        TestContext.WriteLine($"    {project.StartDate}");
                        TestContext.WriteLine($"    {project.EndDate}");

                        TestContext.WriteLine($"  prefs:\n{project.ProjectPreferences}");

                        List<Target> targets = project.targets;
                        TestContext.WriteLine($"num targets: {targets.Count}");

                        foreach (Target target in targets) {
                            TestContext.WriteLine($"     target: {target.name}");
                            TestContext.WriteLine($"project pid: {target.project.profileid}");
                            List<FilterPlan> filterPlans = target.filterplans;

                            foreach (FilterPlan filterPlan in filterPlans) {
                                TestContext.WriteLine($"     exp plan: {filterPlan.filterName} {filterPlan.exposure}");
                            }
                        }
                    }

                    List<FilterPreference> filterPreferences = context.FilterPreferencePlanSet.ToList();
                    foreach (FilterPreference preference in filterPreferences) {
                        TestContext.WriteLine($"filter pref:\n{preference.Preferences}");
                    }

                    TestContext.WriteLine("----------------");
                    List<Project> pq = context.GetActiveProjects(profileId1, DateTime.Now.AddDays(3).AddHours(1));
                    TestContext.WriteLine($"pq for {profileId1}: {pq.Count}");
                    //pq = context.GetActiveProjects(profileId2, DateTime.Now);
                    //TestContext.WriteLine($"pq for {profileId2}: {pq.Count}");

                    pq = context.GetAllProjects(profileId1);
                    TestContext.WriteLine($"prefs: {pq[0].ProjectPreferences}");
                    List<Target> targets2 = pq[0].targets;
                    TestContext.WriteLine($"num targets: {targets2.Count}");

                    foreach (Target target in targets2) {
                        TestContext.WriteLine($"     target: {target.name}");
                        TestContext.WriteLine($"project pid: {target.project.profileid}");
                        List<FilterPlan> filterPlans = target.filterplans;

                        foreach (FilterPlan filterPlan in filterPlans) {
                            TestContext.WriteLine($"     exp plan: {filterPlan.filterName} {filterPlan.exposure}");
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
