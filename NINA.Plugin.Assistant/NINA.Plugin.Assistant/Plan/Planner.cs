using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class Planner {

        private DateTime forDateTime;
        private IProfile activeProfile;

        public AssistantPlan GetPlan(DateTime forDateTime, IProfileService profileService) {
            this.forDateTime = forDateTime;
            activeProfile = profileService.ActiveProfile;

            List<PlanProject> projects = GetActiveProjects(forDateTime, activeProfile.Id.ToString());

            projects = FilterPass1(projects);
            projects = FilterPass2(projects);
            PlanTarget planTarget = FilterPass3(projects);
            planTarget = FilterPass4(planTarget);


            return planTarget != null ? new AssistantPlan(planTarget, null) : null;
        }

        private List<PlanProject> FilterPass1(List<PlanProject> projects) {
            if (projects?.Count == 0) {
                return null;
            }

            List<PlanProject> filtered = new List<PlanProject>();
            foreach (PlanProject planProject in projects) {
                foreach (PlanTarget planTarget in planProject.Targets) {
                    int twilightInclude = GetOverallTwilight(planTarget);

                    Tuple<DateTime, DateTime> tuple = AstrometryUtils.GetImagingWindow(forDateTime, planTarget, activeProfile, planProject.HorizonDefinition, twilightInclude);
                }

                /*

        For each active project:
            For each incomplete target, based on date and location:
                Find most inclusive twilight over all incomplete exposure plans.
                Is it visible (above horizon) for that begin/end twilight period?
                Is the time-on-target > minimum imaging time?
                If yes to all, add to potential target list

                 */
            }

            return filtered;
        }

        private List<PlanProject> FilterPass2(List<PlanProject> projects) {
            if (projects?.Count == 0) {
                return null;
            }

            /*
    Pass 2 applies the Moon Avoidance formula for each filter of each potential target, removing those filter plans that fail the check.

    For each potential target:
        For each incomplete filter plan:
            If enabled, determine moon avoidance - acceptable? Moon position can be calculated based on the midpoint of start/end times for this target.
            If yes, add to list of filter plans for this target

    If all filter plans for a target were culled, remove it from the potential targets list. Revise the hard start/stop times based on the final set of
    filter plans since it may shift with different twilight preferences per remaining filters.             */
            throw new NotImplementedException();
        }

        private PlanTarget FilterPass3(List<PlanProject> projects) {
            if (projects?.Count == 0) {
                return null;
            }

            /*
             * Apply the scoring engine ...
             * Also have to set the hard stop time here or maybe in pass 4
             */

            throw new NotImplementedException();
        }

        private PlanTarget FilterPass4(PlanTarget planTarget) {
            if (planTarget == null) {
                return null;
            }

            /*
    Pass 4 refines the order of filters/exposures for the selected target:

    - If the time span on the target includes twilight, order so that exposures with more restrictive twilight are not
      scheduled during those periods. For example a NB filter set to image at astro twilight could be imaged during that time
      at dusk and dawn, but a WB filter could not and would require nighttime darkness before imaging.
    - Consideration may also be given to prioritizing filters with lower percent complete so that overall acquisition on a target is balanced.

      We also have to set the # of PlannedExposures on each PlanFilter based on what we can get done
             */

            throw new NotImplementedException();
        }

        private int GetOverallTwilight(PlanTarget planTarget) {
            int twilightInclude = 0;
            foreach (PlanFilter planFilter in planTarget.FilterPlans) {
                // find most permissive (brightest) twilight over all plans
                if (planFilter.Preferences.TwilightInclude > twilightInclude) {
                    twilightInclude = planFilter.Preferences.TwilightInclude;
                }
            }

            return twilightInclude;
        }

        private List<PlanProject> GetActiveProjects(DateTime forDateTime, string profileId) {
            List<PlanProject> planProjects = new List<PlanProject>();
            List<Project> projects = null;

            AssistantDatabaseInteraction database = new AssistantDatabaseInteraction();
            using (var context = database.GetContext()) {
                try {
                    projects = context.GetActiveProjects(profileId, forDateTime);
                }
                catch (Exception ex) {
                    Logger.Error($"exception accessing Assistant database: {ex}");
                    // TODO: need to throw so instruction can react properly
                    // maybe: new SequenceEntityFailedException("");
                }
            }

            if (projects?.Count == 0) {
                return null;
            }

            foreach (Project project in projects) {
                // TODO:
                // create plan counterpart DTOs and add to planProjects
                //   BUT cull exposure plans if complete, and whole target/project if all are complete
            }

            return planProjects;
        }

        /*
        private bool ProjectIsComplete(PlanProject planProject) {
            foreach (PlanTarget target in planProject.Targets) {
                foreach (PlanExposure planExposure in target.FilterPlans) {
                    if (planExposure.Accepted < planExposure.Desired) {
                        return false;
                    }
                }
            }

            return true;
        }
         */

        /*
        private AssistantPlan GetPlanOne() {
            DateTime start = DateTime.Now.AddSeconds(5);
            DateTime end = start.AddMinutes(10);

            AssistantPlan plan = new AssistantPlan(start, end);

            Target target = new Target();
            target.name = "Antares";
            target.ra = 16.5;
            target.dec = -26.45;
            target.rotation = 0;

            PlanTarget planTarget = new PlanTarget(start, end, target);

            ExposurePlan exposurePlan = new ExposurePlan();
            exposurePlan.filterName = "Ha";
            exposurePlan.exposure = 6;
            exposurePlan.gain = 100;
            PlanExposureOLD planExposure = new PlanExposureOLD(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filterName = "OIII";
            exposurePlan.exposure = 6;
            exposurePlan.gain = 100;
            planExposure = new PlanExposureOLD(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            plan.SetTarget(planTarget);

            return plan;

        }

        private AssistantPlanOLD GetPlanTwo() {
            DateTime start = DateTime.Now.AddSeconds(5);
            DateTime end = start.AddMinutes(10);

            AssistantPlanOLD plan = new AssistantPlanOLD(start, end);

            Target target = new Target();
            target.name = "M 42";
            target.ra = 5.5;
            target.dec = -15.0;
            target.rotation = 0;

            PlanTargetOLD planTarget = new PlanTargetOLD(start, end, target);

            ExposurePlan exposurePlan = new ExposurePlan();
            exposurePlan.filterName = "R";
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            PlanExposureOLD planExposure = new PlanExposureOLD(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filterName = "G";
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            planExposure = new PlanExposureOLD(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            exposurePlan = new ExposurePlan();
            exposurePlan.filterName = "B";
            exposurePlan.exposure = 4;
            exposurePlan.gain = 100;
            planExposure = new PlanExposureOLD(start, end, 3, exposurePlan);
            planTarget.AddExposurePlan(planExposure);

            plan.SetTarget(planTarget);

            return plan;

        }*/

    }

}
