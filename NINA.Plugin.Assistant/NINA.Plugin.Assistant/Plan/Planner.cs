using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class Planner {

        private DateTime atTime;
        private IProfile activeProfile;
        private IPlanTarget previousPlanTarget;
        private ObserverInfo observerInfo;

        public Planner(DateTime atTime, IProfileService profileService) {
            this.atTime = atTime;
            this.activeProfile = profileService.ActiveProfile;
            this.observerInfo = new ObserverInfo {
                Latitude = activeProfile.AstrometrySettings.Latitude,
                Longitude = activeProfile.AstrometrySettings.Longitude,
                Elevation = activeProfile.AstrometrySettings.Elevation,
            };

            if (AstrometryUtils.IsAbovePolarCircle(observerInfo)) {
                throw new Exception("Assistant: observer location is above a polar circle - not supported");
            }
        }

        public AssistantPlan GetPlan(IPlanTarget previousPlanTarget) {
            this.previousPlanTarget = previousPlanTarget;
            Logger.Debug($"Assistant: getting current plan for {atTime}");

            using (MyStopWatch.Measure("Assistant Plan Generation")) {

                List<IPlanProject> projects = GetProjects(atTime, activeProfile.Id.ToString());

                projects = FilterForIncomplete(projects);
                //Logger.Trace($"Assistant: GetPlan after FilterForIncomplete:\n{PlanProject.ListToString(projects)}");

                projects = FilterForVisibility(projects);
                //Logger.Trace($"Assistant: GetPlan after FilterForVisibility:\n{PlanProject.ListToString(projects)}");

                projects = FilterForMoonAvoidance(projects);
                Logger.Trace($"Assistant: GetPlan after FilterForMoonAvoidance:\n{PlanProject.ListToString(projects)}");

                ScoringEngine scoringEngine = new ScoringEngine(activeProfile, atTime, previousPlanTarget);
                IPlanTarget planTarget = SelectTargetByScore(projects, scoringEngine);
                if (planTarget != null) {
                    Logger.Trace($"Assistant: GetPlan highest scoring target:\n{planTarget}");
                }
                else {
                    Logger.Trace("Assistant: GetPlan no target selected by score");
                }

                planTarget = PlanExposures(planTarget);

                return planTarget != null ? new AssistantPlan(planTarget, GetTargetTimeInterval(atTime, planTarget)) : null;
            }
        }

        /// <summary>
        /// Review the project list and reject those projects that are already complete.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForIncomplete(List<IPlanProject> projects) {
            if (projects == null || projects.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (!ProjectIsInComplete(planProject)) {
                    SetRejected(planProject, Reasons.ProjectComplete);
                }
            }

            return PropagateRejections(projects);
        }

        /// <summary>
        /// Review each project and the list of associated targets: reject those targets that are not visible.  If all targets
        /// for the project are rejected, mark the project rejected too.  A target is visible if it is above the horizon
        /// within the time window set by the most inclusive twilight over all incomplete exposure plans for that target AND that
        /// visible time is greater than the minimum imaging time preference for the project.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForVisibility(List<IPlanProject> projects) {
            if (projects == null || projects.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {

                foreach (IPlanTarget planTarget in planProject.Targets) {

                    if (!AstrometryUtils.RisesAtLocation(observerInfo, planTarget.Coordinates)) {
                        Logger.Warning($"Assistant: target {planProject.Name}/{planTarget.Name} never rises at location - skipping");
                        SetRejected(planTarget, Reasons.TargetNeverRises);
                        continue;
                    }

                    // Get the most inclusive twilight over all incomplete exposure plans
                    NighttimeCircumstances nighttimeCircumstances = new NighttimeCircumstances(observerInfo, atTime);
                    Tuple<DateTime, DateTime> twilightSpan = nighttimeCircumstances.GetTwilightSpan(GetOverallTwilight(planTarget));
                    Logger.Trace($"*** Twilight span {twilightSpan.Item1} - {twilightSpan.Item2}");

                    // Determine the potential imaging time span
                    TargetCircumstances targetCircumstances = new TargetCircumstances(planTarget.Coordinates, observerInfo, planProject.HorizonDefinition, twilightSpan);

                    DateTime actualStart = atTime > targetCircumstances.RiseAboveHorizonTime ? atTime : targetCircumstances.RiseAboveHorizonTime;
                    int TimeOnTargetSeconds = (int)(targetCircumstances.SetBelowHorizonTime - actualStart).TotalSeconds;
                    Logger.Trace($"Assistant: TargetCircumstances:\n{targetCircumstances}, timeOnTarget={TimeOnTargetSeconds}");

                    // If the target is visible for at least the minimum time, then accept it
                    if (targetCircumstances.IsVisible && (TimeOnTargetSeconds >= planProject.Preferences.MinimumTime * 60)) {
                        planTarget.SetCircumstances(targetCircumstances);
                    }
                    else {
                        SetRejected(planTarget, Reasons.TargetNotVisible);
                    }
                }
            }

            return PropagateRejections(projects);
        }

        /// <summary>
        /// Review each project and the list of associated targets.  For each filter plan where moon avoidance is enabled,
        /// determine the moon avoidance separation to the target at the midpoint of the imaging window for that target.
        /// If the separation is less than the preference minimum, reject the filter plan.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForMoonAvoidance(List<IPlanProject> projects) {
            if (projects == null || projects.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                        if (planFilter.IsIncomplete() && planFilter.Preferences.MoonAvoidanceEnabled) {
                            if (RejectForMoonAvoidance(planTarget, planFilter)) {
                                SetRejected(planFilter, Reasons.FilterMoonAvoidance);
                            }
                        }
                    }
                }
            }

            return PropagateRejections(projects);
        }

        public IPlanTarget SelectTargetByScore(List<IPlanProject> projects, IScoringEngine scoringEngine) {
            if (projects == null || projects.Count == 0) {
                return null;
            }

            IPlanTarget highScoreTarget = null;
            double highScore = 0;

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                scoringEngine.RuleWeights = planProject.Preferences.RuleWeights;

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    Logger.Debug($"Assistant: running scoring engine for project/target {planProject.Name}/{planTarget.Name}");
                    double score = scoringEngine.ScoreTarget(planTarget);
                    if (score > highScore) {
                        highScoreTarget = planTarget;
                        highScore = score;
                    }
                }
            }

            return highScoreTarget;
        }

        public IPlanTarget PlanExposures(IPlanTarget planTarget) {
            if (planTarget == null) {
                return null;
            }

            /* TODO: for now:
             * - don't worry about twilight/filter interference
             * - just let each filter get max it can until window is filled
             */

            DateTime actualStartTime = planTarget.StartTime < DateTime.Now ? DateTime.Now : planTarget.StartTime;
            int timeAvailable = (int)(planTarget.EndTime - actualStartTime).TotalSeconds;
            int timeUsed = 0;

            foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                if (planFilter.Rejected) { continue; }

                int maxPossibleExposures = (int)((timeAvailable - timeUsed) / planFilter.ExposureLength);
                planFilter.PlannedExposures = Math.Min(planFilter.NeededExposures, maxPossibleExposures);
                timeUsed += planFilter.PlannedExposures * (int)planFilter.ExposureLength;

                if (timeUsed >= timeAvailable) {
                    break;
                }
            }

            // Reject any that don't have exposures planned
            foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                if (planFilter.Rejected) { continue; }

                if (planFilter.PlannedExposures == 0) {
                    planFilter.Rejected = true;
                    planFilter.RejectedReason = Reasons.FilterNoExposuresPlanned;
                }
            }

            /*  TODO: Pass 4 refines the order of filters/exposures for the selected target:
                - If the time span on the target includes twilight, order so that exposures with more restrictive twilight are not
                  scheduled during those periods. For example a NB filter set to image at astro twilight could be imaged during that time
                  at dusk and dawn, but a WB filter could not and would require nighttime darkness before imaging.
                - Consideration may also be given to prioritizing filters with lower percent complete so that overall acquisition on a target is balanced.
             */

            return planTarget;
        }

        private void SetRejected(IPlanProject planProject, string reason) {
            planProject.Rejected = true;
            planProject.RejectedReason = reason;
        }

        private void SetRejected(IPlanTarget planTarget, string reason) {
            planTarget.Rejected = true;
            planTarget.RejectedReason = reason;
        }

        private void SetRejected(IPlanFilter planFilter, string reason) {
            planFilter.Rejected = true;
            planFilter.RejectedReason = reason;
        }

        private List<IPlanProject> PropagateRejections(List<IPlanProject> projects) {
            if (projects == null || projects.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                bool projectRejected = true;

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }
                    bool targetRejected = true;

                    foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                        if (!planFilter.Rejected) {
                            targetRejected = false;
                            break;
                        }
                    }

                    if (targetRejected) {
                        SetRejected(planTarget, Reasons.TargetAllFilterPlans);
                    }

                    if (!planTarget.Rejected) {
                        projectRejected = false;
                        break;
                    }
                }

                if (projectRejected) {
                    SetRejected(planProject, Reasons.ProjectAllTargets);
                }
            }

            return projects;
        }

        private bool ProjectIsInComplete(IPlanProject planProject) {
            bool incomplete = false;
            foreach (IPlanTarget target in planProject.Targets) {
                foreach (IPlanFilter planFilter in target.FilterPlans) {
                    if (planFilter.IsIncomplete()) {
                        incomplete = true;
                    }
                    else {
                        SetRejected(planFilter, Reasons.FilterComplete);
                    }
                }
            }

            return incomplete;
        }

        private TimeInterval GetTargetTimeInterval(DateTime atTime, IPlanTarget planTarget) {
            // TODO: last step is to set the hard start/stop for this target
            // set start/end to overall start/end of all FilterPlans
            // but then be sure to clip end by a hard (twilight or horizon) stop

            // hard stop time is critical since that's the time we set to come back and run the planner again

            NighttimeCircumstances nighttimeCircumstances = new NighttimeCircumstances(observerInfo, atTime);
            DateTime hardStartTime = planTarget.StartTime < atTime ? atTime : planTarget.StartTime;
            DateTime hardStopTime = planTarget.EndTime;

            /* TODO: also update times based on twilight
            foreach (PlanFilter planFilter in planTarget.FilterPlans) {
                if (planFilter.Rejected) { continue; }

            }*/

            return new TimeInterval(hardStartTime, hardStopTime);
        }

        private int GetOverallTwilight(IPlanTarget planTarget) {
            int twilightInclude = AssistantFilterPreferences.TWILIGHT_INCLUDE_NONE;
            foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                // find most permissive (brightest) twilight over all incomplete plans
                if (planFilter.IsIncomplete() && planFilter.Preferences.TwilightInclude > twilightInclude) {
                    twilightInclude = planFilter.Preferences.TwilightInclude;
                }
            }

            return twilightInclude;
        }

        private bool RejectForMoonAvoidance(IPlanTarget planTarget, IPlanFilter planFilter) {
            DateTime midPointTime = Utils.GetMidpointTime(planTarget.StartTime, planTarget.EndTime);
            double moonAge = AstrometryUtils.GetMoonAge(midPointTime);
            double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(observerInfo, midPointTime, planTarget.Coordinates);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge,
                planFilter.Preferences.MoonAvoidanceSeparation, planFilter.Preferences.MoonAvoidanceWidth);
            Logger.Trace($"Assistant: moon avoidance {planTarget.Name}/{planFilter.FilterName} midpoint={midPointTime}, moonSep={moonSeparation}, moonAvoidSep={moonAvoidanceSeparation}");

            return moonSeparation < moonAvoidanceSeparation;
        }

        private List<IPlanProject> GetProjects(DateTime atTime, string profileId) {
            List<Project> projects = null;
            List<FilterPreference> filterPrefs = null;

            AssistantDatabaseInteraction database = new AssistantDatabaseInteraction();
            using (var context = database.GetContext()) {
                try {
                    projects = context.GetActiveProjects(profileId, atTime);
                    filterPrefs = context.GetFilterPreferences(profileId);
                }
                catch (Exception ex) {
                    Logger.Error($"Assistant: exception reading database: {ex}");
                    // TODO: need to throw so instruction can react properly
                    // maybe: new SequenceEntityFailedException("") ?
                    // or a general exception and let the instruction decide what to throw
                }
            }

            if (projects == null || projects.Count == 0) {
                Logger.Warning("Assistant: no projects are active and within start/end dates at planning time");
                return null;
            }

            List<IPlanProject> planProjects = new List<IPlanProject>();
            Dictionary<string, AssistantFilterPreferences> filterPrefsDictionary = GetFilterPrefDictionary(filterPrefs);

            foreach (Project project in projects) {
                PlanProject planProject = new PlanProject(activeProfile, project, filterPrefsDictionary);
                planProjects.Add(planProject);
            }

            return planProjects;
        }

        private Dictionary<string, AssistantFilterPreferences> GetFilterPrefDictionary(List<FilterPreference> filterPrefs) {
            Dictionary<string, AssistantFilterPreferences> dict = new Dictionary<string, AssistantFilterPreferences>();

            foreach (FilterPreference filterPref in filterPrefs) {
                dict.Add(filterPref.filterName, filterPref.Preferences);
            }

            return dict;
        }

    }

}
