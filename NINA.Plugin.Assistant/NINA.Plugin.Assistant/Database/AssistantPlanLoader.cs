using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Database {

    public class AssistantPlanLoader {

        public List<IPlanProject> LoadActiveProjects(AssistantDbContext context, IProfile activeProfile, DateTime atTime) {
            List<Project> projects = null;
            List<FilterPreference> filterPrefs = null;
            string profileId = activeProfile.Id.ToString();

            using (context) {
                try {
                    projects = context.GetActiveProjects(profileId, atTime);
                    filterPrefs = context.GetFilterPreferences(profileId);
                }
                catch (Exception ex) {
                    throw ex; // let the caller decide how to handle
                }
            }

            if (projects == null || projects.Count == 0) {
                Logger.Warning("Assistant: no projects are active and within start/end dates at planning time");
                return null;
            }

            List<IPlanProject> planProjects = new List<IPlanProject>();
            Dictionary<string, FilterPreference> filterPrefsDictionary = GetFilterPrefDictionary(filterPrefs);

            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(activeProfile, project, filterPrefsDictionary);
                planProjects.Add(planProject);
            }

            return planProjects;

        }

        private Dictionary<string, FilterPreference> GetFilterPrefDictionary(List<FilterPreference> filterPrefs) {
            Dictionary<string, FilterPreference> dict = new Dictionary<string, FilterPreference>();

            foreach (FilterPreference filterPref in filterPrefs) {
                dict.Add(filterPref.FilterName, filterPref);
            }

            return dict;
        }

    }
}
