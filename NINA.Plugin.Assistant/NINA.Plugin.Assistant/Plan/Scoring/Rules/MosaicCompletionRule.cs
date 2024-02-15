namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public class MosaicCompletionRule : ScoringRule {
        public const string RULE_NAME = "Mosaic Completion";
        public const double DEFAULT_WEIGHT = 0 * WEIGHT_SCALE;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target based on percent complete relative to other targets in a mosaic.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            IPlanProject planProject = potentialTarget.Project;

            if (!planProject.IsMosaic || planProject.Targets.Count == 1) { return 0; }

            double sum = 0;
            ExposureCompletionHelper helper = planProject.ExposureCompletionHelper;

            foreach (IPlanTarget planTarget in planProject.Targets) {
                if (planTarget.DatabaseId != potentialTarget.DatabaseId) {
                    sum += helper.PercentComplete(planTarget) / 100;
                }
            }

            double averageCompletionRateOthers = sum / (planProject.Targets.Count - 1);
            double completionRatePotential = helper.PercentComplete(potentialTarget) / 100;

            if (completionRatePotential >= averageCompletionRateOthers) { return 0; }
            return averageCompletionRateOthers - completionRatePotential;
        }
    }
}