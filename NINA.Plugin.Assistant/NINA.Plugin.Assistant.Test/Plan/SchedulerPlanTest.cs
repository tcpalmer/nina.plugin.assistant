using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class SchedulerPlanTest {

        [Test]
        public void TestPlanExposureNoThrottle() {
            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(10);

            exposurePlan = GetExposurePlan(10, 5, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(5);

            exposurePlan = GetExposurePlan(10, 10, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(0);

            exposurePlan = GetExposurePlan(10, 20, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(0);
        }

        [Test]
        public void TestPlanExposureThrottle() {
            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            planTarget.Project.ExposureCompletionHelper = new ExposureCompletionHelper(false, 100);
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(0);

            planTarget.Project.ExposureCompletionHelper = new ExposureCompletionHelper(false, 200);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(12);

            planTarget.Project.ExposureCompletionHelper = new ExposureCompletionHelper(false, 50);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures().Should().Be(0);
        }

        private ExposurePlan GetExposurePlan(int desired, int accepted, int acquired) {
            return new ExposurePlan {
                Desired = desired,
                Accepted = accepted,
                Acquired = acquired
            };
        }

        private ExposureTemplate GetExposureTemplate() {
            return new ExposureTemplate {
                BinningMode = new BinningMode(1, 1)
            };
        }
    }
}