using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Astrometry;
using NINA.Plugin.Assistant.Shared.Utility;
using NUnit.Framework;
using System;
using System.Data.Entity.Validation;
using System.Text;

namespace NINA.Plugin.Assistant.Test.Database {

    [TestFixture]
    public class DbPlayTest {

        private SchedulerDatabaseInteraction db;

        /*
        [SetUp]
        public void SetUp() {
            var testDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"assistantdb.sqlite");
            TestContext.WriteLine($"DB PATH: {testDbPath}");
            db = new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));
        }*/

        //[Test]
        public void AddAcquiredImagesForTSFlats() {

            // BEWARE! THIS WILL UPDATE ACTUAL DATABASE USED BY THE PLUGIN
            var testDbPath = @"C:\Users\Tom\AppData\Local\NINA\SchedulerPlugin\schedulerdb.sqlite";
            db = new SchedulerDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));

            DateTime dt = DateTime.Now.Date.AddDays(-5).AddHours(18);
            //int gain = 139;
            //int offset = 21;
            int gain = -1;
            int offset = -1;
            double rotation = ImageMetadata.NO_ROTATOR_ANGLE;
            string binning = "1x1";

            string profileId = "c0e1645f-4d4c-4cff-b6f8-c66a58be9cd4";
            //string profileId = "4033c406-2709-488c-bf12-5c9387302c05"; // TS Flats Test on astropc

            using (SchedulerDatabaseContext context = db.GetContext()) {
                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 1, 1, profileId, GetImageMetadata("Lum", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 1, 1, profileId, GetImageMetadata("Red", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 1, 1, profileId, GetImageMetadata("Green", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 2, 2, profileId, GetImageMetadata("Lum", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 2, 2, profileId, GetImageMetadata("Red", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 2, 2, profileId, GetImageMetadata("Green", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                for (int i = 0; i < 5; i++) {
                    dt = dt.AddMinutes(i * 3);
                    AcquiredImage ai = GetAcquiredImage(dt, 2, 2, profileId, GetImageMetadata("Blue", gain, offset, binning, 0, rotation, 100));
                    context.AcquiredImageSet.Add(ai);
                }

                context.SaveChanges();
            }
        }

        private ImageMetadata GetImageMetadata(string filterName, int gain, int offset, string binning, int readoutMode, double rotation, double roi) {
            return new ImageMetadata() {
                SessionId = 23,
                FilterName = filterName,
                Gain = gain,
                Offset = offset,
                Binning = binning,
                ReadoutMode = readoutMode,
                RotatorPosition = rotation,
                RotatorMechanicalPosition = rotation,
                ROI = roi
            };
        }

        private AcquiredImage GetAcquiredImage(DateTime dt, int projectId, int targetId, string profileId, ImageMetadata metadata) {
            return new AcquiredImage(metadata) {
                AcquiredDate = dt,
                FilterName = metadata.FilterName,
                ProjectId = projectId,
                TargetId = targetId,
                ProfileId = profileId
            };
        }

        //[Test]
        public void AddAcquiredImages() {

            // BEWARE! THIS WILL UPDATE ACTUAL DATABASE USED BY THE PLUGIN
            var testDbPath = @"C:\Users\Tom\AppData\Local\NINA\AssistantPlugin\assistantdb.sqlite";
            db = new SchedulerDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));

            string profileId = "395fdf35-4ca8-479b-bd5a-ff24ca2b2a91";
            using (var context = db.GetContext()) {

                DateTime expTime = DateTime.Now.Date.AddDays(-10);
                for (int i = 0; i < 30; i++) {
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 1, 1, expTime, "L", IsAccepted(), "", GetIMD("L", expTime, 120)));
                    expTime = expTime.AddMinutes(2);
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 1, 1, expTime, "R", IsAccepted(), "", GetIMD("R", expTime, 120)));
                    expTime = expTime.AddMinutes(2);
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 1, 1, expTime, "G", IsAccepted(), "", GetIMD("G", expTime, 120)));
                    expTime = expTime.AddMinutes(2);
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 1, 1, expTime, "B", IsAccepted(), "", GetIMD("B", expTime, 120)));
                    expTime = expTime.AddMinutes(2);
                }

                expTime = DateTime.Now.Date.AddDays(-5);
                for (int i = 0; i < 20; i++) {
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 2, 2, expTime, "Ha", IsAccepted(), "", GetIMD("Ha", expTime, 180)));
                    expTime = expTime.AddMinutes(3);
                }

                for (int i = 0; i < 20; i++) {
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 2, 2, expTime, "OIII", IsAccepted(), "", GetIMD("OIII", expTime, 180)));
                    expTime = expTime.AddMinutes(3);
                }

                for (int i = 0; i < 20; i++) {
                    context.AcquiredImageSet.Add(new AcquiredImage("abcd-1234", 2, 2, expTime, "SII", IsAccepted(), "", GetIMD("SII", expTime, 180)));
                    expTime = expTime.AddMinutes(3);
                }

                context.SaveChanges();
            }
        }

        private static Random rand = new Random();
        private bool IsAccepted() {
            return rand.NextDouble() < .2;
        }

        private ImageMetadata GetIMD(string filterName, DateTime startTime, double duration) {
            ImageMetadata metadata = new ImageMetadata();
            metadata.FileName = @"C:\foo\bar\img.fits";
            metadata.FilterName = filterName;
            metadata.ExposureStartTime = startTime;
            metadata.ExposureDuration = duration;
            return metadata;
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

                    // TODO: set project pref fields
                    //p1Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    //p1.preferences = new ProjectPreferenceOLD(p1Prefs);

                    ExposureTemplate etHa = new ExposureTemplate(profileId, "Ha", "Ha");
                    ExposureTemplate etOIII = new ExposureTemplate(profileId, "OIII", "OIII");
                    ExposureTemplate etSII = new ExposureTemplate(profileId, "SII", "SII");
                    ExposureTemplate etR = new ExposureTemplate(profileId, "R", "R");
                    ExposureTemplate etG = new ExposureTemplate(profileId, "G", "G");
                    ExposureTemplate etB = new ExposureTemplate(profileId, "G", "B");

                    context.ExposureTemplateSet.Add(etHa);
                    context.ExposureTemplateSet.Add(etOIII);
                    context.ExposureTemplateSet.Add(etSII);
                    context.ExposureTemplateSet.Add(etR);
                    context.ExposureTemplateSet.Add(etG);
                    context.ExposureTemplateSet.Add(etB);

                    Target t1 = new Target();
                    t1.Name = "M 42";
                    t1.ra = AstroUtil.HMSToDegrees("5:35:17");
                    t1.dec = AstroUtil.DMSToDegrees("-5:23:28");
                    p1.Targets.Add(t1);

                    t1.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etHa, Desired = 5 });
                    t1.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etOIII, Desired = 5 });
                    t1.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etSII, Desired = 5 });

                    context.ProjectSet.Add(p1);

                    ////

                    Project p2 = new Project(profileId);
                    p2.Name = "Sh2 240";
                    p2.Description = "test project 2";
                    p2.State = ProjectState.Active;
                    p2.ActiveDate = DateTime.Now;

                    // TODO: set project pref fields
                    //p2Prefs.AddRuleWeight(ProjectPriorityRule.RULE_NAME, ProjectPriorityRule.DEFAULT_WEIGHT);
                    //p2.preferences = new ProjectPreferenceOLD(p2Prefs);

                    Target t2 = new Target();
                    t2.Name = "Sh2 240";
                    t2.ra = AstroUtil.HMSToDegrees("5:41:6");
                    t2.dec = AstroUtil.DMSToDegrees("28:5:0");
                    p2.Targets.Add(t2);

                    t2.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etR, Desired = 5 });
                    t2.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etG, Desired = 5 });
                    t2.ExposurePlans.Add(new ExposurePlan { ProfileId = profileId, ExposureTemplate = etB, Desired = 5 });

                    context.ProjectSet.Add(p2);

                    ///

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
        /*
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

                    ExposurePlan ep1 = new ExposurePlan();
                    ep1.FilterName = "Ha";
                    ExposurePlan ep2 = new ExposurePlan();
                    ep2.FilterName = "OIII";

                    t.ExposurePlans.Add(ep1);
                    t.ExposurePlans.Add(ep2);

                    t = new Target();
                    t.Name = "M 42: Frame 2";
                    t.ra = 4.78;
                    t.dec = -10.54;
                    p.Targets.Add(t);

                    ep1 = new ExposurePlan();
                    ep1.FilterName = "Ha";
                    ep2 = new ExposurePlan();
                    ep2.FilterName = "OIII";

                    t.ExposurePlans.Add(ep1);
                    t.ExposurePlans.Add(ep2);

                    context.ProjectSet.Add(p);

                    ExposureTemplate expTemplate = new ExposureTemplate(Guid.NewGuid().ToString(), "Ha");
                    context.ExposureTemplateSet.Add(expTemplate);

                    p.EnableGrader = true;
                    p.MinimumAltitude = 22;
                    p.RuleWeights = new List<RuleWeight> {
                        {new RuleWeight("foo", 0.3) },
                        {new RuleWeight("bar", 0.8) },
                        {new RuleWeight("foo", 0.4) },
                    };

                    context.SaveChanges();

                    List<Project> projects = context.ProjectSet.Include("targets.exposureplans").Include("preferences").ToList();
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
                            List<ExposurePlan> exposurePlans = target.ExposurePlans;

                            foreach (ExposurePlan exposurePlan in exposurePlans) {
                                TestContext.WriteLine($"     exp plan: {exposurePlan.FilterName} {exposurePlan.Exposure}");
                            }
                        }
                    }

                    List<ExposureTemplate> exposureTemplates = context.ExposureTemplateSet.ToList();
                    foreach (ExposureTemplate preference in exposureTemplates) {
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
                        List<ExposurePlan> exposurePlans = target.ExposurePlans;

                        foreach (ExposurePlan exposurePlan in exposurePlans) {
                            TestContext.WriteLine($"     exp plan: {exposurePlan.FilterName} {exposurePlan.Exposure}");
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
        }*/

    }
}
