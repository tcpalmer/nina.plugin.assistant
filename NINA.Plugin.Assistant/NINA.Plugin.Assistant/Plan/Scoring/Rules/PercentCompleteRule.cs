namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public class PercentCompleteRule : ScoringRule {
        public const string RULE_NAME = "Percent Complete";
        public const double DEFAULT_WEIGHT = .5 * WEIGHT_SCALE;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target based on percent complete.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            return potentialTarget.Project.ExposureCompletionHelper.PercentComplete(potentialTarget) / 100;
        }
    }
}