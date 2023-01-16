using Assistant.NINAPlugin.Database.Schema;

namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public class ProjectPriorityRule : ScoringRule {

        public const string RULE_NAME = "Project Priority";
        public const double DEFAULT_WEIGHT = 0.5;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target based on assigned priority.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            IPlanProject project = potentialTarget.Project;

            switch (project.Priority) {
                case Project.PRIORITY_LOW:
                    return 0;
                case Project.PRIORITY_NORMAL:
                    return 0.5;
                case Project.PRIORITY_HIGH:
                    return 1.0;
                default: return 0;
            }
        }

    }
}
