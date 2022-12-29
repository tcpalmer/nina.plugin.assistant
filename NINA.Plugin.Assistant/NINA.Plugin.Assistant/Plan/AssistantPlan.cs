using Assistant.NINAPlugin.Database.Schema;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class AssistantPlan {

        public string Id { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public PlanTarget PlanTarget { get; private set; }

        public AssistantPlan(DateTime startTime, DateTime endTime) {
            Id = Guid.NewGuid().ToString();
            TimeInterval = new TimeInterval(startTime, endTime);
        }

        public void SetTarget(PlanTarget planTarget) {
            PlanTarget = planTarget;
        }
    }

    /*
     * Operations:
     * - At the given time, return a list of targets that could be imaging (but that's a need during engine run ...)
     * - 
     */

    public class PlanTarget {

        public string Id { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public Target Target { get; private set; }
        public List<PlanExposure> PlanExposures { get; private set; }

        public PlanTarget(DateTime startTime, DateTime endTime, Target target) {
            Id = Guid.NewGuid().ToString();
            TimeInterval = new TimeInterval(startTime, endTime);
            Target = target;
            PlanExposures = new List<PlanExposure>();
        }

        public void AddExposurePlan(PlanExposure planExposure) {
            PlanExposures.Add(planExposure);
        }
    }

    public class PlanExposure {

        public string Id { get; private set; }
        public TimeInterval TimeInterval { get; private set; }
        public ExposurePlan ExposurePlan { get; private set; }
        public int Exposures { get; private set; }

        public PlanExposure(DateTime startTime, DateTime endTime, int exposures, ExposurePlan exposurePlan) {
            Id = Guid.NewGuid().ToString();
            TimeInterval = new TimeInterval(startTime, endTime);
            Exposures = exposures;
            ExposurePlan = exposurePlan;
        }
    }

}
