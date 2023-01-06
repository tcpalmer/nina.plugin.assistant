using Assistant.NINAPlugin.Plan.ScoringEngine.Rules;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan.ScoringEngine {

    public class ScoringEngine {

        private DateTime atTime;
        private Dictionary<string, double> ruleWeights;

        List<ScoringRule> rules;

        public ScoringEngine(DateTime atTime, Dictionary<string, double> ruleWeights) {
            this.atTime = atTime;
            this.ruleWeights = ruleWeights;
            this.rules = AddRules();
        }

        private List<ScoringRule> AddRules() {
            List<ScoringRule> rules = new List<ScoringRule>();

            // TODO: rules will need optional injection of typical mediators: equipment, profile, etc
            //  Example is a rule using SQM from observing conditions device.
            //  See how NINA does it with plugin ctors or any sequence instruction ...

            ScoringRule rule = new ProjectPriorityRule();
            if (ruleWeights[rule.Name] > 0) {
                rules.Add(rule);
            }

            return rules;
        }

        public double ScoreTarget(PlanTarget planTarget) {
            double totalScore = 0;
            foreach (ScoringRule rule in rules) {
                double weight = ruleWeights[rule.Name];
                double score = rule.Score(atTime, planTarget);
                Logger.Debug($"scoring rule {rule.Name}: score={score}, weight={weight}");

                totalScore += weight * score;
            }

            return totalScore;
        }
    }

}
