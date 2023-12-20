using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring.Rules;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan.Scoring {

    public interface IScoringEngine {
        IProfile ActiveProfile { get; }
        ProfilePreference ProfilePreference { get; set; }
        DateTime AtTime { get; }
        IPlanTarget PreviousPlanTarget { get; }
        Dictionary<string, double> RuleWeights { get; set; }
        List<IScoringRule> Rules { get; set; }

        double ScoreTarget(IPlanTarget planTarget);
    }

    public class ScoringEngine : IScoringEngine {

        public IProfile ActiveProfile { get; }
        public ProfilePreference ProfilePreference { get; set; }
        public DateTime AtTime { get; }
        public IPlanTarget PreviousPlanTarget { get; }
        public Dictionary<string, double> RuleWeights { get; set; }
        public List<IScoringRule> Rules { get; set; }

        public ScoringEngine(IProfile activeProfile, ProfilePreference ProfilePreference, DateTime atTime, IPlanTarget previousPlanTarget) {
            this.ActiveProfile = activeProfile;
            this.ProfilePreference = ProfilePreference;
            this.PreviousPlanTarget = previousPlanTarget;
            this.AtTime = atTime;
        }

        public double ScoreTarget(IPlanTarget planTarget) {
            if (Rules == null) {
                Rules = AddAllRules(RuleWeights);
            }

            planTarget.ScoringResults = new ScoringResults();

            double totalScore = 0;
            foreach (ScoringRule rule in Rules) {
                double weight = RuleWeights[rule.Name] / ScoringRule.WEIGHT_SCALE;
                double score = rule.Score(this, planTarget);
                totalScore += weight * score;
                planTarget.ScoringResults.AddRuleResult(new RuleResult(rule, weight, score));
            }

            planTarget.ScoringResults.TotalScore = totalScore;
            return totalScore;
        }

        public List<IScoringRule> AddAllRules(Dictionary<string, double> ruleWeights) {
            Dictionary<string, IScoringRule> allRules = ScoringRule.GetAllScoringRules();
            List<IScoringRule> activeRules = new List<IScoringRule>();

            foreach (KeyValuePair<string, IScoringRule> item in allRules) {
                if (ruleWeights[item.Value.Name] > 0) {
                    activeRules.Add(item.Value);
                }
            }

            return activeRules;
        }
    }

}
