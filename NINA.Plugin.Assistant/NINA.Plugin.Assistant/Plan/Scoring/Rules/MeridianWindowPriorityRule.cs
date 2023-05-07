using System;

namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public class MeridianWindowPriorityRule : ScoringRule {

        public const string RULE_NAME = "Meridian Window Priority";
        public const double DEFAULT_WEIGHT = .75 * WEIGHT_SCALE;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target on whether it uses a meridian window or not.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            IPlanProject project = potentialTarget.Project;
            return project.MeridianWindow > 0 ? 1 : 0;
        }

    }
}
