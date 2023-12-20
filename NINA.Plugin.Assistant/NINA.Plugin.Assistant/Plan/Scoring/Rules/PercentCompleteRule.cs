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

            int desired = 0;
            int accepted = 0;
            bool imageGradingEnabled = potentialTarget.Project.EnableGrader;

            if (imageGradingEnabled) {
                foreach (IPlanExposure planFilter in potentialTarget.ExposurePlans) {
                    desired += planFilter.Desired;
                    accepted += planFilter.Accepted;
                }

                if (accepted > desired) {
                    accepted = desired;
                }

                return desired != 0 ? (double)accepted / (double)desired : 0;
            }

            // With grading off, we have to check acquired/desired relative to the throttle
            double throttle = scoringEngine.ProfilePreference.ExposureThrottle / 100;
            double completionTotal = 0;
            int count = 0;

            foreach (IPlanExposure planFilter in potentialTarget.ExposurePlans) {
                if (planFilter.Desired > 0) {
                    double completion = planFilter.Acquired / (planFilter.Desired * throttle);
                    completionTotal += completion < 1 ? completion : 1;
                    count++;
                }
            }

            return completionTotal / count;
        }
    }
}
