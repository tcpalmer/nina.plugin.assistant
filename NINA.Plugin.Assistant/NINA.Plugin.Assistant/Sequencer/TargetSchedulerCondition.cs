using Assistant.NINAPlugin.Controls.Util;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Sync;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Plugin.Assistant.SyncService.Sync;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Condition")]
    [ExportMetadata("Description", "Loop condition for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerCondition : SequenceCondition {
        private const string TARGETS_REMAIN = "While Targets Remain Tonight";
        private const string ACTIVE_PROJECTS_REMAIN = "While Active Projects Remain";
        private const string FLATS_NEEDED = "While Flats Needed";

        private IProfileService profileService;

        [ImportingConstructor]
        public TargetSchedulerCondition(IProfileService profileService) {
            this.profileService = profileService;

            Modes = new List<string>() { TARGETS_REMAIN, ACTIVE_PROJECTS_REMAIN, FLATS_NEEDED };
            SelectedMode = TARGETS_REMAIN;
        }

        public List<string> Modes { get; }
        private string selectedMode;

        [JsonProperty]
        public string SelectedMode {
            get => selectedMode;
            set {
                selectedMode = value;
                RaisePropertyChanged();
            }
        }

        private TargetSchedulerCondition(TargetSchedulerCondition cloneMe, IProfileService profileService) : this(profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TargetSchedulerCondition(this, profileService) {
                SelectedMode = this.SelectedMode
            };
        }

        private bool checkIsActive = true;
        public bool CheckIsActive { get => checkIsActive; set => checkIsActive = value; }

        /// <summary>
        /// If the main TS container has ended normally (no more targets) then it will call this to force
        /// a full recheck even if the container this condition belongs too hasn't finished an iteration.
        /// </summary>
        public void EnableCheckIsActive() {
            TSLogger.Info("TargetSchedulerCondition informed of need to execute check on next attempt");
            CheckIsActive = true;
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (!CheckIsActive) {
                return true;
            }

            TSLogger.Info($"TargetSchedulerCondition starting check: {SelectedMode}");
            CheckIsActive = false;

            switch (SelectedMode) {
                case TARGETS_REMAIN: return HasRemainingTargets();
                case ACTIVE_PROJECTS_REMAIN: return HasActiveProjects();
                case FLATS_NEEDED: return NeedsFlats();
            }

            return false;
        }

        public override void SequenceBlockFinished() {
            TSLogger.Info($"TargetSchedulerCondition: SequenceBlockFinished");
            CheckIsActive = true;
        }

        public override void ResetProgress() {
            TSLogger.Info($"TargetSchedulerCondition: ResetProgress");
            Status = SequenceEntityStatus.CREATED;
            CheckIsActive = true;
        }

        public override void Initialize() {
            TSLogger.Info($"TargetSchedulerCondition: Initialize");
        }

        public override void SequenceBlockInitialize() {
            TSLogger.Info($"TargetSchedulerCondition: SequenceBlockInitialize");
        }

        public override void SequenceBlockStarted() {
            TSLogger.Info($"TargetSchedulerCondition: SequenceBlockStarted");
        }

        public override void SequenceBlockTeardown() {
            TSLogger.Info($"TargetSchedulerCondition: SequenceBlockTeardown");
        }

        public override void Teardown() {
            TSLogger.Info($"TargetSchedulerCondition: Teardown");
        }

        private bool HasRemainingTargets() {
            try {
                Planner planner = new Planner(DateTime.Now, GetApplicableProfile(), GetProfilePreferences(), true);
                bool result = planner.GetPlan(null) != null;
                TSLogger.Info($"TargetSchedulerCondition check for remaining targets, continue={result}");
                return result;
            } catch (Exception ex) {
                TSLogger.Error($"exception determining remaining targets: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining remaining targets: {ex.Message}", ex);
            }
        }

        private bool HasActiveProjects() {
            try {
                Planner planner = new Planner(DateTime.Now, GetApplicableProfile(), GetProfilePreferences(), true);
                bool result = planner.HasActiveProjects(null);
                TSLogger.Info($"TargetSchedulerCondition check for active projects, continue={result}");
                return result;
            } catch (Exception ex) {
                TSLogger.Error($"exception determining active projects: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining active projects: {ex.Message}", ex);
            }
        }

        private bool NeedsFlats() {
            try {
                FlatsExpert flatsExpert = new FlatsExpert() { VerboseLogging = false };
                bool result = flatsExpert.GetNeededFlats(profileService.ActiveProfile, DateTime.Now).Count > 0;
                TSLogger.Info($"TargetSchedulerCondition check for needed flats, continue={result}");
                return result;
            } catch (Exception ex) {
                TSLogger.Error($"exception determining needed flats: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining needed flats: {ex.Message}", ex);
            }
        }

        private IProfile GetApplicableProfile() {
            if (!SyncManager.Instance.RunningClient) {
                return profileService.ActiveProfile;
            }

            // If running as a sync client, we need to use the server's profile for TS database queries used by the Planner
            string serverProfileId = SyncClient.Instance.ServerProfileId;

            try {
                ProfileMeta profileMeta = profileService.Profiles.Where(p => p.Id.ToString() == serverProfileId).FirstOrDefault();
                if (profileMeta != null) {
                    IProfile serverProfile = ProfileLoader.Load(profileService, profileMeta);
                    TSLogger.Info($"sync client using server profile for TS condition: {serverProfileId}");
                    return serverProfile;
                }

                TSLogger.Warning($"sync client could not load server profile id={serverProfileId}, defaulting to sync client profile");
                return profileService.ActiveProfile;
            } catch (Exception e) {
                TSLogger.Error($"sync client failed to load server profile id={serverProfileId}: {e.Message}");
                return profileService.ActiveProfile;
            }
        }

        private ProfilePreference GetProfilePreferences() {
            SchedulerPlanLoader loader = new SchedulerPlanLoader(profileService.ActiveProfile);
            return loader.GetProfilePreferences(new SchedulerDatabaseInteraction().GetContext());
        }

        public override string ToString() {
            return $"Condition: {nameof(TargetSchedulerCondition)} mode={SelectedMode}";
        }
    }
}