using Assistant.NINAPlugin.Database;
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
                p1.Name.Should().Be("Project: M42");
                p1.Targets.Count.Should().Be(1);

                p1.MinimumTime.Should().Be(60);
                p1.MinimumAltitude.Should().BeApproximately(23, 0.001);
                p1.UseCustomHorizon.Should().BeFalse();
                p1.HorizonOffset.Should().BeApproximately(11, 0.001);
                p1.FilterSwitchFrequency.Should().Be(12);
                p1.DitherEvery.Should().Be(14);
                p1.EnableGrader.Should().BeFalse();

                p1.RuleWeights[0].Name.Should().Be("a");
                p1.RuleWeights[1].Name.Should().Be("b");
                p1.RuleWeights[2].Name.Should().Be("c");
                p1.RuleWeights[0].Weight.Should().BeApproximately(.1, 0.001);
                p1.RuleWeights[1].Weight.Should().BeApproximately(.2, 0.001);
                p1.RuleWeights[2].Weight.Should().BeApproximately(.3, 0.001);

                Target t1p1 = p1.Targets[0];
                t1p1.Name = "M42";
                t1p1.RA.Should().BeApproximately(83.82, 0.001);
                t1p1.Dec.Should().BeApproximately(-5.391, 0.001);
                t1p1.Rotation.Should().BeApproximately(0, 0.001);
                t1p1.ROI.Should().BeApproximately(1, 0.001);

                t1p1.ExposurePlans.Count.Should().Be(3);
                t1p1.ExposurePlans[0].ExposureTemplate.FilterName.Should().Be("Ha");
                t1p1.ExposurePlans[1].ExposureTemplate.FilterName.Should().Be("OIII");
                t1p1.ExposurePlans[2].ExposureTemplate.FilterName.Should().Be("SII");

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

                p2.RuleWeights[0].Name.Should().Be("d");
                p2.RuleWeights[1].Name.Should().Be("e");
                p2.RuleWeights[2].Name.Should().Be("f");
                p2.RuleWeights[0].Weight.Should().BeApproximately(.4, 0.001);
                p2.RuleWeights[1].Weight.Should().BeApproximately(.5, 0.001);
                p2.RuleWeights[2].Weight.Should().BeApproximately(.6, 0.001);

                Target t1p2 = p2.Targets[0];
                t1p2.Name = "IC1805";
                t1p2.RA.Should().BeApproximately(38.175, 0.001);
                t1p2.Dec.Should().BeApproximately(61.45, 0.001);
                t1p2.Rotation.Should().BeApproximately(0, 0.001);
                t1p2.ROI.Should().BeApproximately(1, 0.001);
                t1p2.ExposurePlans.Count.Should().Be(3);
                t1p2.ExposurePlans[0].ExposureTemplate.FilterName.Should().Be("Ha");
                t1p2.ExposurePlans[1].ExposureTemplate.FilterName.Should().Be("OIII");
                t1p2.ExposurePlans[2].ExposureTemplate.FilterName.Should().Be("SII");

                context.GetExposureTemplates("").Count.Should().Be(0);
                List<ExposureTemplate> ets = context.GetExposureTemplates(profileId);
                ets.Count.Should().Be(3);
                ets[0].FilterName.Should().Be("Ha");
                ets[1].FilterName.Should().Be("OIII");
                ets[2].FilterName.Should().Be("SII");
                ets[0].MoonAvoidanceEnabled.Should().BeFalse();
                ets[1].MoonAvoidanceEnabled.Should().BeFalse();
                ets[2].MoonAvoidanceEnabled.Should().BeFalse();

                // Test GetActiveProjects
                projects = context.GetActiveProjects(profileId, markDate);
                projects.Count.Should().Be(1);
                p1 = projects[0];
                p1.Name.Should().Be("Project: M42");
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
                context.AcquiredImageSet.Add(new AcquiredImage(1, 1, markDate.AddDays(1), "Ha", true, new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, 1, markDate.AddDays(1).AddMinutes(1), "Ha", true, new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, 1, markDate.AddDays(1).AddMinutes(2), "Ha", true, new ImageMetadata(msg)));
                context.AcquiredImageSet.Add(new AcquiredImage(1, 1, markDate.AddDays(1).AddMinutes(3), "Ha", true, new ImageMetadata(msg)));
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

                // Associated image data
                byte[] data1 = new byte[] { 0x21, 0x22, 0x23, 0x24, 0x25 };
                byte[] data2 = new byte[] { 0x26, 0x27, 0x28, 0x29, 0x2a };
                byte[] data3 = new byte[] { 0x2b, 0x2c, 0x2d, 0x2e, 0x2f };
                context.ImageDataSet.Add(new ImageData("tag1", data1, ai[0].Id));
                context.ImageDataSet.Add(new ImageData("tag2", data2, ai[0].Id));
                context.ImageDataSet.Add(new ImageData("tag1", data3, ai[1].Id));
                context.SaveChanges();

                ImageData id = context.GetImageData(ai[0].Id, "tag1");
                id.Tag.Should().Be("tag1");
                string s = Encoding.Default.GetString(id.Data);
                s.Should().Be("!\"#$%");

                id = context.GetImageData(ai[0].Id, "tag2");
                id.Tag.Should().Be("tag2");
                s = Encoding.Default.GetString(id.Data);
                s.Should().Be("&'()*");

                id = context.GetImageData(ai[1].Id, "tag1");
                id.Tag.Should().Be("tag1");
                s = Encoding.Default.GetString(id.Data);
                s.Should().Be("+,-./");

                context.GetImageData(ai[1].Id, "tag2").Should().BeNull();
                context.GetImageData(ai[2].Id, "tag1").Should().BeNull();
            }
        }

        [Test, Order(3)]
        [NonParallelizable]
        public void TestWriteUpdateExposurePlans() {
            using (var context = db.GetContext()) {
                Target target = context.GetTarget(1, 1);
                ExposurePlan fp = target.ExposurePlans.Where(t => t.ExposureTemplate.FilterName == "Ha").First();
                fp.Acquired += 2;
                fp.Accepted += 1;
                context.SaveChanges();
            }

            using (var context = db.GetContext()) {
                Target target = context.GetTarget(1, 1);
                ExposurePlan fp = target.ExposurePlans.Where(t => t.ExposureTemplate.FilterName == "Ha").First();
                fp.Desired.Should().Be(3);
                fp.Acquired.Should().Be(2);
                fp.Accepted.Should().Be(1);
            }
        }

        [Test, Order(4)]
        [NonParallelizable]
        public void TestPasteProject() {
            using (var context = db.GetContext()) {
                List<Project> projects = context.GetAllProjects(profileId);
                projects.Count.Should().Be(2);
                Project p2 = projects[1];

                Target p2t1 = p2.Targets.First();
                TestContext.WriteLine($"P2T1: {p2t1}");

                Project pasted = context.PasteProject("abcd-9876", p2);
                pasted.Should().NotBeNull();

                Target t = pasted.Targets.First();
                t.ra = 1.23;
                context.SaveTarget(t);

                Project pasted2 = context.PasteProject("abcd-9876", pasted);
                Target pasted2t1 = pasted2.Targets.First();
                TestContext.WriteLine($"PS2T1: {pasted2t1}");
            }
        }

        [Test, Order(5)]
        [NonParallelizable]
        public void TestPasteTarget() {
            using (var context = db.GetContext()) {

                List<Project> projects = context.GetAllProjects(profileId);
                projects.Count.Should().Be(2);

                Project p1 = projects[0];
                Project p2 = projects[1];
                Target p2t1 = p2.Targets[0];

                context.PasteTarget(p1, p2t1).Should().NotBeNull();
            }
        }

        [Test, Order(6)]
        [NonParallelizable]
        public void TestNewExposurePlan() {
            using (var context = db.GetContext()) {

                ExposureTemplate et = context.GetExposureTemplate(1);
                ExposurePlan ep = new ExposurePlan(et.profileId);
                ep.ExposureTemplateId = et.Id;
                ep.Exposure = 120;
                ep.Desired = 10;

                List<Project> projects = context.GetAllProjects(profileId);
                Target p2t1 = projects[1].Targets[0];
                p2t1.ExposurePlans.Add(ep);

                context.SaveTarget(p2t1);
            }
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

                    p1.RuleWeights = new List<RuleWeight> {
                        {new RuleWeight("a", .1) },
                        {new RuleWeight("b", .2) },
                        {new RuleWeight("c", .3) }
                    };

                    ExposureTemplate etHa = new ExposureTemplate(profileId, "Ha", "Ha");
                    ExposureTemplate etOIII = new ExposureTemplate(profileId, "OIII", "OIII");
                    ExposureTemplate etSII = new ExposureTemplate(profileId, "SII", "SII");
                    context.ExposureTemplateSet.Add(etHa);
                    context.ExposureTemplateSet.Add(etOIII);
                    context.ExposureTemplateSet.Add(etSII);
                    context.SaveChanges();

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

                    p2.RuleWeights = new List<RuleWeight> {
                        {new RuleWeight("d", .4) },
                        {new RuleWeight("e", .5) },
                        {new RuleWeight("f", .6) }
                    };

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

                    context.SaveChanges();
                }
                catch (DbEntityValidationException e) {
                    StringBuilder sb = new StringBuilder();
                    foreach (var eve in e.EntityValidationErrors) {
                        foreach (var dbeve in eve.ValidationErrors) {
                            sb.Append(dbeve.ErrorMessage).Append("\n");
                        }
                    }

                    TestContext.Error.WriteLine($"DB VALIDATION EXCEPTION: {sb}");
                    throw e;
                }
                catch (Exception e) {
                    TestContext.Error.WriteLine($"OTHER EXCEPTION: {e.Message}\n{e}");
                    throw e;
                }
            }
        }
    }
}
