using System;

namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public class SettingSoonestRule : ScoringRule {

        public const string RULE_NAME = "Setting Soonest";
        public const double DEFAULT_WEIGHT = .5 * WEIGHT_SCALE;

        public override string Name { get { return RULE_NAME; } }
        public override double DefaultWeight { get { return DEFAULT_WEIGHT; } }

        /// <summary>
        /// Score the potential target based on the end time, normalized to previous noon to next noon.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public override double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget) {
            DateTime previousNoon, nextNoon;
            (previousNoon, nextNoon) = GetNoons(potentialTarget.EndTime);
            double totalSeconds = (nextNoon - previousNoon).TotalSeconds;
            double endSeconds = (potentialTarget.EndTime - previousNoon).TotalSeconds;
            return 1 - (endSeconds / totalSeconds);
        }

        private (DateTime, DateTime) GetNoons(DateTime endTime) {
            DateTime dt = endTime.Date.AddHours(12);
            return (dt < endTime) ? (dt, dt.AddDays(1)) : (dt.AddDays(-1), dt);
        }
    }
}
