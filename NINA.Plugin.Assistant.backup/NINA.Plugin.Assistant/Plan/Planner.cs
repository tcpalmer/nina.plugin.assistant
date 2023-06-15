using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan.Scoring;
using Assistant.NINAPlugin.Util;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace Assistant.NINAPlugin.Plan {

    public class Planner {

        private DateTime atTime;
        private IProfile activeProfile;
        private ProfilePreference profilePreferences;
        private ObserverInfo observerInfo;
        private List<IPlanProject> projects;

        public static readonly bool USE_EMULATOR = false;

        public Planner(DateTime atTime, IProfileService profileService, ProfilePreference profilePreferences) : this(atTime, profileService, profilePreferences, null) { }

        public Planner(DateTime atTime, IProfileService profileService, ProfilePreference profilePreferences, List<IPlanProject> projects) {
            this.atTime = atTime;
            this.activeProfile = profileService.ActiveProfile;
            this.profilePreferences = profilePreferences;
            this.projects = projects;
            this.observerInfo = new ObserverInfo {
                Latitude = activeProfile.AstrometrySettings.Latitude,
                Longitude = activeProfile.AstrometrySettings.Longitude,
                Elevation = activeProfile.AstrometrySettings.Elevation,
            };

            if (AstrometryUtils.IsAbovePolarCircle(observerInfo)) {
                TSLogger.Error("observer location is above a polar circle - not supported");
                throw new Exception("Scheduler: observer location is above a polar circle - not supported");
            }
        }

        public SchedulerPlan GetPlan(IPlanTarget previousPlanTarget) {
            TSLogger.Info("-- BEGIN PLANNING ENGINE RUN ---------------------------------------------------");
            TSLogger.Debug($"getting current plan for {Utils.FormatDateTimeFull(atTime)}");

            if (USE_EMULATOR) {
                Notification.ShowInformation("REMINDER: running plan emulation");
                return new PlannerEmulator(atTime, activeProfile).GetPlan(previousPlanTarget);
            }

            using (MyStopWatch.Measure("Scheduler Plan Generation")) {
                try {
                    if (projects == null) {
                        projects = GetProjects();
                    }

                    projects = FilterForIncomplete(projects);
                    if (TSLogger.IsEnabled(LogEventLevel.Verbose)) {
                        TSLogger.Trace($"GetPlan after FilterForIncomplete:\n{PlanProject.ListToString(projects)}");
                    }

                    projects = FilterForVisibility(projects);
                    if (TSLogger.IsEnabled(LogEventLevel.Verbose)) {
                        TSLogger.Trace($"GetPlan after FilterForVisibility:\n{PlanProject.ListToString(projects)}");
                    }

                    projects = FilterForMoonAvoidance(projects);
                    if (TSLogger.IsEnabled(LogEventLevel.Verbose)) {
                        TSLogger.Trace($"GetPlan after FilterForMoonAvoidance:\n{PlanProject.ListToString(projects)}");
                    }

                    DateTime? waitForVisibleNow = CheckForVisibleNow(projects);
                    if (waitForVisibleNow != null) {
                        return new SchedulerPlan((DateTime)waitForVisibleNow);
                    }

                    ScoringEngine scoringEngine = new ScoringEngine(activeProfile, atTime, previousPlanTarget);
                    IPlanTarget planTarget = SelectTargetByScore(projects, scoringEngine);

                    if (planTarget != null) {
                        TSLogger.Debug($"highest scoring (or only) target: {planTarget.Name}");
                        if (TSLogger.IsEnabled(LogEventLevel.Verbose)) {
                            TSLogger.Trace($"highest scoring (or only) target:\n{planTarget}");
                        }

                        TimeInterval targetWindow = GetTargetTimeWindow(atTime, planTarget);
                        List<IPlanInstruction> planInstructions = PlanInstructions(planTarget, previousPlanTarget, targetWindow);
                        return new SchedulerPlan(planTarget, targetWindow, planInstructions);
                    }
                    else {
                        TSLogger.Debug("Scheduler Planner: no target selected");
                        return null;
                    }
                }
                catch (Exception ex) {
                    if (ex is SequenceEntityFailedException) {
                        throw ex;
                    }

                    TSLogger.Error($"exception generating plan: {ex.StackTrace}");
                    throw new SequenceEntityFailedException($"Scheduler: exception generating plan: {ex.Message}", ex);
                }
                finally {
                    TSLogger.Info("-- END PLANNING ENGINE RUN -----------------------------------------------------");
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
        public static List<SchedulerPlan> GetPerfectPlan(DateTime atTime, IProfileService profileService, ProfilePreference profilePreferences, List<IPlanProject> projects) {

            TSLogger.Info("-- BEGIN PLAN PREVIEW ----------------------------------------------------------");

            List<SchedulerPlan> plans = new List<SchedulerPlan>();
            DateTime currentTime = atTime;
            IPlanTarget previousPlanTarget = null;

            try {
                SchedulerPlan plan;
                while ((plan = new Planner(currentTime, profileService, profilePreferences, projects).GetPlan(previousPlanTarget)) != null) {
                    plans.Add(plan);
                    previousPlanTarget = plan.WaitForNextTargetTime != null ? null : plan.PlanTarget;
                    currentTime = plan.WaitForNextTargetTime != null ? (DateTime)plan.WaitForNextTargetTime : plan.TimeInterval.EndTime;
                    PrepForNextRun(projects, plan);
                }

                return plans;
            }
            catch (Exception ex) {
                TSLogger.Error($"exception during plan preview: {ex.Message}\n{ex.StackTrace}");
                return plans;
            }
            finally {
                TSLogger.Info("-- END PLAN PREVIEW ------------------------------------------------------------");
            }
        }

        private static void PrepForNextRun(List<IPlanProject> projects, SchedulerPlan plan) {

            foreach (IPlanProject planProject in projects) {
                planProject.Rejected = false;
                planProject.RejectedReason = null;
                foreach (IPlanTarget planTarget in planProject.Targets) {
                    planTarget.Rejected = false;
                    planTarget.RejectedReason = null;
                    foreach (IPlanExposure planExposure in planTarget.ExposurePlans) {
                        planExposure.Accepted += planExposure.PlannedExposures;
                        planExposure.PlannedExposures = 0;
                        planExposure.Rejected = false;
                        planExposure.RejectedReason = null;
                    }
                }
            }
        }

        public static List<SchedulerPlan> GetPerfectPlan(DateTime atTime, IProfileService profileService, ProfilePreference profilePreferences) {
            return GetPerfectPlan(atTime, profileService, profilePreferences, null);
        }

        /// <summary>
        /// Review the project list and reject those projects that are already complete.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForIncomplete(List<IPlanProject> projects) {
            if (NoProjects(projects)) { return null; }

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
        /// within the time window set by the most inclusive twilight over all incomplete exposure plans for that target, is clipped
        /// to any meridian window, and the remaining visible time is greater than the minimum imaging time preference for the project.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public List<IPlanProject> FilterForVisibility(List<IPlanProject> projects) {
            if (NoProjects(projects)) { return null; }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    if (!AstrometryUtils.RisesAtLocation(observerInfo, planTarget.Coordinates)) {
                        TSLogger.Warning($"target {planProject.Name}/{planTarget.Name} never rises at location - skipping");
                        SetRejected(planTarget, Reasons.TargetNeverRises);
                        continue;
                    }

                    // Get the most inclusive twilight over all incomplete exposure plans
                    NighttimeCircumstances nighttimeCircumstances = NighttimeCircumstances.AdjustNighttimeCircumstances(observerInfo, atTime);
                    TimeInterval twilightSpan = nighttimeCircumstances.GetTwilightSpan(GetOverallTwilight(planTarget));

                    // Determine the potential imaging time span
                    TargetCircumstances targetCircumstances = new TargetCircumstances(planTarget.Coordinates, observerInfo, planProject.HorizonDefinition, twilightSpan);

                    DateTime targetStartTime = targetCircumstances.RiseAboveHorizonTime;
                    DateTime targetCulminationTime = targetCircumstances.CulminationTime;
                    DateTime targetEndTime = targetCircumstances.SetBelowHorizonTime;

                    // Clip time span to optional meridian window
                    TimeInterval meridianClippedSpan = null;
                    if (planProject.MeridianWindow > 0) {
                        meridianClippedSpan = new MeridianWindowClipper().Clip(
                                           targetStartTime,
                                           targetCircumstances.CulminationTime,
                                           targetEndTime,
                                           planProject.MeridianWindow);

                        if (meridianClippedSpan == null) {
                            SetRejected(planTarget, Reasons.TargetMeridianWindowClipped);
                            continue;
                        }

                        planTarget.MeridianWindow = meridianClippedSpan;
                        targetStartTime = meridianClippedSpan.StartTime;
                        targetEndTime = meridianClippedSpan.EndTime;
                    }

                    DateTime actualStart = atTime > targetStartTime ? atTime : targetStartTime;
                    int TimeOnTargetSeconds = (int)(targetEndTime - actualStart).TotalSeconds;

                    // If the start time is in the future, reject for now
                    if (actualStart > atTime) {
                        planTarget.StartTime = actualStart;
                        string reason = meridianClippedSpan != null ? Reasons.TargetBeforeMeridianWindow : Reasons.TargetNotYetVisible;
                        SetRejected(planTarget, reason);
                        continue;
                    }

                    // If the target is visible for at least the minimum time, then accept it
                    if (targetCircumstances.IsVisible && (TimeOnTargetSeconds >= planProject.MinimumTime * 60)) {
                        planTarget.SetCircumstances(targetCircumstances.IsVisible, targetStartTime, targetCulminationTime, targetEndTime);
                        continue;
                    }

                    // Otherwise reject it
                    SetRejected(planTarget, Reasons.TargetNotVisible);
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
            if (NoProjects(projects)) { return null; }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    foreach (IPlanExposure planExposure in planTarget.ExposurePlans) {
                        if (planExposure.IsIncomplete() && planExposure.MoonAvoidanceEnabled) {
                            if (RejectForMoonAvoidance(planTarget, planExposure)) {
                                SetRejected(planExposure, Reasons.FilterMoonAvoidance);
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
            if (NoProjects(projects)) { return null; }

            DateTime? nextAvailableTime = DateTime.MaxValue;

            foreach (IPlanProject project in projects) {
                foreach (IPlanTarget planTarget in project.Targets) {
                    if (!planTarget.Rejected) {
                        return null;
                    }

                    if (planTarget.RejectedReason == Reasons.TargetNotYetVisible || planTarget.RejectedReason == Reasons.TargetBeforeMeridianWindow) {
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

            // If no active projects or targets, we're done
            List<IPlanTarget> targets = GetActiveTargets(projects);
            if (targets.Count == 0) {
                return null;
            }

            // If only one active target, no need to run scoring engine
            if (targets.Count == 1) {
                return targets[0];
            }

            IPlanTarget highScoreTarget = null;
            double highScore = double.MinValue;

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                scoringEngine.RuleWeights = planProject.RuleWeights;

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    TSLogger.Debug($"running scoring engine for project/target {planProject.Name}/{planTarget.Name}");
                    double score = scoringEngine.ScoreTarget(planTarget);
                    if (score > highScore) {
                        highScoreTarget = planTarget;
                        highScore = score;
                    }
                }
            }

            // Mark losing targets rejected
            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }

                    if (planTarget != highScoreTarget) {
                        planTarget.Rejected = true;
                        planTarget.RejectedReason = Reasons.TargetLowerScore;
                    }
                }
            }

            return highScoreTarget;
        }

        public TimeInterval GetTargetTimeWindow(DateTime atTime, IPlanTarget planTarget) {

            DateTime planStartTime = planTarget.StartTime < atTime ? atTime : planTarget.StartTime;
            DateTime planStopTime = new PlanStopTimeExpert().GetStopTime(planStartTime, planTarget, null);

            if (planStopTime > planTarget.EndTime) {
                planStopTime = planTarget.EndTime;
            }

            return new TimeInterval(planStartTime, planStopTime);
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

            NighttimeCircumstances nighttimeCircumstances = NighttimeCircumstances.AdjustNighttimeCircumstances(observerInfo, atTime);
            instructions.AddRange(new ExposurePlanner(profilePreferences, planTarget, targetWindow, nighttimeCircumstances).Plan());
            return instructions;
        }

        public bool HasActiveProjects(List<IPlanProject> projects) {
            if (projects == null) {
                projects = GetProjects();
            }

            if (NoProjects(projects)) {
                return false;
            }

            foreach (IPlanProject project in projects) {
                if (ProjectIsInComplete(project)) {
                    return true;
                }
            }

            return false;
        }

        private bool NoProjects(List<IPlanProject> projects) {
            return projects == null || projects.Count == 0;
        }

        private void SetRejected(IPlanProject planProject, string reason) {
            planProject.Rejected = true;
            planProject.RejectedReason = reason;
        }

        private void SetRejected(IPlanTarget planTarget, string reason) {
            planTarget.Rejected = true;
            planTarget.RejectedReason = reason;
        }

        private void SetRejected(IPlanExposure planExposure, string reason) {
            planExposure.Rejected = true;
            planExposure.RejectedReason = reason;
        }

        private List<IPlanProject> PropagateRejections(List<IPlanProject> projects) {
            if (NoProjects(projects)) { return null; }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                bool projectRejected = true;

                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }
                    bool targetRejected = true;

                    foreach (IPlanExposure planExposure in planTarget.ExposurePlans) {
                        if (!planExposure.Rejected) {
                            targetRejected = false;
                            break;
                        }
                    }

                    if (targetRejected) {
                        SetRejected(planTarget, Reasons.TargetAllExposurePlans);
                    }

                    if (!planTarget.Rejected) {
                        projectRejected = false;
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
                foreach (IPlanExposure planExposure in target.ExposurePlans) {
                    if (planExposure.IsIncomplete()) {
                        incomplete = true;
                    }
                    else {
                        SetRejected(planExposure, Reasons.FilterComplete);
                    }
                }
            }

            return incomplete;
        }

        private TwilightLevel GetOverallTwilight(IPlanTarget planTarget) {
            TwilightLevel twilightLevel = TwilightLevel.Nighttime;
            foreach (IPlanExposure planExposure in planTarget.ExposurePlans) {
                // find most permissive (brightest) twilight over all incomplete plans
                if (planExposure.IsIncomplete() && planExposure.TwilightLevel > twilightLevel) {
                    twilightLevel = planExposure.TwilightLevel;
                }
            }

            return twilightLevel;
        }

        private bool RejectForMoonAvoidance(IPlanTarget planTarget, IPlanExposure planExposure) {
            DateTime midPointTime = Utils.GetMidpointTime(planTarget.StartTime, planTarget.EndTime);
            double moonAge = AstrometryUtils.GetMoonAge(midPointTime);
            double moonSeparation = AstrometryUtils.GetMoonSeparationAngle(observerInfo, midPointTime, planTarget.Coordinates);
            double moonAvoidanceSeparation = AstrometryUtils.GetMoonAvoidanceLorentzianSeparation(moonAge,
                planExposure.MoonAvoidanceSeparation, planExposure.MoonAvoidanceWidth);

            bool rejected = moonSeparation < moonAvoidanceSeparation;
            TSLogger.Debug($"moon avoidance {planTarget.Name}/{planExposure.FilterName} rejected={rejected}, midpoint={midPointTime}, moonSep={moonSeparation}, moonAvoidSep={moonAvoidanceSeparation}");

            return rejected;
        }

        private List<IPlanTarget> GetActiveTargets(List<IPlanProject> projects) {
            List<IPlanTarget> targets = new List<IPlanTarget>();

            if (NoProjects(projects)) {
                return targets;
            }

            foreach (IPlanProject planProject in projects) {
                if (planProject.Rejected) { continue; }
                foreach (IPlanTarget planTarget in planProject.Targets) {
                    if (planTarget.Rejected) { continue; }
                    targets.Add(planTarget);
                }
            }

            return targets;
        }

        private List<IPlanProject> GetProjects() {

            try {
                SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
                SchedulerPlanLoader loader = new SchedulerPlanLoader(activeProfile);
                return loader.LoadActiveProjects(database.GetContext());
            }
            catch (Exception ex) {
                TSLogger.Error($"exception reading database: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"Scheduler: exception reading database: {ex.Message}", ex);
            }
        }
    }

}
