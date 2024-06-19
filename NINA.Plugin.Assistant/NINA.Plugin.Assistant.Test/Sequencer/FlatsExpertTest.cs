using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sequencer;
using Assistant.NINAPlugin.Util;
using FluentAssertions;
using Moq;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Sequencer {

    public class FlatsExpertTest {

        [Test]
        public void TestGetNeededFlats() {
            DateTime baseDate = new DateTime(2023, 12, 1).AddHours(20);
            Mock<IProfile> profileMock = new Mock<IProfile>();
            profileMock.SetupProperty(m => m.Id, new Guid("01234567-0000-0000-0000-000000000000"));

            List<Target> cadenceTargets = new List<Target>();
            List<Target> completedTargets = new List<Target>();
            List<AcquiredImage> acquiredImages = new List<AcquiredImage>();
            List<FlatHistory> flatHistories = new List<FlatHistory>();

            Mock<FlatsExpert> mock = new Mock<FlatsExpert> { };
            mock.Setup(m => m.GetTargetsForPeriodicFlats(It.IsAny<IProfile>())).Returns(cadenceTargets);
            mock.Setup(m => m.GetTargetsForCompletionFlats(It.IsAny<IProfile>())).Returns(completedTargets);
            mock.Setup(m => m.GetAcquiredImages(It.IsAny<IProfile>())).Returns(acquiredImages);
            mock.Setup(m => m.GetFlatHistory(It.IsAny<Target>())).Returns(flatHistories);

            mock.Object.GetNeededFlats(profileMock.Object, baseDate).Count.Should().Be(0);

            acquiredImages.AddRange(GetTestAcquiredImages(baseDate, 1, 3, 1));

            // Cadence = 3
            cadenceTargets.Add(GetTestTarget(1, baseDate, 3));
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(0)).Count.Should().Be(0);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(1)).Count.Should().Be(0);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(2)).Count.Should().Be(0);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(3)).Count.Should().Be(3);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(4)).Count.Should().Be(3);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(5)).Count.Should().Be(3);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(6)).Count.Should().Be(6);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(7)).Count.Should().Be(6);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(8)).Count.Should().Be(6);
            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(9)).Count.Should().Be(9);

            // Skip for existing flat history
            FlatSpec fs1 = new FlatSpec("R", 10, 20, new BinningMode(1, 1), 0, 10, 100);
            FlatSpec fs2 = new FlatSpec("G", 10, 20, new BinningMode(1, 1), 0, 10, 100);
            FlatSpec fs3 = new FlatSpec("B", 10, 20, new BinningMode(1, 1), 0, 10, 100);
            DateTime lightSessionDate = new FlatsExpert().GetLightSessionDate(baseDate);
            flatHistories.Add(GetFlatHistory(1, lightSessionDate, 1, baseDate.AddMinutes(0), fs1));
            flatHistories.Add(GetFlatHistory(1, lightSessionDate, 1, baseDate.AddMinutes(1), fs2));
            flatHistories.Add(GetFlatHistory(1, lightSessionDate, 1, baseDate.AddMinutes(2), fs3));

            mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(9)).Count.Should().Be(6);

            // Check for completed project
            acquiredImages.AddRange(GetTestAcquiredImages(baseDate, 2, 1, 1));
            completedTargets.Add(GetTestTarget(2, baseDate, Project.FLATS_HANDLING_TARGET_COMPLETION));
            flatHistories.Clear();
            List<LightSession> lightSessions = mock.Object.GetNeededFlats(profileMock.Object, baseDate.AddDays(9));
            lightSessions.Count.Should().Be(12);
            lightSessions[0].TargetId.Should().Be(1); lightSessions[0].SessionId.Should().Be(1);
            lightSessions[1].TargetId.Should().Be(1); lightSessions[1].SessionId.Should().Be(1);
            lightSessions[2].TargetId.Should().Be(1); lightSessions[2].SessionId.Should().Be(1);
            lightSessions[3].TargetId.Should().Be(1); lightSessions[3].SessionId.Should().Be(2);
            lightSessions[4].TargetId.Should().Be(1); lightSessions[4].SessionId.Should().Be(2);
            lightSessions[5].TargetId.Should().Be(1); lightSessions[5].SessionId.Should().Be(2);
            lightSessions[6].TargetId.Should().Be(1); lightSessions[6].SessionId.Should().Be(3);
            lightSessions[7].TargetId.Should().Be(1); lightSessions[7].SessionId.Should().Be(3);
            lightSessions[8].TargetId.Should().Be(1); lightSessions[8].SessionId.Should().Be(3);

            lightSessions[9].TargetId.Should().Be(2); lightSessions[9].SessionId.Should().Be(1);
            lightSessions[10].TargetId.Should().Be(2); lightSessions[10].SessionId.Should().Be(1);
            lightSessions[11].TargetId.Should().Be(2); lightSessions[11].SessionId.Should().Be(1);
        }

        private List<AcquiredImage> GetTestAcquiredImages(DateTime baseDate, int targetId, int numSessions, int startSessionId) {
            List<AcquiredImage> list = new List<AcquiredImage>();

            DateTime running;
            int sid = startSessionId;

            for (int i = 0; i < numSessions; i++) {
                running = baseDate.AddDays(i);

                for (int j = 0; j < 3; j++) {
                    list.Add(GetAcquiredImage(running.AddMinutes(j), targetId, GetImageMetadata(sid, "R", 10, 20, "1x1", 0, 10, 100)));
                    list.Add(GetAcquiredImage(running.AddMinutes(j + 1), targetId, GetImageMetadata(sid, "G", 10, 20, "1x1", 0, 10, 100)));
                    list.Add(GetAcquiredImage(running.AddMinutes(j + 2), targetId, GetImageMetadata(sid, "B", 10, 20, "1x1", 0, 10, 100)));
                    running = running.AddMinutes(2);
                }

                sid++;
            }

            return list;
        }

        [Test]
        public void TestGetLightSessions() {
            DateTime d1 = DateTime.Now.Date.AddDays(-5).AddHours(23);

            AcquiredImage T1R1 = GetAcquiredImage(d1, 1, GetImageMetadata(1, "R", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1R2 = GetAcquiredImage(d1.AddMinutes(1), 1, GetImageMetadata(1, "R", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1R3 = GetAcquiredImage(d1.AddMinutes(2), 1, GetImageMetadata(1, "R", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G1 = GetAcquiredImage(d1.AddMinutes(3), 1, GetImageMetadata(1, "G", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G2 = GetAcquiredImage(d1.AddMinutes(4), 1, GetImageMetadata(1, "G", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B1 = GetAcquiredImage(d1.AddMinutes(5), 1, GetImageMetadata(1, "B", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B2 = GetAcquiredImage(d1.AddMinutes(6), 1, GetImageMetadata(1, "B", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B3 = GetAcquiredImage(d1.AddMinutes(7), 1, GetImageMetadata(1, "B", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B4 = GetAcquiredImage(d1.AddMinutes(8), 1, GetImageMetadata(1, "B", 10, 20, "1x1", 0, 10, 100));

            List<AcquiredImage> acquiredImages = new List<AcquiredImage> { T1R1, T1R2, T1R3, T1G1, T1G2, T1B1, T1B2, T1B3, T1B4 };

            DateTime na = DateTime.Now;
            Target t = GetTestTarget(1, na, 3);
            FlatsExpert sut = new FlatsExpert();

            List<LightSession> lightSessions = sut.GetLightSessions(t, acquiredImages);
            lightSessions.Count.Should().Be(3);
            AssertLightSession(sut, lightSessions[0], d1, 1, "R");
            AssertLightSession(sut, lightSessions[1], d1, 1, "G");
            AssertLightSession(sut, lightSessions[2], d1, 1, "B");

            DateTime d2 = d1.AddDays(1);
            AcquiredImage T1R4 = GetAcquiredImage(d2, 1, GetImageMetadata(2, "R", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G3 = GetAcquiredImage(d2.AddMinutes(1), 1, GetImageMetadata(2, "G", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G4 = GetAcquiredImage(d2.AddMinutes(2), 1, GetImageMetadata(2, "G", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G5 = GetAcquiredImage(d2.AddMinutes(3), 1, GetImageMetadata(2, "G", 10, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B5 = GetAcquiredImage(d2.AddMinutes(4), 1, GetImageMetadata(2, "B", 10, 20, "1x1", 0, 10, 100));
            acquiredImages.Add(T1R4);
            acquiredImages.Add(T1G3);
            acquiredImages.Add(T1G4);
            acquiredImages.Add(T1G5);
            acquiredImages.Add(T1B5);

            lightSessions = sut.GetLightSessions(t, acquiredImages);
            lightSessions.Count.Should().Be(6);
            AssertLightSession(sut, lightSessions[0], d1, 1, "R");
            AssertLightSession(sut, lightSessions[1], d1, 1, "G");
            AssertLightSession(sut, lightSessions[2], d1, 1, "B");
            AssertLightSession(sut, lightSessions[3], d2, 2, "R");
            AssertLightSession(sut, lightSessions[4], d2, 2, "G");
            AssertLightSession(sut, lightSessions[5], d2, 2, "B");

            // Gain changes on 2nd R exposure
            DateTime d3 = d1.AddDays(2);
            AcquiredImage T1R5 = GetAcquiredImage(d3, 1, GetImageMetadata(3, "R", 10, 20, "1x1", 0, 10, 100)); // old gain
            AcquiredImage T1R6 = GetAcquiredImage(d3.AddMinutes(1), 1, GetImageMetadata(3, "R", 15, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G6 = GetAcquiredImage(d3.AddMinutes(2), 1, GetImageMetadata(3, "G", 15, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G7 = GetAcquiredImage(d3.AddMinutes(3), 1, GetImageMetadata(3, "G", 15, 20, "1x1", 0, 10, 100));
            AcquiredImage T1G8 = GetAcquiredImage(d3.AddMinutes(4), 1, GetImageMetadata(3, "G", 15, 20, "1x1", 0, 10, 100));
            AcquiredImage T1B6 = GetAcquiredImage(d3.AddMinutes(5), 1, GetImageMetadata(3, "B", 15, 20, "1x1", 0, 10, 100));
            acquiredImages.Add(T1R5);
            acquiredImages.Add(T1R6);
            acquiredImages.Add(T1G6);
            acquiredImages.Add(T1G7);
            acquiredImages.Add(T1G8);
            acquiredImages.Add(T1B6);

            lightSessions = sut.GetLightSessions(t, acquiredImages);
            lightSessions.Count.Should().Be(10);
            AssertLightSession(sut, lightSessions[0], d1, 1, "R");
            AssertLightSession(sut, lightSessions[1], d1, 1, "G");
            AssertLightSession(sut, lightSessions[2], d1, 1, "B");
            AssertLightSession(sut, lightSessions[3], d2, 2, "R");
            AssertLightSession(sut, lightSessions[4], d2, 2, "G");
            AssertLightSession(sut, lightSessions[5], d2, 2, "B");
            AssertLightSession(sut, lightSessions[6], d3, 3, "R"); // R gain 10
            AssertLightSession(sut, lightSessions[7], d3, 3, "R"); // R gain 15
            AssertLightSession(sut, lightSessions[8], d3, 3, "G");
            AssertLightSession(sut, lightSessions[9], d3, 3, "B");

            lightSessions[6].FlatSpec.Gain.Should().Be(10);
            lightSessions[7].FlatSpec.Gain.Should().Be(15);
        }

        [Test]
        public void TestCullByCadencePeriod() {
            FlatsExpert sut = new FlatsExpert();
            List<LightSession> lightSessions = new List<LightSession>();

            DateTime baseDate = DateTime.Now.Date.AddDays(-5).AddHours(23);
            DateTime sd0 = sut.GetLightSessionDate(baseDate);
            DateTime sd1 = sut.GetLightSessionDate(baseDate.AddDays(1));
            DateTime sd2 = sut.GetLightSessionDate(baseDate.AddDays(2));
            DateTime sd3 = sut.GetLightSessionDate(baseDate.AddDays(3));

            FlatSpec fsR = new FlatSpec("R", 10, 20, new BinningMode(1, 1), 0, 10, 100);
            FlatSpec fsG = new FlatSpec("G", 10, 20, new BinningMode(1, 1), 0, 10, 100);
            FlatSpec fsB = new FlatSpec("B", 10, 20, new BinningMode(1, 1), 0, 10, 100);

            lightSessions.Add(new LightSession(1, sd0, 1, fsR));
            lightSessions.Add(new LightSession(1, sd0, 1, fsG));
            lightSessions.Add(new LightSession(1, sd0, 1, fsB));

            DateTime createDate = DateTime.Now.Date.AddDays(-5);

            // For cadence 1, nothing should ever be culled
            Target target = GetTestTarget(1, createDate, 1);
            List<LightSession> culled = sut.CullByCadencePeriod(target, lightSessions, createDate);
            culled.Count.Should().Be(3);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(10));
            culled.Count.Should().Be(3);

            // Cadence 2
            target = GetTestTarget(1, createDate, 2);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate);
            culled.Count.Should().Be(0);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(1));
            culled.Count.Should().Be(0);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(2));
            culled.Count.Should().Be(3);

            // Cadence 3
            target = GetTestTarget(1, createDate, 3);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate);
            culled.Count.Should().Be(0);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(1));
            culled.Count.Should().Be(0);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(2));
            culled.Count.Should().Be(0);
            culled = sut.CullByCadencePeriod(target, lightSessions, createDate.AddDays(3));
            culled.Count.Should().Be(3);
        }

        [Test]
        public void TestCullByFlatsHistory() {
            FlatsExpert sut = new FlatsExpert();

            DateTime baseDate = new DateTime(2023, 12, 1).AddHours(18);
            List<LightSession> sessions = new List<LightSession>();
            List<FlatHistory> history = new List<FlatHistory>();
            Target t = new Target() { Id = 1, Name = "Test" };

            DateTime sd1 = sut.GetLightSessionDate(baseDate);
            DateTime sd2 = sut.GetLightSessionDate(baseDate.AddDays(-2));
            FlatSpec fs1 = new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs2 = new FlatSpec("O3", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs3 = new FlatSpec("S2", 10, 20, new BinningMode(1, 1), 0, 12, 100);

            LightSession ls1 = new LightSession(1, sd1, 1, fs1);
            LightSession ls2 = new LightSession(1, sd1, 1, fs2);
            LightSession ls3 = new LightSession(1, sd1, 1, fs3);
            sessions = new List<LightSession>() { ls1, ls2, ls3 };

            // No history
            List<LightSession> list = sut.CullByFlatsHistory(t, sessions, history);
            list.Count.Should().Be(sessions.Count);
            list[0].Should().BeSameAs(ls1);
            list[1].Should().BeSameAs(ls2);
            list[2].Should().BeSameAs(ls3);

            FlatHistory fh1 = GetFlatHistory(1, sd1, 1, baseDate.AddMinutes(4), fs1);
            FlatHistory fh2 = GetFlatHistory(1, sd1, 1, baseDate.AddMinutes(4), fs2);
            history = new List<FlatHistory>() { fh1, fh2 };

            // Cull two
            list = sut.CullByFlatsHistory(t, sessions, history);
            list.Count.Should().Be(1);
            list[0].Should().BeSameAs(ls3);

            // But not if session date differs
            fh1 = GetFlatHistory(1, sd2, 1, baseDate.AddMinutes(4), fs1);
            fh2 = GetFlatHistory(1, sd2, 1, baseDate.AddMinutes(4), fs2);
            history = new List<FlatHistory>() { fh1, fh2 };
            list = sut.CullByFlatsHistory(t, sessions, history);
            list.Count.Should().Be(sessions.Count);

            // Or if target Id differs
            fh1 = GetFlatHistory(1, sd2, 1, baseDate.AddMinutes(4), fs1);
            fh2 = GetFlatHistory(1, sd2, 1, baseDate.AddMinutes(4), fs2);
            history = new List<FlatHistory>() { fh1, fh2 };
            list = sut.CullByFlatsHistory(t, sessions, history);
            list.Count.Should().Be(sessions.Count);
        }

        [Test]
        public void TestIsRequiredFlat() {
            FlatsExpert sut = new FlatsExpert();
            DateTime baseDate = new DateTime(2023, 12, 1).AddHours(18);
            List<FlatSpec> allTakenFlats = new List<FlatSpec>();

            FlatSpec fs1 = new FlatSpec("R", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs2 = new FlatSpec("G", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs3 = new FlatSpec("B", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            FlatSpec fs4 = new FlatSpec("Ha", 10, 20, new BinningMode(1, 1), 0, 12, 100);
            List<FlatSpec> targetTakenFlats = new List<FlatSpec> { fs1, fs2, fs3 };

            // Always repeat is false ...
            LightSession neededFlat = new LightSession(1, baseDate, 1, fs1);
            sut.IsRequiredFlat(false, neededFlat, targetTakenFlats, allTakenFlats).Should().BeFalse();
            neededFlat = new LightSession(1, baseDate, 1, fs2);
            sut.IsRequiredFlat(false, neededFlat, targetTakenFlats, allTakenFlats).Should().BeFalse();
            neededFlat = new LightSession(1, baseDate, 1, fs3);
            sut.IsRequiredFlat(false, neededFlat, targetTakenFlats, allTakenFlats).Should().BeFalse();

            neededFlat = new LightSession(1, baseDate, 1, fs4);
            sut.IsRequiredFlat(false, neededFlat, targetTakenFlats, allTakenFlats).Should().BeTrue();

            // Now fs4 is already taken for another target so can skip ...
            allTakenFlats.Add(fs4);
            sut.IsRequiredFlat(false, neededFlat, targetTakenFlats, allTakenFlats).Should().BeFalse();

            // ... but not if Always repeat is true ... have to retake even if already taken
            sut.IsRequiredFlat(true, neededFlat, targetTakenFlats, allTakenFlats).Should().BeTrue();
        }

        [Test]
        public void TestGetTrainedFlatExposureSetting() {
            Mock<IProfile> profileMock = GetMockProfile();

            FlatDeviceSettings flatDeviceSettings = new FlatDeviceSettings();
            profileMock.SetupProperty(m => m.FlatDeviceSettings, flatDeviceSettings);

            ObserveAllCollection<TrainedFlatExposureSetting> trainedFlats = new ObserveAllCollection<TrainedFlatExposureSetting>();
            flatDeviceSettings.TrainedFlatExposureSettings = trainedFlats;

            BinningMode binning = new BinningMode(1, 1);
            FlatsExpert sut = new FlatsExpert();
            FlatSpec flatSpec = new FlatSpec("Lum", 10, 20, binning, 0, 0, 100);

            sut.GetTrainedFlatExposureSetting(profileMock.Object, flatSpec).Should().BeNull();

            // Don't even consider gain or offet
            trainedFlats.Add(new TrainedFlatExposureSetting() { Filter = 0, Gain = 0, Offset = 0, Binning = binning, Time = 1, Brightness = 21 });
            var setting = sut.GetTrainedFlatExposureSetting(profileMock.Object, flatSpec);
            setting.Should().NotBeNull();
            setting.Filter.Should().Be(0);
            setting.Time.Should().Be(1);

            // Gain and offet 'not set'
            trainedFlats.Add(new TrainedFlatExposureSetting() { Filter = 0, Gain = -1, Offset = -1, Binning = binning, Time = 2, Brightness = 21 });
            setting = sut.GetTrainedFlatExposureSetting(profileMock.Object, flatSpec);
            setting.Should().NotBeNull();
            setting.Filter.Should().Be(0);
            setting.Time.Should().Be(2);

            // Exact match
            trainedFlats.Add(new TrainedFlatExposureSetting() { Filter = 0, Gain = 10, Offset = 20, Binning = binning, Time = 3, Brightness = 21 });
            setting = sut.GetTrainedFlatExposureSetting(profileMock.Object, flatSpec);
            setting.Should().NotBeNull();
            setting.Filter.Should().Be(0);
            setting.Time.Should().Be(3);
        }

        [Test]
        public void TestGetFilterPosition() {
            FlatsExpert sut = new FlatsExpert();
            sut.GetFilterPosition(GetMockProfile().Object, "Lum").Should().Be(0);
        }

        private Mock<IProfile> GetMockProfile() {
            Mock<IProfile> activeProfileMock = new Mock<IProfile>();
            activeProfileMock.SetupAllProperties();

            Mock<IFilterWheelSettings> filterWheelSettingsMock = new Mock<IFilterWheelSettings>();
            filterWheelSettingsMock.SetupAllProperties();

            ObserveAllCollection<FilterInfo> filterInfoList = new ObserveAllCollection<FilterInfo>();

            filterWheelSettingsMock.SetupProperty(m => m.FilterWheelFilters, filterInfoList);
            activeProfileMock.SetupProperty(m => m.FilterWheelSettings, filterWheelSettingsMock.Object);

            FilterInfo fi = new FilterInfo();
            fi.Name = "Lum";
            filterInfoList.Add(fi);

            return activeProfileMock;
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
        public void TestGetCurrentSessionId() {
            DateTime baseDate = new DateTime(2023, 12, 5).AddHours(18);
            int flatsHandling = 1;
            FlatsExpert sut = new FlatsExpert();

            sut.GetCurrentSessionId(null, baseDate).Should().Be(1);

            Project project = new Project() {
                CreateDate = baseDate,
                FlatsHandling = flatsHandling,
                RuleWeights = new List<RuleWeight>(),
                Targets = new List<Target>()
            };

            // Flats Handling = cadence 1
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-1);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-2);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(3);

            project.CreateDate = baseDate.AddDays(-3);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(4);

            // Flats Handling = cadence 2
            project.FlatsHandling = 2;
            project.CreateDate = baseDate;
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-1);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-2);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-3);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-4);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(3);

            // Flats Handling = target completion
            project.CreateDate = baseDate;
            project.FlatsHandling = Project.FLATS_HANDLING_TARGET_COMPLETION;
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-1);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-2);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(3);

            project.CreateDate = baseDate.AddDays(-3);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(4);

            // Flats Handling = immediate
            project.CreateDate = baseDate;
            project.FlatsHandling = Project.FLATS_HANDLING_IMMEDIATE;
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-1);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-2);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(3);

            project.CreateDate = baseDate.AddDays(-3);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(4);

            // Flats Handling = off
            project.CreateDate = baseDate;
            project.FlatsHandling = Project.FLATS_HANDLING_OFF;
            sut.GetCurrentSessionId(project, baseDate).Should().Be(1);

            project.CreateDate = baseDate.AddDays(-1);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(2);

            project.CreateDate = baseDate.AddDays(-2);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(3);

            project.CreateDate = baseDate.AddDays(-3);
            sut.GetCurrentSessionId(project, baseDate).Should().Be(4);

            project.FlatsHandling = 5;
            project.CreateDate = baseDate;
            int expectedSid = 1;
            for (int i = 0; i < 10; i++) {
                DateTime current = baseDate.AddDays(i);
                sut.GetCurrentSessionId(project, current).Should().Be(expectedSid);
                if (i == 4) { expectedSid++; }
            }

            DateTime newDate = baseDate.AddDays(10);
            expectedSid = 3;
            for (int i = 0; i < 10; i++) {
                DateTime current = newDate.AddDays(i);
                sut.GetCurrentSessionId(project, current).Should().Be(expectedSid);
                if (i == 4) { expectedSid++; }
            }

            project.FlatsHandling = 7;
            expectedSid = 1;
            for (int i = 0; i < 80; i++) {
                DateTime current = baseDate.AddDays(i);
                sut.GetCurrentSessionId(project, current).Should().Be(expectedSid);
                if (i % 7 == 6) { expectedSid++; }
            }

            // Test based on SRO database
            project.FlatsHandling = 7;
            project.CreateDate = new DateTime(2023, 5, 4).AddHours(18);
            TestContext.WriteLine($"CREATED: {Utils.FormatDateTimeFull(project.CreateDate)}");
            DateTime testDate = new DateTime(2023, 11, 15).AddHours(13);
            for (int i = 0; i < 32; i++) {
                TestContext.WriteLine($"{Utils.FormatDateTimeFull(testDate)}: {sut.GetCurrentSessionId(project, testDate)}");
                testDate = testDate.AddDays(1);
            }
        }

        [Test]
        [TestCase(null, "0000")]
        [TestCase(0, "0000")]
        [TestCase(1, "0001")]
        [TestCase(11, "0011")]
        [TestCase(111, "0111")]
        [TestCase(975, "0975")]
        [TestCase(1111, "1111")]
        [TestCase(11111, "11111")]
        public void TestFormatSessionIdentifier(int sessionId, string expected) {
            FlatsExpert sut = new FlatsExpert();
            sut.FormatSessionIdentifier(sessionId).Should().Be(expected);
        }

        [Test]
        public void TestOverloadTargetName() {
            FlatsExpert sut = new FlatsExpert();

            string overloaded = sut.GetOverloadTargetName(null, 43);
            overloaded.Should().Be($"{FlatsExpert.OVERLOAD_SEP}43");
            var result = sut.DeOverloadTargetName(overloaded);
            result.Item1.Should().Be("");
            result.Item2.Should().Be("43");

            overloaded = sut.GetOverloadTargetName("", 43);
            overloaded.Should().Be($"{FlatsExpert.OVERLOAD_SEP}43");
            result = sut.DeOverloadTargetName(overloaded);
            result.Item1.Should().Be("");
            result.Item2.Should().Be("43");

            overloaded = sut.GetOverloadTargetName("foo", 43);
            overloaded.Should().Be($"foo{FlatsExpert.OVERLOAD_SEP}43");
            result = sut.DeOverloadTargetName(overloaded);
            result.Item1.Should().Be("foo");
            result.Item2.Should().Be("43");
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

            ImageMetadata imageMetaData = GetImageMetadata(1, "Ha", 10, 20, "2x2", 0, 123.4, 89);
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

            imageMetaData = GetImageMetadata(1, "Ha", 10, 20, "2x2", 0, ImageMetadata.NO_ROTATOR_ANGLE, 89);
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

            LightSession ls1 = new LightSession(1, sd1, 1, fs1);
            ls1.Equals(ls1).Should().BeTrue();
            LightSession ls2 = new LightSession(1, sd1, 1, fs1);
            ls1.Equals(ls2).Should().BeTrue();

            ls2 = new LightSession(1, sd1, 2, fs1);
            ls1.Equals(ls2).Should().BeFalse();

            ls2 = new LightSession(2, sd1, 1, fs1);
            ls1.Equals(ls2).Should().BeFalse();
            ls2 = new LightSession(2, sd1, 1, fs2);
            ls1.Equals(ls2).Should().BeFalse();

            ls1 = new LightSession(1, sd2, 1, fs1);
            ls2 = new LightSession(1, sd2, 1, fs1);
            ls1.Equals(ls2).Should().BeTrue();
            ls2 = new LightSession(2, sd1, 1, fs1);
            ls1.Equals(ls2).Should().BeFalse();

            List<LightSession> list = new List<LightSession> { ls1, ls2 };
            list.Contains(ls1).Should().BeTrue();
            list.Contains(ls2).Should().BeTrue();
            LightSession ls3 = new LightSession(3, sd3, 1, fs3);
            list.Contains(ls3).Should().BeFalse();
            list.Add(ls3);
            list.Contains(ls3).Should().BeTrue();

            LightSession ls4 = new LightSession(1, sd1, 1, fs4);
            ls4.Equals(ls4).Should().BeTrue();
            LightSession ls5 = new LightSession(1, sd1, 1, fs4);
            ls4.Equals(ls5).Should().BeTrue();

            ls5 = new LightSession(2, sd1, 1, fs4);
            ls4.Equals(ls5).Should().BeFalse();

            ls4 = new LightSession(2, sd2, 1, fs4);
            ls5 = new LightSession(2, sd2, 1, fs4);
            ls4.Equals(ls5).Should().BeTrue();

            ls5 = new LightSession(2, sd1, 1, fs4);
            ls4.Equals(ls5).Should().BeFalse();

            ls5 = new LightSession(2, sd2, 1, fs5);
            ls4.Equals(ls5).Should().BeFalse();
        }

        private Target GetTestTarget(int id, DateTime createDate, int flatsHandling) {
            Project p1 = new Project("abcd-1234") {
                Name = "P1",
                State = ProjectState.Active,
                CreateDate = createDate,
                FlatsHandling = flatsHandling
            };

            Target t1 = new Target() { Id = id, Name = "T1" };
            t1.Project = p1;
            p1.Targets.Add(t1);
            return t1;
        }

        private void AssertLightSession(FlatsExpert sut, LightSession ls, DateTime expDate, int sid, string filter) {
            ls.SessionDate.Should().Be(sut.GetLightSessionDate(expDate));
            ls.SessionId.Should().Be(sid);
            ls.FlatSpec.FilterName.Should().Be(filter);
        }

        private ImageMetadata GetImageMetadata(int sessionId, string filterName, int gain, int offset, string binning, int readoutMode, double rotation, double roi) {
            return new ImageMetadata() {
                SessionId = sessionId,
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

        private FlatHistory GetFlatHistory(int targetId, DateTime lightSessionDate, int lightSessionId, DateTime flatsTakenDate, FlatSpec flatSpec) {
            return new FlatHistory() {
                TargetId = targetId,
                LightSessionDate = lightSessionDate,
                LightSessionId = lightSessionId,
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
    }
}