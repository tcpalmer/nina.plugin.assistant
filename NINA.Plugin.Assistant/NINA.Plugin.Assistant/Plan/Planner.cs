using Assistant.NINAPlugin.Database.Schema;
using System;

namespace Assistant.NINAPlugin.Plan {

    public sealed class Planner {

        private static readonly Planner instance = new Planner();

        private static int Calls = 0;

        public AssistantPlan GetPlan() {

            // TODO: hack to get a plan to test with.  This should always get fresh data from the DB and rerun the engine.

            if (Calls == 0) {
                Calls++;
                return GetPlanOne();
            }

            if (Calls == 1) {
                Calls++;
                return GetPlanTwo();
            }

            return null;
        }

        private AssistantPlan GetPlanOne() {
            DateTime start = DateTime.Now.AddSeconds(5);
            DateTime end = start.AddMinutes(10);

            AssistantPlan plan = new AssistantPlan(start, end);

            Target target = new Target();
            target.name = "Antares";
            target.ra = 16.5;
            target.dec = -26.45;
            target.rotation = 0;

            PlanTarget planTarget = new PlanTarget(start, end, target);

            ExposurePlan exposurePlan = new ExposurePlan();
            exposurePlan.filtername = "Ha";
            exposurePlan.filterpos = 5;
            exposurePlan.exposure = 6;
            exposurePlan.gain = 100;
            PlanExposure planExposure = new PlanExposure(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filtername = "OIII";
            exposurePlan.filterpos = 7;
            exposurePlan.exposure = 6;
            exposurePlan.gain = 100;
            planExposure = new PlanExposure(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            plan.SetTarget(planTarget);

            return plan;

        }

        private AssistantPlan GetPlanTwo() {
            DateTime start = DateTime.Now.AddSeconds(5);
            DateTime end = start.AddMinutes(10);

            AssistantPlan plan = new AssistantPlan(start, end);

            Target target = new Target();
            target.name = "M 42";
            target.ra = 5.5;
            target.dec = -15.0;
            target.rotation = 0;

            PlanTarget planTarget = new PlanTarget(start, end, target);

            ExposurePlan exposurePlan = new ExposurePlan();
            exposurePlan.filtername = "R";
            exposurePlan.filterpos = 2;
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            PlanExposure planExposure = new PlanExposure(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filtername = "G";
            exposurePlan.filterpos = 3;
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            planExposure = new PlanExposure(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filtername = "B";
            exposurePlan.filterpos = 4;
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            planExposure = new PlanExposure(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            plan.SetTarget(planTarget);

            return plan;

        }

        static Planner() { }
        private Planner() { }

        public static Planner Instance {
            get { return instance; }
        }
    }

}
