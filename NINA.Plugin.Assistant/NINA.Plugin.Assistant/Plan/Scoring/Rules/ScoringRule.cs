using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Assistant.NINAPlugin.Plan.Scoring.Rules {

    public interface IScoringRule {
        string Name { get; }
        double DefaultWeight { get; }

        double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget);
    }

    public abstract class ScoringRule : IScoringRule {
        public const double WEIGHT_SCALE = 100;

        public abstract string Name { get; }
        public abstract double DefaultWeight { get; }

        /// <summary>
        /// Apply the rule and return a score in the range 0-1.
        /// </summary>
        /// <param name="scoringEngine"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public abstract double Score(IScoringEngine scoringEngine, IPlanTarget potentialTarget);

        /* TODO: additional rules:
           - Meridian window: assign a higher score to targets that would be taking exposures closer to the meridian sweet spot. Would need to compute a metric based on the target time span overlap with changing distance from the meridian (integrate).
           - Meridian Flip: assign a lower score to targets that will require an immediate MF. Related: if a target is east of the meridian but ?close? (check NINA profile MF settings), don?t switch to it until it?s well past the meridian.
           - Mosaic completion priority: assign a higher score to mosaic targets that are closer to 100% complete to wrap them up.
           - Mosaic balance priority: assign a higher score to mosaic targets that are closer to 0% complete to balance exposures across frames. (Obviously in conflict with Mosaic completion priority so only one should be used.)

        Could have a rule that checks target visibility at TIME RANGE and de-weights targets that are blocked for some of that period (horizon tree problem)
         */

        public static Dictionary<string, IScoringRule> GetAllScoringRules() {
            IEnumerable<Type> ruleTypes = Assembly.GetAssembly(typeof(ScoringRule)).GetTypes().
                    Where(ruleType => ruleType.IsClass && !ruleType.IsAbstract && ruleType.IsSubclassOf(typeof(ScoringRule)));

            Dictionary<string, IScoringRule> map = new Dictionary<string, IScoringRule>();
            foreach (Type ruleType in ruleTypes) {
                ScoringRule rule = (ScoringRule)Activator.CreateInstance(ruleType);
                map.Add(rule.Name, rule);
            }

            return map;
        }
    }
}