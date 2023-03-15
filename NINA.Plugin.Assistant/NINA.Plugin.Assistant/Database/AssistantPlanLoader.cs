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
            string profileId = activeProfile.Id.ToString();

            using (context) {
                try {
                    projects = context.GetActiveProjects(profileId, atTime);
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
            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(activeProfile, project);
                planProjects.Add(planProject);
            }

            return planProjects;
        }

    }
}
