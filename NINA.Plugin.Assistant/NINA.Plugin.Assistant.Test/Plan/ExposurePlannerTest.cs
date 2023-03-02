using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class ExposurePlannerTest {

        [Test]
        public void testTypical() {
            DateTime dateTime = DateTime.Now;
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNormal(dateTime);

            int nbExposures = 50;
            int nbExposureLength = 180;
            int wbExposures = 10;
            int wbExposureLength = 120;

            Mock<IPlanProject> pp = GetTestProject(dateTime, 0, nbExposures, nbExposureLength, wbExposures, wbExposureLength);
            IPlanTarget pt = pp.Object.Targets[0];

            TimeInterval window = new TimeInterval((DateTime)ntc.AstronomicalTwilightStart, (DateTime)ntc.AstronomicalTwilightEnd);
            List<IPlanInstruction> list = new ExposurePlanner(pt, window, ntc).Plan();
            AssertPlan(GetExpectedTestTypical(ntc), list);

            Assert.AreEqual(nbExposures, pt.ExposurePlans[0].PlannedExposures);
            Assert.AreEqual(nbExposures, pt.ExposurePlans[1].PlannedExposures);
            Assert.AreEqual(nbExposures, pt.ExposurePlans[2].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[3].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[4].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[5].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[6].PlannedExposures);
        }

        [Test]
        public void testTypicalFilterSwitch2() {
            DateTime dateTime = DateTime.Now;
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNormal(dateTime);

            int nbExposures = 50;
            int nbExposureLength = 180;
            int wbExposures = 10;
            int wbExposureLength = 120;

            Mock<IPlanProject> pp = GetTestProject(dateTime, 2, nbExposures, nbExposureLength, wbExposures, wbExposureLength);
            IPlanTarget pt = pp.Object.Targets[0];

            TimeInterval window = new TimeInterval((DateTime)ntc.AstronomicalTwilightStart, ((DateTime)ntc.NighttimeStart).AddMinutes(60));
            List<IPlanInstruction> list = new ExposurePlanner(pt, window, ntc).Plan();
            AssertPlan(GetExpectedTestTypicalFS2(ntc), list);

            Assert.AreEqual(11, pt.ExposurePlans[0].PlannedExposures);
            Assert.AreEqual(9, pt.ExposurePlans[1].PlannedExposures);
            Assert.AreEqual(8, pt.ExposurePlans[2].PlannedExposures);
            Assert.AreEqual(4, pt.ExposurePlans[3].PlannedExposures);
            Assert.AreEqual(4, pt.ExposurePlans[4].PlannedExposures);
            Assert.AreEqual(4, pt.ExposurePlans[5].PlannedExposures);
            Assert.AreEqual(4, pt.ExposurePlans[6].PlannedExposures);
        }

        [Test]
        public void testWindowNotFilled() {
            DateTime dateTime = DateTime.Now;
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNormal(dateTime);

            int nbExposures = 5;
            int nbExposureLength = 60;
            int wbExposures = 10;
            int wbExposureLength = 120;

            Mock<IPlanProject> pp = GetTestProject(dateTime, 0, nbExposures, nbExposureLength, wbExposures, wbExposureLength);
            IPlanTarget pt = pp.Object.Targets[0];

            TimeInterval window = new TimeInterval((DateTime)ntc.AstronomicalTwilightStart, ((DateTime)ntc.NighttimeStart).AddMinutes(60));
            List<IPlanInstruction> list = new ExposurePlanner(pt, window, ntc).Plan();
            AssertPlan(GetExpectedWindowNotFilled(ntc), list);

            Assert.AreEqual(nbExposures, pt.ExposurePlans[0].PlannedExposures);
            Assert.AreEqual(nbExposures, pt.ExposurePlans[1].PlannedExposures);
            Assert.AreEqual(nbExposures, pt.ExposurePlans[2].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[3].PlannedExposures);
            Assert.AreEqual(wbExposures, pt.ExposurePlans[4].PlannedExposures);
            Assert.AreEqual(9, pt.ExposurePlans[5].PlannedExposures);
            Assert.AreEqual(0, pt.ExposurePlans[6].PlannedExposures);
        }

        [Test]
        public void testNoNightAtDusk() {
            DateTime dateTime = DateTime.Now;
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNoNight(dateTime);

            int exposures = 20;
            int exposureLength = 180;

            Mock<IPlanProject> pp = GetHighLatitudeTestProject(dateTime, 0, exposures, exposureLength);
            IPlanTarget pt = pp.Object.Targets[0];

            TimeInterval window = new TimeInterval(ntc.CivilTwilightStart, ((DateTime)ntc.AstronomicalTwilightStart).AddHours(2));
            List<IPlanInstruction> list = new ExposurePlanner(pt, window, ntc).Plan();
            AssertPlan(GetExpectedTestNoNightAtDusk(ntc), list);

            Assert.AreEqual(exposures, pt.ExposurePlans[0].PlannedExposures);
            Assert.AreEqual(exposures, pt.ExposurePlans[1].PlannedExposures);
            Assert.AreEqual(exposures, pt.ExposurePlans[2].PlannedExposures);
            Assert.AreEqual(0, pt.ExposurePlans[3].PlannedExposures);
        }

        [Test]
        public void testNoNightAtDawn() {
            DateTime dateTime = DateTime.Now;
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNoNight(dateTime);

            int exposures = 20;
            int exposureLength = 180;

            Mock<IPlanProject> pp = GetHighLatitudeTestProject(dateTime, 0, exposures, exposureLength);
            IPlanTarget pt = pp.Object.Targets[0];

            TimeInterval window = new TimeInterval(dateTime.Date.AddDays(1).AddHours(4), ntc.CivilTwilightEnd);
            List<IPlanInstruction> list = new ExposurePlanner(pt, window, ntc).Plan();
            AssertPlan(GetExpectedTestNoNightAtDawn(ntc), list);

            Assert.AreEqual(exposures, pt.ExposurePlans[0].PlannedExposures);
            Assert.AreEqual(18, pt.ExposurePlans[1].PlannedExposures);
            Assert.AreEqual(0, pt.ExposurePlans[2].PlannedExposures);
            Assert.AreEqual(0, pt.ExposurePlans[3].PlannedExposures);
        }

        private Mock<IPlanProject> GetTestProject(DateTime dateTime, int filterSwitchFrequency, int nbExposures, int nbExposureLength, int wbExposures, int wbExposureLength) {
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNormal(dateTime);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp.Object.FilterSwitchFrequency = filterSwitchFrequency;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp, pt);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Ha", nbExposures, 0, nbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Astronomical;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("OIII", nbExposures, 0, nbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Astronomical;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("SII", nbExposures, 0, nbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Astronomical;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("L", wbExposures, 0, wbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nighttime;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("R", wbExposures, 0, wbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nighttime;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("G", wbExposures, 0, wbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nighttime;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("B", wbExposures, 0, wbExposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nighttime;
            PlanMocks.AddMockPlanFilter(pt, pf);

            return pp;
        }

        private Mock<IPlanProject> GetHighLatitudeTestProject(DateTime dateTime, int filterSwitchFrequency, int exposures, int exposureLength) {
            TestNighttimeCircumstances ntc = TestNighttimeCircumstances.GetNormal(dateTime);

            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", ProjectState.Active);
            pp.Object.FilterSwitchFrequency = filterSwitchFrequency;
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            PlanMocks.AddMockPlanTarget(pp, pt);

            Mock<IPlanExposure> pf = PlanMocks.GetMockPlanExposure("Civil", exposures, 0, exposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Civil;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("Nautical", exposures, 0, exposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nautical;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("Astro", exposures, 0, exposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Astronomical;
            PlanMocks.AddMockPlanFilter(pt, pf);
            pf = PlanMocks.GetMockPlanExposure("Night", exposures, 0, exposureLength);
            pf.Object.TwilightLevel = TwilightLevel.Nighttime;
            PlanMocks.AddMockPlanFilter(pt, pf);

            return pp;
        }

        private void AssertPlan(List<IPlanInstruction> expectedPlan, List<IPlanInstruction> actualPlan) {

            TestContext.WriteLine("EXPECTED:");
            for (int i = 0; i < expectedPlan.Count; i++) {
                TestContext.WriteLine($"{expectedPlan[i]}");
            }

            TestContext.WriteLine("\nACTUAL:");
            for (int i = 0; i < actualPlan.Count; i++) {
                TestContext.WriteLine($"{actualPlan[i]}");
            }

            Assert.AreEqual(expectedPlan.Count, actualPlan.Count);

            for (int i = 0; i < expectedPlan.Count; i++) {
                IPlanInstruction expected = expectedPlan[i];
                IPlanInstruction actual = actualPlan[i];
                //TestContext.Error.WriteLine($"i {i}");

                Assert.IsTrue(expected.GetType() == actual.GetType());

                if (expected is PlanMessage) {
                    continue;
                }

                if (expected is PlanSwitchFilter) {
                    Assert.AreEqual(expected.planFilter.FilterName, actual.planFilter.FilterName);
                    continue;
                }

                if (expected is PlanSetReadoutMode) {
                    Assert.AreEqual(expected.planFilter.ReadoutMode, actual.planFilter.ReadoutMode);
                    continue;
                }

                if (expected is PlanTakeExposure) {
                    Assert.AreEqual(expected.planFilter.FilterName, actual.planFilter.FilterName);
                    continue;
                }

                if (expected is PlanWait) {
                    Assert.AreEqual(((PlanWait)expected).waitForTime, ((PlanWait)actual).waitForTime);
                    continue;
                }

                throw new AssertionException($"unknown actual instruction type: {actual.GetType().FullName}");
            }
        }

        private List<IPlanInstruction> GetExpectedTestTypical(NighttimeCircumstances ntc) {
            List<IPlanInstruction> actual = new List<IPlanInstruction>();

            Mock<IPlanExposure> Ha = PlanMocks.GetMockPlanExposure("Ha", 10, 0, 180);
            Mock<IPlanExposure> OIII = PlanMocks.GetMockPlanExposure("OIII", 10, 0, 180);
            Mock<IPlanExposure> SII = PlanMocks.GetMockPlanExposure("SII", 10, 0, 180);
            Mock<IPlanExposure> L = PlanMocks.GetMockPlanExposure("L", 10, 0, 120);
            Mock<IPlanExposure> R = PlanMocks.GetMockPlanExposure("R", 10, 0, 120);
            Mock<IPlanExposure> G = PlanMocks.GetMockPlanExposure("G", 10, 0, 120);
            Mock<IPlanExposure> B = PlanMocks.GetMockPlanExposure("B", 10, 0, 120);

            actual.Add(new PlanMessage(""));
            actual.Add(new PlanMessage(""));

            AddActualExposures(actual, Ha.Object, 19);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, L.Object, 10);
            AddActualExposures(actual, R.Object, 10);
            AddActualExposures(actual, G.Object, 10);
            AddActualExposures(actual, B.Object, 10);
            AddActualExposures(actual, Ha.Object, 31);
            AddActualExposures(actual, OIII.Object, 50);
            AddActualExposures(actual, SII.Object, 32);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, SII.Object, 18);

            return actual;
        }

        private List<IPlanInstruction> GetExpectedTestTypicalFS2(NighttimeCircumstances ntc) {
            List<IPlanInstruction> actual = new List<IPlanInstruction>();

            Mock<IPlanExposure> Ha = PlanMocks.GetMockPlanExposure("Ha", 10, 0, 180);
            Mock<IPlanExposure> OIII = PlanMocks.GetMockPlanExposure("OIII", 10, 0, 180);
            Mock<IPlanExposure> SII = PlanMocks.GetMockPlanExposure("SII", 10, 0, 180);
            Mock<IPlanExposure> L = PlanMocks.GetMockPlanExposure("L", 10, 0, 120);
            Mock<IPlanExposure> R = PlanMocks.GetMockPlanExposure("R", 10, 0, 120);
            Mock<IPlanExposure> G = PlanMocks.GetMockPlanExposure("G", 10, 0, 120);
            Mock<IPlanExposure> B = PlanMocks.GetMockPlanExposure("B", 10, 0, 120);

            actual.Add(new PlanMessage(""));
            actual.Add(new PlanMessage(""));

            for (int i = 0; i < 3; i++) {
                AddActualExposures(actual, Ha.Object, 2);
                AddActualExposures(actual, OIII.Object, 2);
                AddActualExposures(actual, SII.Object, 2);
            }

            AddActualExposures(actual, Ha.Object, 1);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, L.Object, 2);
            AddActualExposures(actual, R.Object, 2);
            AddActualExposures(actual, G.Object, 2);
            AddActualExposures(actual, B.Object, 2);
            AddActualExposures(actual, Ha.Object, 2);
            AddActualExposures(actual, OIII.Object, 2);
            AddActualExposures(actual, SII.Object, 2);

            AddActualExposures(actual, L.Object, 2);
            AddActualExposures(actual, R.Object, 2);
            AddActualExposures(actual, G.Object, 2);
            AddActualExposures(actual, B.Object, 2);
            AddActualExposures(actual, Ha.Object, 2);
            AddActualExposures(actual, OIII.Object, 1);

            return actual;
        }

        private List<IPlanInstruction> GetExpectedWindowNotFilled(NighttimeCircumstances ntc) {
            List<IPlanInstruction> actual = new List<IPlanInstruction>();

            Mock<IPlanExposure> Ha = PlanMocks.GetMockPlanExposure("Ha", 10, 0, 180);
            Mock<IPlanExposure> OIII = PlanMocks.GetMockPlanExposure("OIII", 10, 0, 180);
            Mock<IPlanExposure> SII = PlanMocks.GetMockPlanExposure("SII", 10, 0, 180);
            Mock<IPlanExposure> L = PlanMocks.GetMockPlanExposure("L", 10, 0, 120);
            Mock<IPlanExposure> R = PlanMocks.GetMockPlanExposure("R", 10, 0, 120);
            Mock<IPlanExposure> G = PlanMocks.GetMockPlanExposure("G", 10, 0, 120);
            Mock<IPlanExposure> B = PlanMocks.GetMockPlanExposure("B", 10, 0, 120);

            actual.Add(new PlanMessage(""));
            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Ha.Object, 5);
            AddActualExposures(actual, OIII.Object, 5);
            AddActualExposures(actual, SII.Object, 5);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, L.Object, 10);
            AddActualExposures(actual, R.Object, 10);
            AddActualExposures(actual, G.Object, 9);

            return actual;
        }

        private List<IPlanInstruction> GetExpectedTestNoNightAtDusk(TestNighttimeCircumstances ntc) {
            List<IPlanInstruction> actual = new List<IPlanInstruction>();

            Mock<IPlanExposure> Civil = PlanMocks.GetMockPlanExposure("Civil", 10, 0, 180);
            Mock<IPlanExposure> Nautical = PlanMocks.GetMockPlanExposure("Nautical", 10, 0, 180);
            Mock<IPlanExposure> Astro = PlanMocks.GetMockPlanExposure("Astro", 10, 0, 180);

            actual.Add(new PlanMessage(""));
            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Civil.Object, 19);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Civil.Object, 1);
            AddActualExposures(actual, Nautical.Object, 18);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Nautical.Object, 2);
            AddActualExposures(actual, Astro.Object, 20);

            return actual;
        }

        private List<IPlanInstruction> GetExpectedTestNoNightAtDawn(TestNighttimeCircumstances ntc) {
            List<IPlanInstruction> actual = new List<IPlanInstruction>();

            Mock<IPlanExposure> Civil = PlanMocks.GetMockPlanExposure("Civil", 10, 0, 180);
            Mock<IPlanExposure> Nautical = PlanMocks.GetMockPlanExposure("Nautical", 10, 0, 180);

            actual.Add(new PlanMessage(""));
            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Civil.Object, 19);

            actual.Add(new PlanMessage(""));
            AddActualExposures(actual, Civil.Object, 1);
            AddActualExposures(actual, Nautical.Object, 18);

            return actual;
        }

        private void AddActualExposures(List<IPlanInstruction> actual, IPlanExposure planFilter, int count) {
            actual.Add(new PlanSwitchFilter(planFilter));
            actual.Add(new PlanSetReadoutMode(planFilter));
            for (int i = 0; i < count; i++) {
                actual.Add(new PlanTakeExposure(planFilter));
            }
        }

        private void DumpInstructions(List<IPlanInstruction> list) {
            foreach (IPlanInstruction instruction in list) {
                TestContext.WriteLine(instruction);
            }

            TestContext.WriteLine();
        }
    }

    class TestNighttimeCircumstances : NighttimeCircumstances {

        public TestNighttimeCircumstances(DateTime civilTwilightStart,
                                          DateTime? nauticalTwilightStart,
                                          DateTime? astronomicalTwilightStart,
                                          DateTime? nighttimeStart,
                                          DateTime? nighttimeEnd,
                                          DateTime? astronomicalTwilightEnd,
                                          DateTime? nauticalTwilightEnd,
                                          DateTime civilTwilightEnd) {

            this.CivilTwilightStart = civilTwilightStart;
            this.NauticalTwilightStart = nauticalTwilightStart;
            this.AstronomicalTwilightStart = astronomicalTwilightStart;
            this.NighttimeStart = nighttimeStart;
            this.NighttimeEnd = nighttimeEnd;
            this.AstronomicalTwilightEnd = astronomicalTwilightEnd;
            this.NauticalTwilightEnd = nauticalTwilightEnd;
            this.CivilTwilightEnd = civilTwilightEnd;
        }

        public static TestNighttimeCircumstances GetNormal(DateTime dateTime) {
            return new TestNighttimeCircumstances(
                            dateTime.Date.AddHours(18),
                            dateTime.Date.AddHours(19),
                            dateTime.Date.AddHours(20),
                            dateTime.Date.AddHours(21),
                            dateTime.Date.AddDays(1).AddHours(4),
                            dateTime.Date.AddDays(1).AddHours(5),
                            dateTime.Date.AddDays(1).AddHours(6),
                            dateTime.Date.AddDays(1).AddHours(7));
        }

        public static TestNighttimeCircumstances GetNoNight(DateTime dateTime) {
            return new TestNighttimeCircumstances(
                            dateTime.Date.AddHours(18),
                            dateTime.Date.AddHours(19),
                            dateTime.Date.AddHours(20),
                            null, null,
                            dateTime.Date.AddDays(1).AddHours(5),
                            dateTime.Date.AddDays(1).AddHours(6),
                            dateTime.Date.AddDays(1).AddHours(7));
        }

        public static TestNighttimeCircumstances GetNoAstronomical(DateTime dateTime) {
            return new TestNighttimeCircumstances(
                            dateTime.Date.AddHours(18),
                            dateTime.Date.AddHours(19),
                            null, null,
                            null, null,
                            dateTime.Date.AddDays(1).AddHours(6),
                            dateTime.Date.AddDays(1).AddHours(7));
        }

        public static TestNighttimeCircumstances GetNoNautical(DateTime dateTime) {
            return new TestNighttimeCircumstances(
                            dateTime.Date.AddHours(18),
                            null, null,
                            null, null,
                            null, null,
                            dateTime.Date.AddDays(1).AddHours(7));
        }
    }
}
