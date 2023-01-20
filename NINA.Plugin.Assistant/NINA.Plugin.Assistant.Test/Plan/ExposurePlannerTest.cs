using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Moq;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class ExposurePlannerTest {

        [Test]
        public void testWHAT() {
            Mock<IPlanProject> pp = PlanMocks.GetMockPlanProject("pp1", Project.STATE_ACTIVE);
            Mock<IPlanTarget> pt = PlanMocks.GetMockPlanTarget("M42", TestUtil.M42);
            Mock<IPlanFilter> pf = PlanMocks.GetMockPlanFilter("Ha", 10, 0);
            PlanMocks.AddMockPlanFilter(pt, pf);
            PlanMocks.AddMockPlanTarget(pp, pt);
        }
    }
}
