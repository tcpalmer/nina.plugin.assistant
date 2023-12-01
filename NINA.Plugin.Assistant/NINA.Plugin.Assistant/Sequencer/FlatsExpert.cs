using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model.Equipment;
using NINA.Plugin.Assistant.Shared.Utility;
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
                    project.FlatsHandling == Project.FLATS_HANDLING_TARGET_COMPLETION) { continue; }

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
                    LightSession lightSession = new LightSession(exposure.TargetId, GetLightSessionDate(exposure.AcquiredDate), new FlatSpec(exposure));

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
                int flatsPeriod = target.Project.FlatsHandling;

                // Get the light sessions and flat history for this target
                List<LightSession> targetLightSessions = allLightSessions.Where(ls => ls.TargetId == target.Id).ToList();
                List<FlatHistory> targetFlatHistory = takenFlats.Where(tf => tf.TargetId == target.Id).ToList();

                List<LightSession> potentialLightSessions = new List<LightSession>();

                foreach (LightSession lightSession in targetLightSessions) {
                    potentialLightSessions.Add(lightSession);
                    foreach (FlatHistory flatHistory in targetFlatHistory) {

                        // Remove if there is a flat set for the light session
                        if (lightSession.SessionDate == flatHistory.LightSessionDate && lightSession.FlatSpec.Equals(FlatSpecFromFlatHistory(flatHistory))) {
                            potentialLightSessions.Remove(lightSession);
                            continue;
                        }
                    }
                }

                // Skip if not enough days have passed based on project setting
                foreach (LightSession lightSession in potentialLightSessions) {
                    if ((checkDate - lightSession.SessionDate).TotalDays < flatsPeriod) { continue; }
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
                        if (lightSession.SessionDate == flatHistory.LightSessionDate && lightSession.FlatSpec.Equals(FlatSpecFromFlatHistory(flatHistory))) {
                            missingLightSessions.Remove(lightSession);
                            continue;
                        }
                    }
                }
            }

            return missingLightSessions;
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

        private FlatSpec FlatSpecFromFlatHistory(FlatHistory flatHistory) {
            return new FlatSpec(flatHistory.FilterName, flatHistory.Gain, flatHistory.Offset, flatHistory.BinningMode, flatHistory.ReadoutMode, flatHistory.Rotation, flatHistory.ROI);
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
        public FlatSpec FlatSpec { get; private set; }

        public LightSession(int targetId, DateTime sessionDate, FlatSpec flatSpec) {
            TargetId = targetId;
            SessionDate = sessionDate;
            FlatSpec = flatSpec;
        }

        public override string ToString() {
            return $"{TargetId} {Utils.FormatDateTimeFull(SessionDate)} {FlatSpec}";
        }

        public int CompareTo(object obj) {
            LightSession lightSession = obj as LightSession;
            return (lightSession != null) ? SessionDate.CompareTo(lightSession.SessionDate) : 0;
        }

        public bool Equals(LightSession other) {
            if (other is null) { return false; }
            if (ReferenceEquals(this, other)) { return true; }
            if (GetType() != other.GetType()) { return false; }

            return TargetId == other.TargetId && SessionDate == other.SessionDate && FlatSpec.Equals(other.FlatSpec);
        }
    }
}
