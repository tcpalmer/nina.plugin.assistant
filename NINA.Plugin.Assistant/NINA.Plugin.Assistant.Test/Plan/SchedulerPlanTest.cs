using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using FluentAssertions;
using Moq;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Test.Astrometry;
using NUnit.Framework;
using System;
using System.Xml.Linq;

namespace NINA.Plugin.Assistant.Test.Plan {

    [TestFixture]
    public class SchedulerPlanTest {

        [Test]
        public void testPlanExposureNoThrottle() {

            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 5, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(5);

            exposurePlan = GetExposurePlan(10, 10, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 20, 0);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(-1).Should().Be(0);
        }

        [Test]
        public void testPlanExposureThrottle() {

            IPlanTarget planTarget = PlanMocks.GetMockPlanTarget("target", TestUtil.M31).Object;
            ExposureTemplate exposureTemplate = GetExposureTemplate();

            ExposurePlan exposurePlan = GetExposurePlan(10, 0, 0);
            PlanExposure sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(100).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(100).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 50, 10);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(200).Should().Be(10);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(50).Should().Be(0);

            exposurePlan = GetExposurePlan(10, 50, 8);
            sut = new PlanExposure(planTarget, exposurePlan, exposureTemplate);
            sut.NeededExposures(200).Should().Be(12);
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
