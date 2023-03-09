using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Database {

    public class AssistantPlanLoader {

        public List<IPlanProject> LoadActiveProjects(AssistantDatabaseContext context, IProfile activeProfile, DateTime atTime) {
            List<Project> projects = null;
            List<ExposureTemplate> exposureTemplates = null;
            string profileId = activeProfile.Id.ToString();

            using (context) {
                try {
                    projects = context.GetActiveProjects(profileId, atTime);
                    exposureTemplates = context.GetExposureTemplates(profileId);
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
            Dictionary<string, ExposureTemplate> exposureTemplatesDictionary = GetExposureTemplateDictionary(exposureTemplates);

            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(activeProfile, project, exposureTemplatesDictionary);
                planProjects.Add(planProject);
            }

            return planProjects;
        }

        private Dictionary<string, ExposureTemplate> GetExposureTemplateDictionary(List<ExposureTemplate> exposureTemplates) {
            Dictionary<string, ExposureTemplate> dict = new Dictionary<string, ExposureTemplate>();

            foreach (ExposureTemplate exposureTemplate in exposureTemplates) {
                dict.Add(exposureTemplate.FilterName, exposureTemplate);
            }

            return dict;
        }

    }
}
