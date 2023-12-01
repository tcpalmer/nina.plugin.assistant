using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sequencer;
using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Shared.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Sequencer {

    [TestFixture]
    public class FlatsExpertTest {

        [Test]
        public void TestGetPotentialTargets() {
            List<Project> projects = GetTestProjects1();
            List<Target> targets = new FlatsExpert().GetTargetsForPeriodicFlats(projects);
            targets.Count.Should().Be(4);
            targets[0].Name.Should().Be("T3");
            targets[0].Id.Should().Be(3);
            targets[1].Name.Should().Be("T5");
            targets[1].Id.Should().Be(5);
            targets[2].Name.Should().Be("T6");
            targets[2].Id.Should().Be(6);
            targets[3].Name.Should().Be("T7");
            targets[3].Id.Should().Be(7);
        }

        [Test]
        public void TestGetCompletedTargetsForFlats() {
            List<Project> projects = GetTestProjects2();
            List<Target> targets = new FlatsExpert().GetCompletedTargetsForFlats(projects);
            targets.Count.Should().Be(1);
            targets[0].Name.Should().Be("T8");
            targets[0].Id.Should().Be(8);
        }

        [Test]
        public void TestGetLightSessions() {
            FlatsExpert sut = new FlatsExpert();
            List<Project> projects = GetTestProjects1();
            List<Target> targets = sut.GetTargetsForPeriodicFlats(projects);
            targets.Count.Should().Be(4);

            List<LightSession> sessions = sut.GetLightSessions(targets, GetTestAcquiredImages());
            sessions.Count.Should().Be(12);
            DateTime d1 = sut.GetLightSessionDate(DateTime.Now.Date.AddDays(-5).AddHours(23));
            DateTime d2 = sut.GetLightSessionDate(DateTime.Now.Date.AddDays(-4).AddHours(26));

            sessions.Contains(new LightSession(3, d1, new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(3, d1, new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(3, d1, new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(5, d1, new FlatSpec("Ha", 11, 20, new BinningMode(1, 1), 0, 0, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(5, d1, new FlatSpec("O3", 11, 20, new BinningMode(1, 1), 0, 0, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(5, d1, new FlatSpec("S2", 11, 20, new BinningMode(1, 1), 0, 0, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(6, d1, new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 12, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(6, d1, new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 12, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(6, d1, new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 12, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(3, d2, new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(3, d2, new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();
            sessions.Contains(new LightSession(3, d2, new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 10, 100))).Should().BeTrue();

            sessions[0].TargetId.Should().Be(3);
            sessions[1].TargetId.Should().Be(3);
            sessions[2].TargetId.Should().Be(3);
            sessions[3].TargetId.Should().Be(5);
            sessions[4].TargetId.Should().Be(5);
            sessions[5].TargetId.Should().Be(5);
            sessions[6].TargetId.Should().Be(6);
            sessions[7].TargetId.Should().Be(6);
            sessions[8].TargetId.Should().Be(6);
            sessions[9].TargetId.Should().Be(3);
            sessions[10].TargetId.Should().Be(3);
            sessions[11].TargetId.Should().Be(3);
        }

        [Test]
        public void TestGetNeededPeriodicFlats() {
            FlatsExpert sut = new FlatsExpert();

            List<Target> targets = sut.GetTargetsForPeriodicFlats(GetTestProjects3());
            targets.Count.Should().Be(1);

            DateTime baseDate = new DateTime(2023, 12, 1).AddHours(18);
            List<LightSession> sessions = new List<LightSession>();
            List<FlatHistory> history = new List<FlatHistory>();

            // No light sessions ...
            sut.GetNeededPeriodicFlats(baseDate.AddDays(2), targets, sessions, history).Count.Should().Be(0);

            DateTime sd1 = sut.GetLightSessionDate(baseDate);
            FlatSpec fs1 = new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs2 = new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs3 = new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 12, 100);

            LightSession ls1 = new LightSession(1, sd1, fs1);
            LightSession ls2 = new LightSession(1, sd1, fs2);
            LightSession ls3 = new LightSession(1, sd1, fs3);
            sessions = new List<LightSession>() { ls1, ls2, ls3 };

            // Three light sessions and no flat history but too soon ...
            sut.GetNeededPeriodicFlats(baseDate.AddDays(1), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(2), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(3), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(4), targets, sessions, history).Count.Should().Be(0);

            // It's time ...
            List<LightSession> got = sut.GetNeededPeriodicFlats(baseDate.AddDays(5), targets, sessions, history);
            got.Count.Should().Be(3);
            got[0].Should().BeEquivalentTo(ls1);
            got[1].Should().BeEquivalentTo(ls2);
            got[2].Should().BeEquivalentTo(ls3);

            // Now 'take' the flats and none will be needed ...
            DateTime flatsTaken = baseDate.AddDays(5);
            FlatHistory fh1 = GetFlatHistory(1, sd1, flatsTaken, fs1);
            FlatHistory fh2 = GetFlatHistory(1, sd1, flatsTaken, fs2);
            FlatHistory fh3 = GetFlatHistory(1, sd1, flatsTaken, fs3);
            history = new List<FlatHistory>() { fh1, fh2, fh3 };
            sut.GetNeededPeriodicFlats(baseDate.AddDays(5), targets, sessions, history).Count.Should().Be(0);

            // Additional sessions are added but too soon for them ...
            DateTime sd2 = sut.GetLightSessionDate(baseDate).AddDays(6);
            LightSession ls4 = new LightSession(1, sd2, fs1);
            LightSession ls5 = new LightSession(1, sd2, fs2);
            LightSession ls6 = new LightSession(1, sd2, fs3);
            sessions = new List<LightSession>() { ls1, ls2, ls3, ls4, ls5, ls6 };
            sut.GetNeededPeriodicFlats(baseDate.AddDays(7), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(8), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(9), targets, sessions, history).Count.Should().Be(0);
            sut.GetNeededPeriodicFlats(baseDate.AddDays(10), targets, sessions, history).Count.Should().Be(0);

            // It's time ...
            got = sut.GetNeededPeriodicFlats(baseDate.AddDays(11), targets, sessions, history);
            got.Count.Should().Be(3);
            got[0].Should().BeEquivalentTo(ls4);
            got[1].Should().BeEquivalentTo(ls5);
            got[2].Should().BeEquivalentTo(ls6);

            // Now take the latest flats and none will be needed ...
            flatsTaken = baseDate.AddDays(11);
            FlatHistory fh4 = GetFlatHistory(1, sd2, flatsTaken, fs1);
            FlatHistory fh5 = GetFlatHistory(1, sd2, flatsTaken, fs2);
            FlatHistory fh6 = GetFlatHistory(1, sd2, flatsTaken, fs3);
            history = new List<FlatHistory>() { fh1, fh2, fh3, fh4, fh5, fh6 };
            sut.GetNeededPeriodicFlats(baseDate.AddDays(5), targets, sessions, history).Count.Should().Be(0);
        }

        [Test]
        public void TestGetNeededTargetCompletionFlats() {
            FlatsExpert sut = new FlatsExpert();

            List<Target> targets = sut.GetCompletedTargetsForFlats(GetTestProjects4());
            targets.Count.Should().Be(2);

            DateTime baseDate = new DateTime(2023, 12, 1).AddHours(18);
            List<LightSession> sessions = new List<LightSession>();
            List<FlatHistory> history = new List<FlatHistory>();

            // No light sessions ...
            sut.GetNeededTargetCompletionFlats(targets, sessions, history).Count.Should().Be(0);

            DateTime sd1 = sut.GetLightSessionDate(baseDate);
            DateTime sd2 = sut.GetLightSessionDate(baseDate.AddDays(2));
            FlatSpec fs1 = new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs2 = new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs3 = new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 12, 100);

            LightSession ls1 = new LightSession(1, sd1, fs1);
            LightSession ls2 = new LightSession(1, sd1, fs2);
            LightSession ls3 = new LightSession(1, sd1, fs3);
            LightSession ls4 = new LightSession(1, sd2, fs1);
            LightSession ls5 = new LightSession(1, sd2, fs2);
            LightSession ls6 = new LightSession(1, sd2, fs3);
            sessions = new List<LightSession>() { ls1, ls2, ls3, ls4, ls5, ls6 };

            // Six light sessions and no flat history ...
            List<LightSession> needed = sut.GetNeededTargetCompletionFlats(targets, sessions, history);
            needed.Count.Should().Be(6);
            needed[0].FlatSpec.FilterName.Should().Be("Ha");
            needed[1].FlatSpec.FilterName.Should().Be("O3");
            needed[2].FlatSpec.FilterName.Should().Be("S2");
            needed[3].FlatSpec.FilterName.Should().Be("Ha");
            needed[4].FlatSpec.FilterName.Should().Be("O3");
            needed[5].FlatSpec.FilterName.Should().Be("S2");

            FlatHistory fh1 = GetFlatHistory(1, sd1, baseDate.AddDays(4), fs1);
            FlatHistory fh2 = GetFlatHistory(1, sd1, baseDate.AddDays(4), fs2);
            history = new List<FlatHistory>() { fh1, fh2 };

            // Two already taken, four remain ...
            needed = sut.GetNeededTargetCompletionFlats(targets, sessions, history);
            needed.Count.Should().Be(4);
            needed[0].FlatSpec.FilterName.Should().Be("S2");
            needed[1].FlatSpec.FilterName.Should().Be("Ha");
            needed[2].FlatSpec.FilterName.Should().Be("O3");
            needed[3].FlatSpec.FilterName.Should().Be("S2");

            FlatHistory fh3 = GetFlatHistory(1, sd1, baseDate.AddDays(5), fs3);
            history = new List<FlatHistory>() { fh1, fh2, fh3 };

            // Three remaining ...
            needed = sut.GetNeededTargetCompletionFlats(targets, sessions, history);
            needed.Count.Should().Be(3);
            needed[0].FlatSpec.FilterName.Should().Be("Ha");
            needed[1].FlatSpec.FilterName.Should().Be("O3");
            needed[2].FlatSpec.FilterName.Should().Be("S2");

            FlatHistory fh4 = GetFlatHistory(1, sd2, baseDate.AddDays(6), fs1);
            FlatHistory fh5 = GetFlatHistory(1, sd2, baseDate.AddDays(6), fs2);
            FlatHistory fh6 = GetFlatHistory(1, sd2, baseDate.AddDays(6), fs3);
            history = new List<FlatHistory>() { fh1, fh2, fh3, fh4, fh5, fh6 };

            // All done
            needed = sut.GetNeededTargetCompletionFlats(targets, sessions, history);
            needed.Count.Should().Be(0);
        }

        [Test]
        public void TestGetLightSessionDate() {
            FlatsExpert sut = new FlatsExpert();

            DateTime d1 = DateTime.Now.Date.AddHours(18).AddMinutes(22);
            sut.GetLightSessionDate(d1).Should().Be(DateTime.Now.Date.AddHours(12));
            d1 = DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            sut.GetLightSessionDate(d1).Should().Be(DateTime.Now.Date.AddHours(12));

            DateTime d2 = DateTime.Now.Date.AddHours(1).AddMinutes(22);
            sut.GetLightSessionDate(d2).Should().Be(DateTime.Now.Date.AddDays(-1).AddHours(12));
            d1 = DateTime.Now.Date.AddSeconds(1);
            sut.GetLightSessionDate(d2).Should().Be(DateTime.Now.Date.AddDays(-1).AddHours(12));
        }

        [Test]
        public void TestFlatsSpec() {
            FlatSpec fs1 = new FlatSpec("Ha", 10, 20, new BinningMode(2, 2), 0, 123.4, 89);
            fs1.FilterName.Should().Be("Ha");
            fs1.Gain.Should().Be(10);
            fs1.Offset.Should().Be(20);
            fs1.BinningMode.X.Should().Be(2);
            fs1.ReadoutMode.Should().Be(0);
            fs1.Rotation.Should().Be(123.4);
            fs1.ROI.Should().Be(89);
            fs1.Key.Should().Be("Ha_10_20_2x2_0_123.4_89");

            ImageMetadata imageMetaData = GetImageMetadata("Ha", 10, 20, "2x2", 0, 123.4, 89);
            AcquiredImage acquiredImage = new AcquiredImage(imageMetaData);
            acquiredImage.FilterName = imageMetaData.FilterName;
            FlatSpec fs2 = new FlatSpec(acquiredImage);
            fs2.FilterName.Should().Be("Ha");
            fs2.Gain.Should().Be(10);
            fs2.Offset.Should().Be(20);
            fs2.BinningMode.X.Should().Be(2);
            fs2.ReadoutMode.Should().Be(0);
            fs2.Rotation.Should().Be(123.4);
            fs2.ROI.Should().Be(89);
            fs2.Key.Should().Be("Ha_10_20_2x2_0_123.4_89");

            fs1.Equals(fs2).Should().BeTrue();

            FlatSpec fs3 = new FlatSpec("Ha", 10, 20, new BinningMode(2, 2), 0, ImageMetadata.NO_ROTATOR_ANGLE, 89);
            fs3.FilterName.Should().Be("Ha");
            fs3.Gain.Should().Be(10);
            fs3.Offset.Should().Be(20);
            fs3.BinningMode.X.Should().Be(2);
            fs3.ReadoutMode.Should().Be(0);
            fs3.Rotation.Should().Be(ImageMetadata.NO_ROTATOR_ANGLE);
            fs3.ROI.Should().Be(89);
            fs3.Key.Should().Be("Ha_10_20_2x2_0_na_89");

            imageMetaData = GetImageMetadata("Ha", 10, 20, "2x2", 0, ImageMetadata.NO_ROTATOR_ANGLE, 89);
            acquiredImage = new AcquiredImage(imageMetaData);
            acquiredImage.FilterName = imageMetaData.FilterName;
            FlatSpec fs4 = new FlatSpec(acquiredImage);
            fs4.FilterName.Should().Be("Ha");
            fs4.Gain.Should().Be(10);
            fs4.Offset.Should().Be(20);
            fs4.BinningMode.X.Should().Be(2);
            fs4.ReadoutMode.Should().Be(0);
            fs4.Rotation.Should().Be(ImageMetadata.NO_ROTATOR_ANGLE);
            fs4.ROI.Should().Be(89);
            fs4.Key.Should().Be("Ha_10_20_2x2_0_na_89");

            fs3.Equals(fs4).Should().BeTrue();

            fs1.Equals(fs3).Should().BeFalse();
            fs1.Equals(fs4).Should().BeFalse();
            fs2.Equals(fs3).Should().BeFalse();
            fs2.Equals(fs4).Should().BeFalse();
        }

        [Test]
        public void TestLightSession() {
            FlatsExpert fe = new FlatsExpert();

            FlatSpec fs1 = new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 123.4, 89);
            FlatSpec fs2 = new FlatSpec("OIII", 10, 20, new BinningMode(1, 1), 0, 123.4, 89);
            FlatSpec fs3 = new FlatSpec("SII", 10, 20, new BinningMode(1, 1), 0, 123.4, 89);
            FlatSpec fs4 = new FlatSpec("Red", 10, 20, new BinningMode(1, 1), 0, ImageMetadata.NO_ROTATOR_ANGLE, 89);
            FlatSpec fs5 = new FlatSpec("Red", 10, 20, new BinningMode(1, 1), 0, ImageMetadata.NO_ROTATOR_ANGLE, 100);

            DateTime sd1 = fe.GetLightSessionDate(DateTime.Now.Date.AddHours(18));
            DateTime sd2 = fe.GetLightSessionDate(DateTime.Now.Date.AddDays(2).AddHours(18));
            DateTime sd3 = fe.GetLightSessionDate(DateTime.Now.Date.AddDays(3).AddHours(18));

            LightSession ls1 = new LightSession(1, sd1, fs1);
            ls1.Equals(ls1).Should().BeTrue();
            LightSession ls2 = new LightSession(1, sd1, fs1);
            ls1.Equals(ls2).Should().BeTrue();

            ls2 = new LightSession(2, sd1, fs1);
            ls1.Equals(ls2).Should().BeFalse();
            ls2 = new LightSession(2, sd1, fs2);
            ls1.Equals(ls2).Should().BeFalse();

            ls1 = new LightSession(1, sd2, fs1);
            ls2 = new LightSession(1, sd2, fs1);
            ls1.Equals(ls2).Should().BeTrue();
            ls2 = new LightSession(2, sd1, fs1);
            ls1.Equals(ls2).Should().BeFalse();

            List<LightSession> list = new List<LightSession> { ls1, ls2 };
            list.Contains(ls1).Should().BeTrue();
            list.Contains(ls2).Should().BeTrue();
            LightSession ls3 = new LightSession(3, sd3, fs3);
            list.Contains(ls3).Should().BeFalse();
            list.Add(ls3);
            list.Contains(ls3).Should().BeTrue();

            LightSession ls4 = new LightSession(1, sd1, fs4);
            ls4.Equals(ls4).Should().BeTrue();
            LightSession ls5 = new LightSession(1, sd1, fs4);
            ls4.Equals(ls5).Should().BeTrue();

            ls5 = new LightSession(2, sd1, fs4);
            ls4.Equals(ls5).Should().BeFalse();

            ls4 = new LightSession(2, sd2, fs4);
            ls5 = new LightSession(2, sd2, fs4);
            ls4.Equals(ls5).Should().BeTrue();

            ls5 = new LightSession(2, sd1, fs4);
            ls4.Equals(ls5).Should().BeFalse();

            ls5 = new LightSession(2, sd2, fs5);
            ls4.Equals(ls5).Should().BeFalse();
        }

        private FlatHistory GetFlatHistory(int targetId, DateTime lightSessionDate, DateTime flatsTakenDate, FlatSpec flatSpec) {
            return new FlatHistory() {
                TargetId = targetId,
                LightSessionDate = lightSessionDate,
                FlatsTakenDate = flatsTakenDate,
                FilterName = flatSpec.FilterName,
                Gain = flatSpec.Gain,
                Offset = flatSpec.Offset,
                BinningMode = flatSpec.BinningMode,
                ReadoutMode = flatSpec.ReadoutMode,
                Rotation = flatSpec.Rotation,
                ROI = flatSpec.ROI
            };
        }

        private List<Project> GetTestProjects1() {
            Project p1 = new Project("abcd-1234") { Name = "P1", State = ProjectState.Inactive, FlatsHandling = 1 };
            Project p2 = new Project("abcd-1234") { Name = "P2", State = ProjectState.Active, FlatsHandling = Project.FLATS_HANDLING_OFF };
            Project p3 = new Project("abcd-1234") { Name = "P3", State = ProjectState.Active, FlatsHandling = 3 };
            Project p4 = new Project("abcd-1234") { Name = "P4", State = ProjectState.Active, FlatsHandling = Project.FLATS_HANDLING_TARGET_COMPLETION };
            Project p5 = new Project("abcd-1234") { Name = "P5", State = ProjectState.Active, FlatsHandling = Project.FLATS_HANDLING_ROTATED };

            Target t1 = new Target() { Id = 1, Name = "T1" };
            Target t2 = new Target() { Id = 2, Name = "T2" };
            Target t3 = new Target() { Id = 3, Name = "T3" };
            Target t4 = new Target() { Id = 4, Name = "T4" };
            Target t5 = new Target() { Id = 5, Name = "T5" };
            Target t6 = new Target() { Id = 6, Name = "T6" };
            Target t7 = new Target() { Id = 7, Name = "T7" };

            p1.Targets.Add(t1);
            p2.Targets.Add(t2);
            p3.Targets.Add(t3);
            p4.Targets.Add(t4);
            p3.Targets.Add(t5);
            p3.Targets.Add(t6);
            p5.Targets.Add(t7);

            return new List<Project>() { p1, p2, p3, p4, p5 };
        }

        private List<Project> GetTestProjects2() {
            List<Project> projects = GetTestProjects1();

            Target t8 = new Target() { Id = 8, Name = "T8" };
            t8.ExposurePlans.Add(new ExposurePlan() { Desired = 10, Accepted = 10 });
            Target t9 = new Target() { Id = 9, Name = "T9" };
            t9.ExposurePlans.Add(new ExposurePlan() { Desired = 10, Accepted = 5 });

            projects[3].Targets.Add(t8);
            projects[3].Targets.Add(t9);

            return projects;
        }

        private List<Project> GetTestProjects3() {
            Project p1 = new Project("abcd-1234") { Name = "P1", State = ProjectState.Active, FlatsHandling = 5 };
            Target t1 = new Target() { Id = 1, Name = "T1", Project = p1 };
            p1.Targets.Add(t1);

            return new List<Project>() { p1 };
        }

        private List<Project> GetTestProjects4() {
            Project p1 = new Project("abcd-1234") { Name = "P1", State = ProjectState.Active, FlatsHandling = Project.FLATS_HANDLING_TARGET_COMPLETION };
            Project p2 = new Project("abcd-1234") { Name = "P2", State = ProjectState.Active, FlatsHandling = Project.FLATS_HANDLING_TARGET_COMPLETION };

            Target t1 = new Target() { Id = 1, Name = "T1", Project = p1 };
            t1.ExposurePlans.Add(new ExposurePlan() { Desired = 10, Accepted = 10 });
            p1.Targets.Add(t1);

            Target t2 = new Target() { Id = 2, Name = "T2", Project = p2 };
            t2.ExposurePlans.Add(new ExposurePlan() { Desired = 20, Accepted = 30 });
            p2.Targets.Add(t2);

            return new List<Project>() { p1, p2 };
        }

        private List<AcquiredImage> GetTestAcquiredImages() {

            DateTime d1 = DateTime.Now.Date.AddDays(-5).AddHours(23);

            AcquiredImage T3a1 = GetAcquiredImage(d1, 3, GetImageMetadata("Ha", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T3a2 = GetAcquiredImage(d1, 3, GetImageMetadata("O3", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T3a3 = GetAcquiredImage(d1, 3, GetImageMetadata("S2", 10, 20, "1x1", 0, 10, 100));

            AcquiredImage T4a1 = GetAcquiredImage(d1, 4, GetImageMetadata("Ha", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T4a2 = GetAcquiredImage(d1, 4, GetImageMetadata("O3", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T4a3 = GetAcquiredImage(d1, 4, GetImageMetadata("S2", 10, 20, "1x1", 0, 10, 100));

            AcquiredImage T5a1 = GetAcquiredImage(d1, 5, GetImageMetadata("Ha", 11, 20, "1x1", 0, 0, 100));
            AcquiredImage T5a2 = GetAcquiredImage(d1, 5, GetImageMetadata("O3", 11, 20, "1x1", 0, 0, 100));
            AcquiredImage T5a3 = GetAcquiredImage(d1, 5, GetImageMetadata("S2", 11, 20, "1x1", 0, 0, 100));

            AcquiredImage T6a1 = GetAcquiredImage(d1, 6, GetImageMetadata("Ha", 10, 20, "1x1", 0, 12, 100));
            AcquiredImage T6a2 = GetAcquiredImage(d1, 6, GetImageMetadata("O3", 10, 20, "1x1", 0, 12, 100));
            AcquiredImage T6a3 = GetAcquiredImage(d1, 6, GetImageMetadata("S2", 10, 20, "1x1", 0, 12, 100));

            DateTime d2 = DateTime.Now.Date.AddDays(-4).AddHours(26);

            AcquiredImage T3a4 = GetAcquiredImage(d2, 3, GetImageMetadata("Ha", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T3a5 = GetAcquiredImage(d2, 3, GetImageMetadata("O3", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T3a6 = GetAcquiredImage(d2, 3, GetImageMetadata("S2", 10, 20, "1x1", 0, 10, 100));

            return new List<AcquiredImage> {
                T3a1, T3a2, T3a3,
                T4a1, T4a2, T4a3,
                T5a1, T5a2, T5a3,
                T6a1, T6a2, T6a3,
                T3a4, T3a5, T3a6,
            };
        }


        private ImageMetadata GetImageMetadata(string filterName, int gain, int offset, string binning, int readoutMode, double rotation, double roi) {
            return new ImageMetadata() {
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

        private AcquiredImage GetAcquiredImage(DateTime dt, int targetId, ImageMetadata metadata) {
            return new AcquiredImage(metadata) {
                AcquiredDate = dt,
                FilterName = metadata.FilterName,
                TargetId = targetId
            };
        }
    }
}
