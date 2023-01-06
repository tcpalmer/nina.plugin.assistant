using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Assistant.NINAPlugin.Plan.ScoringEngine.Rules {

    public abstract class ScoringRule {

        public abstract string Name { get; }
        public abstract double DefaultWeight { get; }

        // TODO: add a static method that reflects and gets a list of ALL rules instantiated
        // can be used for default weights as well as ScoringEngine init

        /// <summary>
        /// Apply the rule and return a score in the range 0-1.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="potentialTarget"></param>
        /// <returns></returns>
        public abstract double Score(DateTime atTime, PlanTarget potentialTarget);

        // TODO: methods:
        // - get the associated project
        // - get twilight time for the day
        // - get transit time
        // - other astrometry
        // - access profile details (e.g. MF settings)

        /*
        Meridian window: assign a higher score to targets that would be taking exposures closer to the meridian sweet spot. Would need to compute a metric based on the target time span overlap with changing distance from the meridian (integrate).
        Time Limit: assign a higher score to targets that are setting ?soon?. This helps to avoid missing opportunities to image a target before it sets for the year.
        Season Limit: assign a higher score to targets that have shorter remaining imaging seasons.
        Meridian Flip: assign a lower score to targets that will require an immediate MF. Related: if a target is east of the meridian but ?close? (check NINA profile MF settings), don?t switch to it until it?s well past the meridian.
        Percent Complete: assign a higher score to targets that are closer to 100% complete to wrap them up.
        Switch penalty: assign a higher score to the current target (if it?s stop time is still in the future) and lower scores to others. This assigns some cost to the time to switch to another target (slew/center) and ideally prevents target thrashing.
        Mosaic completion priority: assign a higher score to mosaic targets that are closer to 100% complete to wrap them up.
        Mosaic balance priority: assign a higher score to mosaic targets that are closer to 0% complete to balance exposures across frames. (Obviously in conflict with Mosaic completion priority so only one should be used.)
         */

        public static Dictionary<string, ScoringRule> GetAllScoringRules() {

            IEnumerable<Type> ruleTypes = Assembly.GetAssembly(typeof(ScoringRule)).GetTypes().
                    Where(ruleType => ruleType.IsClass && !ruleType.IsAbstract && ruleType.IsSubclassOf(typeof(ScoringRule)));

            Dictionary<string, ScoringRule> list = new Dictionary<string, ScoringRule>();
            foreach (Type ruleType in ruleTypes) {
                ScoringRule rule = (ScoringRule)Activator.CreateInstance(ruleType);
                list.Add(rule.Name, rule);
            }

            return list;
        }
    }
}
