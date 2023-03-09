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
        [Ignore("tbd")]
        public void TomTest1() {
            string profileId = "3c160865-776f-4f72-8a05-5964225ca0fa"; // Zim
            using (var context = db.GetContext()) {
                try {
                    Project p1 = new Project(profileId);
                    p1.Name = "Project: M42";
                    p1.Description = "";
                    p1.State = ProjectState.Active;
                    p1.ActiveDate = new DateTime(2022, 12, 1);
                    p1.StartDate = p1.ActiveDate;
                    p1.EndDate = new DateTime(2023, 2, 1);
                    // TODO: set new project prefs here
                    /*
                    AssistantProjectPreferencesOLD p1Prefs = new AssistantProjectPreferencesOLD();
                    p1Prefs.SetDefaults();
                    p1Prefs.MinimumAltitude = 10;
                    SetDefaultRuleWeights(p1Prefs);
                    p1.preferences = new ProjectPreferenceOLD(p1Prefs);
                    */

                    ExposureTemplate etHa = new ExposureTemplate(profileId, "Ha", "Ha");
                    ExposureTemplate etOIII = new ExposureTemplate(profileId, "OIII", "OIII");
                    ExposureTemplate etSII = new ExposureTemplate(profileId, "SII", "SII");
                    context.ExposureTemplateSet.Add(etHa);
                    context.ExposureTemplateSet.Add(etOIII);
                    context.ExposureTemplateSet.Add(etSII);

                    Target t1 = new Target();
                    t1.Name = "M42";
                    t1.ra = TestUtil.M42.RADegrees;
                    t1.dec = TestUtil.M42.Dec;
                    p1.Targets.Add(t1);

                    ExposurePlan ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etHa.Id;
                    ep.Desired = 3;
                    ep.Exposure = 20;
                    t1.ExposurePlans.Add(ep);

                    ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etOIII.Id;
                    ep.Desired = 3;
                    ep.Exposure = 20;
                    t1.ExposurePlans.Add(ep);

                    ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etSII.Id;
                    ep.Desired = 3;
                    ep.Exposure = 20;
                    t1.ExposurePlans.Add(ep);

                    context.ProjectSet.Add(p1);

                    Project p2 = new Project(profileId);
                    p2.Name = "Project: IC1805";
                    p2.Description = "";
                    p2.State = ProjectState.Active;
                    p2.ActiveDate = new DateTime(2022, 12, 1);
                    p2.StartDate = p2.ActiveDate;
                    p2.EndDate = new DateTime(2023, 2, 1);

                    Target t2 = new Target();
                    t2.Name = "IC1805";
                    t2.ra = TestUtil.IC1805.RADegrees;
                    t2.dec = TestUtil.IC1805.Dec;
                    p2.Targets.Add(t2);

                    ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etHa.Id;
                    ep.Desired = 5;
                    ep.Exposure = 20;
                    t2.ExposurePlans.Add(ep);

                    ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etOIII.Id;
                    ep.Desired = 5;
                    ep.Exposure = 20;
                    t2.ExposurePlans.Add(ep);

                    ep = new ExposurePlan(profileId);
                    ep.ExposureTemplateId = etSII.Id;
                    ep.Desired = 5;
                    ep.Exposure = 20;
                    t2.ExposurePlans.Add(ep);

                    context.ProjectSet.Add(p2);

                    //ImageMetadata imd = new ImageMetadata(PlanMocks.GetImageSavedEventArgs(DateTime.Now, "Ha"));
                    //AcquiredImage ai = new AcquiredImage(1, DateTime.Now, "Ha", imd);
                    //context.AcquiredImageSet.Add(ai);

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

        [Test]
        [Ignore("")]
        public void RealTest1() {

            using (var context = db.GetContext()) {
                try {
                    DateTime atTime = new DateTime(2023, 1, 26);
                    string profileId = "3c160865-776f-4f72-8a05-5964225ca0fa"; // Zim
                    //string profileId = "1f78fa60-ab20-41af-9c17-a12016553007"; // Astroimaging Redcat 51 / ASI1600mm

                    Project p1 = new Project(profileId);
                    p1.Name = "Project: C00";
                    p1.Description = "";
                    p1.State = ProjectState.Active;
                    p1.ActiveDate = atTime.AddDays(-1);
                    p1.StartDate = atTime;
                    p1.EndDate = atTime.AddDays(100);
                    p1.MinimumTime = 60;
                    p1.MinimumAltitude = 22;
                    p1.UseCustomHorizon = false;
                    p1.HorizonOffset = 0;
                    p1.FilterSwitchFrequency = 1;
                    p1.DitherEvery = 2;
                    SetDefaultRuleWeights(p1);

                    ExposureTemplate etHa = new ExposureTemplate(profileId, "Ha", "Ha");
                    ExposureTemplate etOIII = new ExposureTemplate(profileId, "OIII", "OIII");
                    ExposureTemplate etSII = new ExposureTemplate(profileId, "SII", "SII");
                    ExposureTemplate etLum = new ExposureTemplate(profileId, "Lum", "Lum");
                    ExposureTemplate etRed = new ExposureTemplate(profileId, "Red", "Red");
                    ExposureTemplate etGrn = new ExposureTemplate(profileId, "Green", "Green");
                    ExposureTemplate etBlu = new ExposureTemplate(profileId, "Blue", "Blue");
                    context.ExposureTemplateSet.Add(etHa);
                    context.ExposureTemplateSet.Add(etOIII);
                    context.ExposureTemplateSet.Add(etSII);
                    context.ExposureTemplateSet.Add(etLum);
                    context.ExposureTemplateSet.Add(etRed);
                    context.ExposureTemplateSet.Add(etGrn);
                    context.ExposureTemplateSet.Add(etBlu);

                    Target t1 = new Target();
                    t1.Name = "C00";
                    t1.ra = TestUtil.C00.RA;
                    t1.dec = TestUtil.C00.Dec;
                    p1.Targets.Add(t1);

                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etLum, 5, 0, 60));
                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etRed, 5, 0, 60));
                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etGrn, 5, 0, 60));
                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etBlu, 5, 0, 60));

                    Project p2 = new Project(profileId);
                    p2.Name = "Project: C90";
                    p2.Description = "";
                    p2.State = ProjectState.Active;
                    p2.ActiveDate = atTime.AddDays(-1);
                    p2.StartDate = atTime;
                    p2.EndDate = atTime.AddDays(100);

                    SetDefaultRuleWeights(p2);

                    Target t2 = new Target();
                    t2.Name = "C90";
                    t2.ra = TestUtil.C90.RA;
                    t2.dec = TestUtil.C90.Dec;
                    p2.Targets.Add(t2);

                    t2.ExposurePlans.Add(GetExposurePlan(profileId, etHa, 5, 0, 90));
                    t2.ExposurePlans.Add(GetExposurePlan(profileId, etOIII, 5, 0, 90));
                    t2.ExposurePlans.Add(GetExposurePlan(profileId, etSII, 5, 0, 90));

                    context.ProjectSet.Add(p1);
                    context.ProjectSet.Add(p2);

                    context.SaveChanges();
                }
                catch (Exception e) {
                    TestContext.Error.WriteLine($"failed to create test database: {e.Message}\n{e.ToString()}");
                    throw e;
                }
            }
        }

        [Test]
        [Ignore("")]
        public void DaytimeTest1() {

            using (var context = db.GetContext()) {
                try {
                    DateTime atTime = new DateTime(2023, 1, 28);
                    string profileId = "3c160865-776f-4f72-8a05-5964225ca0fa"; // Zim
                    //string profileId = "1f78fa60-ab20-41af-9c17-a12016553007"; // Astroimaging Redcat 51 / ASI1600mm

                    Project p1 = new Project(profileId);
                    p1.Name = "Project: C00";
                    p1.Description = "";
                    p1.State = ProjectState.Active;
                    p1.ActiveDate = atTime.AddDays(-1);
                    p1.StartDate = atTime;
                    p1.EndDate = atTime.AddDays(100);
                    p1.MinimumTime = 30;
                    p1.MinimumAltitude = 0;
                    p1.UseCustomHorizon = false;
                    p1.HorizonOffset = 0;
                    p1.FilterSwitchFrequency = 0;
                    p1.DitherEvery = 0;
                    p1.EnableGrader = true;

                    ExposureTemplate etLum = new ExposureTemplate(profileId, "L", "L");
                    ExposureTemplate etRed = new ExposureTemplate(profileId, "R", "R");
                    ExposureTemplate etGrn = new ExposureTemplate(profileId, "G", "G");
                    ExposureTemplate etBlu = new ExposureTemplate(profileId, "B", "B");
                    context.ExposureTemplateSet.Add(etLum);
                    context.ExposureTemplateSet.Add(etRed);
                    context.ExposureTemplateSet.Add(etGrn);
                    context.ExposureTemplateSet.Add(etBlu);

                    Target t1 = new Target();
                    t1.Name = "C00";
                    t1.ra = TestUtil.C00.RA;
                    t1.dec = TestUtil.C00.Dec;
                    p1.Targets.Add(t1);

                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etLum, 5, 0, 60));
                    t1.ExposurePlans.Add(GetExposurePlan(profileId, etRed, 5, 0, 60));
                    //t1.ExposurePlans.Add(GetExposurePlan(profileId, etGrn, 5, 0, 60));
                    //t1.ExposurePlans.Add(GetExposurePlan(profileId, etBlu, 5, 0, 60));

                    context.ProjectSet.Add(p1);

                    context.SaveChanges();
                }
                catch (Exception e) {
                    TestContext.Error.WriteLine($"failed to create test database: {e.Message}\n{e.ToString()}");
                    throw e;
                }
            }
        }

        private void SetDefaultRuleWeights(Project project) {
            Dictionary<string, IScoringRule> rules = ScoringRule.GetAllScoringRules();
            foreach (KeyValuePair<string, IScoringRule> entry in rules) {
                var rule = entry.Value;
                project.ruleWeights.Add(new RuleWeight(rule.Name, rule.DefaultWeight));
            }
        }

        private AssistantDatabaseInteraction GetDatabase() {
            var testDbPath = Path.Combine(TestContext.CurrentContext.TestDirectory, @"assistantdb.sqlite");
            TestContext.WriteLine($"DB PATH: {testDbPath}");
            return new AssistantDatabaseInteraction(string.Format(@"Data Source={0};", testDbPath));
        }

        private ExposurePlan GetExposurePlan(string profileId, ExposureTemplate exposureTemplate, int desired, int accepted, int exposure) {
            ExposurePlan ep = new ExposurePlan(profileId);
            ep.ExposureTemplateId = exposureTemplate.Id;
            ep.Desired = desired;
            ep.Accepted = accepted;
            ep.Exposure = exposure;
            return ep;
        }

        private List<IPlanProject> ReadAndDump(string profileId, DateTime atTime) {

            List<Project> projects = null;
            List<ExposureTemplate> exposureTemplates = null;

            AssistantDatabaseInteraction database = GetDatabase();
            using (var context = database.GetContext()) {
                try {
                    projects = context.GetActiveProjects(profileId, atTime);
                    exposureTemplates = context.GetExposureTemplates(profileId);
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

            Dictionary<string, ExposureTemplate> dict = new Dictionary<string, ExposureTemplate>();
            foreach (ExposureTemplate exposureTemplate in exposureTemplates) {
                dict.Add(exposureTemplate.FilterName, exposureTemplate);
            }
            Dictionary<string, ExposureTemplate> exposureTemplatesDictionary = dict;

            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(profileMock.Object.ActiveProfile, project, exposureTemplatesDictionary);
                planProjects.Add(planProject);
                TestContext.WriteLine($"PROJECT:\n{planProject}");
            }

            return planProjects;
        }
    }
}
