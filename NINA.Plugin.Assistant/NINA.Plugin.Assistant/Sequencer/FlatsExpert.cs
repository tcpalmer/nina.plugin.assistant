using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistant.NINAPlugin.Sequencer {

    public class FlatsExpert {

        public static readonly double ACQUIRED_IMAGES_CUTOFF_DAYS = -45;

        public FlatsExpert() { }

        /// <summary>
        /// Get the list of active targets using periodic flat timing.
        /// </summary>
        /// <param name="allProjects"></param>
        /// <returns></returns>
        public List<Target> GetTargetsForPeriodicFlats(List<Project> allProjects) {
            List<Target> targets = new List<Target>();
            foreach (Project project in allProjects) {
                if (project.State != ProjectState.Active ||
                    project.FlatsHandling == Project.FLATS_HANDLING_OFF ||
                    project.FlatsHandling == Project.FLATS_HANDLING_TARGET_COMPLETION ||
                    project.FlatsHandling == Project.FLATS_HANDLING_IMMEDIATE) { continue; }

                targets.AddRange(project.Targets.Where(t => t.Enabled == true));
            }

            return targets;
        }

        /// <summary>
        /// Get the list of active targets using completion flat timing and with all exposure plans complete.
        /// </summary>
        /// <param name="allProjects"></param>
        /// <returns></returns>
        public List<Target> GetCompletedTargetsForFlats(List<Project> allProjects) {
            List<Target> targets = new List<Target>();
            foreach (Project project in allProjects) {
                if (project.State != ProjectState.Active || project.FlatsHandling != Project.FLATS_HANDLING_TARGET_COMPLETION) { continue; }

                targets.AddRange(project.Targets.Where(t => t.Enabled == true && t.PercentComplete >= 100));
            }

            return targets;
        }

        /// <summary>
        /// Get all light sessions associated with the provided targets as determined from acquired image records.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="acquiredImages"></param>
        /// <returns></returns>
        public List<LightSession> GetLightSessions(List<Target> targets, List<AcquiredImage> acquiredImages) {
            List<LightSession> lightSessions = new List<LightSession>();

            foreach (Target target in targets) {

                foreach (AcquiredImage exposure in acquiredImages) {
                    if (target.Id != exposure.TargetId) { continue; }
                    LightSession lightSession = new LightSession(exposure.TargetId,
                                                                 GetLightSessionDate(exposure.AcquiredDate),
                                                                 exposure.Metadata.SessionId,
                                                                 new FlatSpec(exposure));
                    if (!lightSessions.Contains(lightSession)) {
                        lightSessions.Add(lightSession);
                    }
                }
            }

            lightSessions.Sort();
            return lightSessions;
        }

        /// <summary>
        /// Find light sessions without corresponding flats history that are older than the flats cadence for the targets
        /// that employ periodic flats.
        /// 
        /// Note that the list returned may well contain what would amount to duplicate flat sets if the same set is
        /// needed by more than one target.  The list will be culled for dups before actually generating the flats but
        /// we need the whole list to ultimately write flat history records when done.
        /// </summary>
        /// <param name="checkDate"></param>
        /// <param name="targets"></param>
        /// <param name="allLightSessions"></param>
        /// <param name="takenFlats"></param>
        /// <returns></returns>
        public List<LightSession> GetNeededPeriodicFlats(DateTime runDate, List<Target> targets, List<LightSession> allLightSessions, List<FlatHistory> takenFlats) {
            List<LightSession> missingLightSessions = new List<LightSession>();
            DateTime checkDate = GetLightSessionDate(runDate);

            foreach (Target target in targets) {

                // Get the light sessions and flat history for this target
                List<LightSession> targetLightSessions = allLightSessions.Where(ls => ls.TargetId == target.Id).ToList();
                List<FlatHistory> targetFlatHistory = takenFlats.Where(tf => tf.TargetId == target.Id).ToList();

                List<LightSession> potentialLightSessions = new List<LightSession>();

                foreach (LightSession lightSession in targetLightSessions) {
                    potentialLightSessions.Add(lightSession);
                    foreach (FlatHistory flatHistory in targetFlatHistory) {

                        // Remove if there is already a flat history record for this light session
                        if (lightSession.SessionDate == flatHistory.LightSessionDate
                            && lightSession.SessionId == flatHistory.LightSessionId
                            && lightSession.FlatSpec.Equals(new FlatSpec(flatHistory))) {
                            potentialLightSessions.Remove(lightSession);
                            continue;
                        }
                    }
                }

                // Skip if not enough days have passed based on project setting
                int flatsPeriod = target.Project.FlatsHandling;
                foreach (LightSession lightSession in potentialLightSessions) {
                    if (flatsPeriod > 1 && (checkDate - lightSession.SessionDate).TotalDays < flatsPeriod) { continue; }
                    missingLightSessions.Add(lightSession);
                }
            }

            return missingLightSessions;
        }

        /// <summary>
        /// Find light sessions without corresponding flats history for targets that employ target completion flats.
        /// 
        /// Note that the list returned may well contain what would amount to duplicate flat sets if the same set is
        /// needed by more than one target.  The list will be culled for dups before actually generating the flats but
        /// we need the whole list to ultimately write flat history records when done.
        /// </summary>
        /// <param name="targets"></param>
        /// <param name="allLightSessions"></param>
        /// <param name="takenFlats"></param>
        /// <returns></returns>
        public List<LightSession> GetNeededTargetCompletionFlats(List<Target> targets, List<LightSession> allLightSessions, List<FlatHistory> takenFlats) {
            List<LightSession> missingLightSessions = new List<LightSession>();

            foreach (Target target in targets) {

                // Get the light sessions and flat history for this target
                List<LightSession> targetLightSessions = allLightSessions.Where(ls => ls.TargetId == target.Id).ToList();
                List<FlatHistory> targetFlatHistory = takenFlats.Where(tf => tf.TargetId == target.Id).ToList();

                foreach (LightSession lightSession in targetLightSessions) {
                    missingLightSessions.Add(lightSession);
                    foreach (FlatHistory flatHistory in targetFlatHistory) {

                        // Remove if there is a flat set for the light session
                        if (lightSession.SessionDate == flatHistory.LightSessionDate
                            && lightSession.SessionId == flatHistory.LightSessionId
                            && lightSession.FlatSpec.Equals(new FlatSpec(flatHistory))) {
                            missingLightSessions.Remove(lightSession);
                            continue;
                        }
                    }
                }
            }

            return missingLightSessions;
        }

        /// <summary>
        /// Remove light sessions from the needed list if the same flat spec is in one of the history records.
        /// Note that the comparison ignores the SessionId since this is used for immediate flats.
        /// </summary>
        /// <param name="neededFlats"></param>
        /// <param name="takenFlats"></param>
        /// <returns></returns>
        public List<LightSession> CullFlatsByHistory(List<LightSession> neededFlats, List<FlatHistory> takenFlats) {

            if (takenFlats.Count == 0) {
                return neededFlats;
            }

            List<LightSession> culledList = new List<LightSession>();
            foreach (LightSession lightSession in neededFlats) {
                culledList.Add(lightSession);
                foreach (FlatHistory flatHistory in takenFlats) {
                    if (lightSession.SessionDate == flatHistory.LightSessionDate &&
                        lightSession.TargetId == flatHistory.TargetId &&
                        lightSession.FlatSpec.Equals(new FlatSpec(flatHistory))) {
                        culledList.Remove(lightSession);
                        break;
                    }
                }
            }

            return culledList;
        }

        /// <summary>
        /// A light session date is just a date/time marker that groups exposures into a single 'session'.
        /// All instances are set to noon indicating that the associated session is the upcoming period of
        /// darkness (immediate dusk to following dawn).
        /// </summary>
        /// <param name="exposureDate"></param>
        /// <returns></returns>
        public DateTime GetLightSessionDate(DateTime exposureDate) {
            return (exposureDate.Hour >= 12 && exposureDate.Hour <= 23)
                ? exposureDate.Date.AddHours(12)
                : exposureDate.Date.AddDays(-1).AddHours(12);
        }

        public string GetSessionIdentifier(int? sessionId) {
            int id = (sessionId != null) ? (int)sessionId : 0;
            return string.Format("{0:D4}", id);
        }

        /// <summary>
        /// This method encapsulates getting needed flats for cadence or target completed profiles/targets - including
        /// the database access.  It's here so that it can be shared between TS Flats instruction and TS Condition.
        /// </summary>
        /// <param name="activeProfile"></param>
        /// <param name="database"></param>
        /// <returns></returns>
        public List<LightSession> GetNeededCadenceOrCompletedTargetFlats(IProfile activeProfile, SchedulerDatabaseInteraction database) {

            List<LightSession> neededFlats = new List<LightSession>();
            DateTime cutoff = DateTime.Now.Date.AddDays(FlatsExpert.ACQUIRED_IMAGES_CUTOFF_DAYS);
            string profileId = activeProfile.Id.ToString();

            using (var context = database.GetContext()) {
                List<Project> activeProjects = context.GetActiveProjects(profileId);
                List<AcquiredImage> acquiredImages = context.GetAcquiredImages(profileId, cutoff);

                // Handle flats taken periodically
                List<Target> targets = GetTargetsForPeriodicFlats(activeProjects);
                if (targets.Count > 0) {
                    List<LightSession> lightSessions = GetLightSessions(targets, acquiredImages);
                    if (lightSessions.Count > 0) {
                        List<FlatHistory> takenFlats = context.GetFlatsHistory(targets);
                        neededFlats.AddRange(GetNeededPeriodicFlats(DateTime.Now, targets, lightSessions, takenFlats));
                    }
                    else {
                        TSLogger.Info("TS Flats: no light sessions for targets active for periodic flats");
                    }
                }
                else {
                    TSLogger.Info("TS Flats: no targets active for periodic flats");
                }

                // Add any flats needed for target completion targets
                targets = GetCompletedTargetsForFlats(activeProjects);
                if (targets.Count > 0) {
                    List<LightSession> lightSessions = GetLightSessions(targets, acquiredImages);
                    if (lightSessions.Count > 0) {
                        List<FlatHistory> takenFlats = context.GetFlatsHistory(targets);
                        // TODO: implement AlwaysRepeatFlatSet here
                        // BUT what does it mean here?  What's the 'repeat time span'?  Same as cadence?
                        neededFlats.AddRange(GetNeededTargetCompletionFlats(targets, lightSessions, takenFlats));
                    }
                    else {
                        TSLogger.Info("TS Flats: no light sessions for targets active for target completed flats");
                    }
                }
                else {
                    TSLogger.Info("TS Flats: no targets active for target completed flats");
                }

                if (neededFlats.Count == 0) {
                    TSLogger.Info("TS Flats: no flats needed");
                    return null;
                }

                // Sort in increasing rotation angle order to minimize rotator movements, then by target ID
                return neededFlats.OrderBy(ls => ls.FlatSpec.Rotation).ThenBy(ls => ls.TargetId).ToList();
            }
        }
    }

    public class FlatSpec : IEquatable<FlatSpec> {

        public string FilterName { get; private set; }
        public int Gain { get; private set; }
        public int Offset { get; private set; }
        public BinningMode BinningMode { get; private set; }
        public int ReadoutMode { get; private set; }
        public double Rotation { get; private set; }
        public double ROI { get; private set; }
        public string Key { get; private set; }

        public FlatSpec(string filterName, int gain, int offset, BinningMode binning, int readoutMode, double rotation, double roi) {
            FilterName = filterName;
            Gain = gain;
            Offset = offset;
            BinningMode = binning;
            ReadoutMode = readoutMode;
            Rotation = rotation;
            ROI = roi;
            Key = GetKey();
        }

        public FlatSpec(AcquiredImage exposure) {
            FilterName = exposure.FilterName;
            Gain = exposure.Metadata.Gain;
            Offset = exposure.Metadata.Offset;
            BinningMode bin;
            BinningMode.TryParse(exposure.Metadata.Binning, out bin);
            BinningMode = bin;
            ReadoutMode = exposure.Metadata.ReadoutMode;
            Rotation = exposure.Metadata.RotatorMechanicalPosition;
            ROI = exposure.Metadata.ROI;
            Key = GetKey();
        }

        public FlatSpec(FlatHistory flatHistory) {
            FilterName = flatHistory.FilterName;
            Gain = flatHistory.Gain;
            Offset = flatHistory.Offset;
            BinningMode = flatHistory.BinningMode;
            ReadoutMode = flatHistory.ReadoutMode;
            Rotation = flatHistory.Rotation;
            ROI = flatHistory.ROI;
            Key = GetKey();
        }

        private string GetKey() {
            string rotationKey = Rotation != ImageMetadata.NO_ROTATOR_ANGLE ? $"{Rotation}" : "na";
            return $"{FilterName}_{Gain}_{Offset}_{BinningMode}_{ReadoutMode}_{rotationKey}_{ROI}";
        }

        public bool Equals(FlatSpec other) {
            if (other is null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }

            return Key == other.Key;
        }

        public override string ToString() {
            string rot = Rotation != ImageMetadata.NO_ROTATOR_ANGLE ? Rotation.ToString() : "n/a";
            return $"filter:{FilterName} gain:{Gain} offset:{Offset} bin:{BinningMode} readout:{ReadoutMode} rot:{rot} roi: {ROI}";
        }
    }

    public class LightSession : IComparable, IEquatable<LightSession> {

        public int TargetId { get; private set; }
        public DateTime SessionDate { get; private set; }
        public int SessionId { get; private set; }
        public FlatSpec FlatSpec { get; private set; }

        public LightSession(int targetId, DateTime sessionDate, int sessionId, FlatSpec flatSpec) {
            TargetId = targetId;
            SessionDate = sessionDate;
            SessionId = sessionId;
            FlatSpec = flatSpec;
        }

        public override string ToString() {
            return $"{TargetId} {Utils.FormatDateTimeFull(SessionDate)} {SessionId} {FlatSpec}";
        }

        public int CompareTo(object obj) {
            LightSession lightSession = obj as LightSession;
            return (lightSession != null) ? SessionDate.CompareTo(lightSession.SessionDate) : 0;
        }

        public bool Equals(LightSession other) {
            if (other is null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }

            return TargetId == other.TargetId
                && SessionDate == other.SessionDate
                && SessionId == other.SessionId
                && FlatSpec.Equals(other.FlatSpec);
        }
    }
}
