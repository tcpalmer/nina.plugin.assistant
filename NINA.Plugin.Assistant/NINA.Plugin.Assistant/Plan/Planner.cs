﻿using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class Planner {

        private DateTime atTime;
        private IProfile activeProfile;
        private ObserverInfo observerInfo;
        private List<IPlanProject> projects;

        public Planner(DateTime atTime, IProfileService profileService) : this(atTime, profileService, null) { }

        public Planner(DateTime atTime, IProfileService profileService, List<IPlanProject> projects) {
            this.atTime = atTime;
            this.activeProfile = profileService.ActiveProfile;
            this.projects = projects;
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
            Logger.Debug($"Assistant: getting current plan for {atTime}");

            // TODO: be nice to come up with a better way to 
            if (true) {
                // HACK!
                return new PlannerEmulator(atTime, activeProfile).GetPlan(previousPlanTarget);
            }

            using (MyStopWatch.Measure("Assistant Plan Generation")) {
                try {
                    if (projects == null) {
                        projects = GetProjects(atTime);
                    }

                    projects = FilterForIncomplete(projects);
                    //Logger.Trace($"Assistant: GetPlan after FilterForIncomplete:\n{PlanProject.ListToString(projects)}");

                    projects = FilterForVisibility(projects);
                    //Logger.Trace($"Assistant: GetPlan after FilterForVisibility:\n{PlanProject.ListToString(projects)}");

                    projects = FilterForMoonAvoidance(projects);
                    //Logger.Trace($"Assistant: GetPlan after FilterForMoonAvoidance:\n{PlanProject.ListToString(projects)}");

                    DateTime? waitForVisibleNow = CheckForVisibleNow(projects);
                    if (waitForVisibleNow != null) {
                        return new AssistantPlan((DateTime)waitForVisibleNow);
                    }

                    ScoringEngine scoringEngine = new ScoringEngine(activeProfile, atTime, previousPlanTarget);

                    IPlanTarget planTarget = SelectTargetByScore(projects, scoringEngine);

                    if (planTarget != null) {
                        Logger.Debug($"Assistant: GetPlan highest scoring target:\n{planTarget}");
                        TimeInterval targetWindow = GetTargetTimeWindow(atTime, planTarget);
                        List<IPlanInstruction> planInstructions = PlanInstructions(planTarget, previousPlanTarget, targetWindow);
                        return new AssistantPlan(planTarget, targetWindow, planInstructions);
                    }
                    else {
                        Logger.Debug("Assistant: GetPlan no target selected");
                        return null;
                    }
                }
                catch (Exception ex) {
                    if (ex is SequenceEntityFailedException) {
                        throw ex;
                    }

                    Logger.Error($"Assistant: exception generating plan: {ex.StackTrace}");
                    throw new SequenceEntityFailedException($"Assistant: exception generating plan: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// To estimate what the planner might do on a given night, a series of plans can be generated by repeatedly
        /// calling the planner using the end time of the previous run as the next starting point.  This series is 'perfect'
        /// for two reasons.  One, it assumes that operations that absolutely will take time (like slew/center, switching filters,
        /// autofocus, meridian flips, etc) take zero time.  So while each individual plan run will end at the proper time, you
        /// are unlikely to get the number of exposures it schedules.  And two, all images are assumed to be acceptable and will
        /// increment the accepted count for the target/filter.  The net result is that acceptable images will be acquired
        /// (and projects completed) significantly faster than in actual usage.
        /// 
        /// Nevertheless, a perfect plan provides some idea of what the planner will do on a given night which is useful
        /// for previewing and troubleshooting.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="profileService"></param>
        /// <param name="projects"></param>
        /// <returns>list</returns>
        public static List<AssistantPlan> GetPerfectPlan(DateTime atTime, IProfileService profileService, List<IPlanProject> projects) {
            List<AssistantPlan> plans = new List<AssistantPlan>();

            DateTime currentTime = atTime;
            IPlanTarget previousPlanTarget = null;

            AssistantPlan plan;
            while ((plan = new Planner(currentTime, profileService, projects).GetPlan(previousPlanTarget)) != null) {
                plans.Add(plan);
                previousPlanTarget = plan.WaitForNextTargetTime != null ? null : plan.PlanTarget;
                currentTime = plan.WaitForNextTargetTime != null ? (DateTime)plan.WaitForNextTargetTime : plan.TimeInterval.EndTime;
                PrepForNextRun(projects, plan);
            }

            return plans;
        }

        private static void PrepForNextRun(List<IPlanProject> projects, AssistantPlan plan) {

            foreach (IPlanProject planProject in projects) {
                planProject.Rejected = false;
                planProject.RejectedReason = null;
                foreach (IPlanTarget planTarget in planProject.Targets) {
                    planTarget.Rejected = false;
                    planTarget.RejectedReason = null;
                    foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                        planFilter.Accepted += planFilter.PlannedExposures;
                        planFilter.PlannedExposures = 0;
                        planFilter.Rejected = false;
                        planFilter.RejectedReason = null;
                    }
                }
            }
        }

        public static List<AssistantPlan> GetPerfectPlan(DateTime atTime, IProfileService profileService) {
            return GetPerfectPlan(atTime, profileService, null);
        }

        /// <summary>
        /// Review the project list and reject those projects that are already complete.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForIncomplete(List<IPlanProject> projects) {
            if (projects?.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (!ProjectIsInComplete(planProject)) {
                    SetRejected(planProject, Reasons.ProjectComplete);
                    foreach (IPlanTarget planTarget in planProject.Targets) {
                        SetRejected(planTarget, Reasons.TargetComplete);
                    }
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
            if (projects?.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    if (!AstrometryUtils.RisesAtLocation(observerInfo, planTarget.Coordinates)) {
                        Logger.Warning($"Assistant: target {planProject.Name}/{planTarget.Name} never rises at location - skipping");
                        SetRejected(planTarget, Reasons.TargetNeverRises);
                        continue;
                    }

                    // Get the most inclusive twilight over all incomplete exposure plans
                    NighttimeCircumstances nighttimeCircumstances = new NighttimeCircumstances(observerInfo, atTime);
                    TimeInterval twilightSpan = nighttimeCircumstances.GetTwilightSpan(GetOverallTwilight(planTarget));

                    // Determine the potential imaging time span
                    TargetCircumstances targetCircumstances = new TargetCircumstances(planTarget.Coordinates, observerInfo, planProject.HorizonDefinition, twilightSpan);

                    DateTime actualStart = atTime > targetCircumstances.RiseAboveHorizonTime ? atTime : targetCircumstances.RiseAboveHorizonTime;
                    int TimeOnTargetSeconds = (int)(targetCircumstances.SetBelowHorizonTime - actualStart).TotalSeconds;

                    // If the start time is in the future, reject for now
                    if (actualStart > atTime) {
                        planTarget.StartTime = actualStart;
                        SetRejected(planTarget, Reasons.TargetNotYetVisible);
                    }
                    // If the target is visible for at least the minimum time, then accept it
                    else if (targetCircumstances.IsVisible && (TimeOnTargetSeconds >= planProject.MinimumTime * 60)) {
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
            if (projects?.Count == 0) {
                return null;
            }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                        if (planFilter.IsIncomplete() && planFilter.MoonAvoidanceEnabled) {
                            if (RejectForMoonAvoidance(planTarget, planFilter)) {
                                SetRejected(planFilter, Reasons.FilterMoonAvoidance);
                            }
                        }
                    }
                }
            }

            return PropagateRejections(projects);
        }

        /// <summary>
        /// If all targets were rejected but some due to not yet visible, then find the earliest of those start times - will
        /// have to wait until then.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public DateTime? CheckForVisibleNow(List<IPlanProject> projects) {
            if (projects?.Count == 0) {
                return null;
            }

            DateTime? nextAvailableTime = DateTime.MaxValue;

            foreach (IPlanProject project in projects) {
                foreach (IPlanTarget planTarget in project.Targets) {
                    if (!planTarget.Rejected) {
                        return null;
                    }

                    if (planTarget.RejectedReason == Reasons.TargetNotYetVisible) {
                        nextAvailableTime = planTarget.StartTime < nextAvailableTime ? planTarget.StartTime : nextAvailableTime;
                    }
                }
            }

            return (nextAvailableTime < DateTime.MaxValue) ? nextAvailableTime : null;
        }

        /// <summary>
        /// Run the scoring engine, applying the weighted rules to determine the target with the highest score.
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="scoringEngine"></param>
        /// <returns></returns>
        public IPlanTarget SelectTargetByScore(List<IPlanProject> projects, IScoringEngine scoringEngine) {
            if (projects?.Count == 0) {
                return null;
            }

            IPlanTarget highScoreTarget = null;
            double highScore = 0;

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                scoringEngine.RuleWeights = planProject.RuleWeights;

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

        /// <summary>
        /// Determine the time window for the selected target.  The start time is basically ASAP but the end time
        /// needs to be chosen carefully since that's the point at which the planner will be called again to
        /// select the next (same or different) target.
        /// </summary>
        /// <param name="atTime"></param>
        /// <param name="planTarget"></param>
        /// <returns></returns>
        public TimeInterval GetTargetTimeWindow(DateTime atTime, IPlanTarget planTarget) {
            DateTime hardStartTime = planTarget.StartTime < atTime ? atTime : planTarget.StartTime;

            // Set the stop time to the earliest of the target's hard stop time and the time when the
            // minimum time-on-target is achieved.  Rather than do a deeper analysis of which target
            // might be better to image next, we just let the planner run again and decide at that point.

            // TODO: in reality, the above is probably not great.  What if another target would get w/in
            // its meridian window before the current gets its minimum time?  Might have a hard time imaging
            // that target if another is always before it.  But the above may work OK if the minimum time
            // isn't too long.
            // But, if the min time on target is longish, then you run the risk of deciding that a target
            // can't be imaged since it can't get it's min time - when in fact it could get say 80% of it
            // which might otherwise be wasted.

            // TODO: create a set of events, sorted in order of soonest time:
            // - upcoming hard stop for selected target (automatically = stop time if that's earliest)
            // - upcoming twilight events
            // - for all potentially visible other targets (omit selected): visibility begins (which would include meridian window)?
            //    - can this be done during the visibility calc?
            // We'll then be in a position to make a decent choice

            int minimumTimeOnTarget = planTarget.Project.MinimumTime;
            DateTime hardStopTime = hardStartTime.AddMinutes(minimumTimeOnTarget);
            if (hardStartTime > planTarget.EndTime) {
                hardStartTime = planTarget.EndTime;
            }

            return new TimeInterval(hardStartTime, hardStopTime);
        }

        /// <summary>
        /// Plan the sequence of instructions needed to take the desired exposures of the target during the
        /// target window.
        /// </summary>
        /// <param name="planTarget"></param>
        /// <param name="previousPlanTarget"></param>
        /// <param name="targetWindow"></param>
        /// <returns>instructions</returns>
        public List<IPlanInstruction> PlanInstructions(IPlanTarget planTarget, IPlanTarget previousPlanTarget, TimeInterval targetWindow) {
            if (planTarget == null) {
                return null;
            }

            List<IPlanInstruction> instructions = new List<IPlanInstruction>();

            // If this target is different from the previous, add the slew instruction
            if (!planTarget.Equals(previousPlanTarget)) {
                instructions.Add(new PlanSlew(true));
            }

            NighttimeCircumstances nighttimeCircumstances = new NighttimeCircumstances(observerInfo, atTime);
            instructions.AddRange(new ExposurePlanner(planTarget, targetWindow, nighttimeCircumstances).Plan());
            return instructions;
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
            if (projects?.Count == 0) {
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

        private TwilightLevel GetOverallTwilight(IPlanTarget planTarget) {
            TwilightLevel twilightLevel = TwilightLevel.Nighttime;
            foreach (IPlanFilter planFilter in planTarget.FilterPlans) {
                // find most permissive (brightest) twilight over all incomplete plans
                if (planFilter.IsIncomplete() && planFilter.TwilightLevel > twilightLevel) {
                    twilightLevel = planFilter.TwilightLevel;
                }
            }

            return twilightLevel;
        }

        private bool RejectForMoonAvoidance(IPlanTarget planTarget, IPlanFilter planFilter) {
            DateTime midPointTime = Utils.GetMidpointTime(planTarget.StartTime, planTarget.EndTime);
            double moonAge = AstrometryUtils.GetMoonAge(midPointTime);
            double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(observerInfo, midPointTime, planTarget.Coordinates);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge,
                planFilter.MoonAvoidanceSeparation, planFilter.MoonAvoidanceWidth);
            Logger.Debug($"Assistant: moon avoidance {planTarget.Name}/{planFilter.FilterName} midpoint={midPointTime}, moonSep={moonSeparation}, moonAvoidSep={moonAvoidanceSeparation}");

            return moonSeparation < moonAvoidanceSeparation;
        }

        private List<IPlanProject> GetProjects(DateTime atTime) {

            try {
                AssistantDatabaseInteraction database = new AssistantDatabaseInteraction();
                AssistantPlanLoader loader = new AssistantPlanLoader();
                return loader.LoadActiveProjects(database.GetContext(), activeProfile, atTime);
            }
            catch (Exception ex) {
                Logger.Error($"Assistant: exception reading database: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"Assistant: exception reading database: {ex.Message}", ex);
            }
        }

    }

}
