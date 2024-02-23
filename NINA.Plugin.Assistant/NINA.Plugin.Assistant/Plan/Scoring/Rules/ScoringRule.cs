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