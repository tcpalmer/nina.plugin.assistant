using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
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

        [Test]
        public void testIt() {

            Logger.SetLogLevel(Core.Enum.LogLevelEnum.TRACE);

            using (var context = db.GetContext()) {
                try {
                    Project p = new Project();
                    p.name = "M 42";
                    p.description = "first project";
                    p.profileId = Guid.NewGuid().ToString();

                    Target t = new Target();
                    t.name = "M 42: Frame 1";
                    t.ra = 4.56;
                    t.dec = -10.23;
                    p.targets.Add(t);

                    ExposurePlan ep1 = new ExposurePlan();
                    ep1.filtername = "Ha";
                    ep1.filterpos = 1;
                    ExposurePlan ep2 = new ExposurePlan();
                    ep2.filtername = "OIII";
                    ep2.filterpos = 2;

                    t.exposureplans.Add(ep1);
                    t.exposureplans.Add(ep2);

                    t = new Target();
                    t.name = "M 42: Frame 2";
                    t.ra = 4.78;
                    t.dec = -10.54;
                    p.targets.Add(t);

                    ep1 = new ExposurePlan();
                    ep1.filtername = "Ha";
                    ep1.filterpos = 1;
                    ep2 = new ExposurePlan();
                    ep2.filtername = "OIII";
                    ep2.filterpos = 2;

                    t.exposureplans.Add(ep1);
                    t.exposureplans.Add(ep2);

                    context.ProjectSet.Add(p);

                    AssistantPreferences ap = new AssistantPreferences();
                    ap.MoonAvoidanceEnabled = true;
                    ap.MoonAvoidanceSeparation = 55;
                    ap.MoonAvoidanceWidth = 7;
                    Preference pref = new Preference(Guid.NewGuid().ToString(), ap);
                    context.PreferencePlanSet.Add(pref);

                    // TODO: this isn't enforcing a unique constraint - should blow up

                    ap = new AssistantPreferences();
                    ap.MeridianWindowEnabled = true;
                    ap.MeridianWindowMinutes = 60;
                    pref = new Preference(Guid.NewGuid().ToString(), ap);
                    context.PreferencePlanSet.Add(pref);

                    context.SaveChanges();

                    /*
                     * Needed queries:
                     *    - all projects (no relations) sorted by name, optionally filtering on state
                     *      - for profileId only?
                     *      - active only
                     *      - all except state=CLOSED
                     *      - ...
                     *    - project by ID with all relations
                     *    - target by ID with all relations
                     */

                    List<Project> projects = context.ProjectSet.Include("targets.exposureplans").ToList();
                    TestContext.WriteLine($"num projects: {projects.Count}");

                    foreach (Project project in projects) {
                        TestContext.WriteLine($"project: {project.name} {AssistantDbContext.UnixSecondsToDateTime(project.createdate)}");

                        List<Target> targets = project.targets;
                        TestContext.WriteLine($"num targets: {targets.Count}");

                        foreach (Target target in targets) {
                            TestContext.WriteLine($"   target: {target.name}");
                            List<ExposurePlan> exposurePlans = target.exposureplans;

                            foreach (ExposurePlan exposurePlan in exposurePlans) {
                                TestContext.WriteLine($"     exp plan: {exposurePlan.filtername} {exposurePlan.exposure}");
                            }
                        }
                    }

                    List<Preference> preferences = context.PreferencePlanSet.ToList();
                    foreach (Preference preference in preferences) {
                        TestContext.WriteLine($"pref:\n{preference.preferences}");
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
