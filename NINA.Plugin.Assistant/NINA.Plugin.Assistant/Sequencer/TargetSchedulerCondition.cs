using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Newtonsoft.Json;
using NINA.Core.Enum;
using NINA.Core.Model;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

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

        private bool blockFinished = false;

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {

            if (!blockFinished) {
                return true;
            }

            blockFinished = false;

            switch (SelectedMode) {
                case TARGETS_REMAIN: return HasRemainingTargets();
                case ACTIVE_PROJECTS_REMAIN: return HasActiveProjects();
                case FLATS_NEEDED: return NeedsFlats();
            }

            return false;
        }

        public override void SequenceBlockFinished() {
            blockFinished = true;
        }

        public override void ResetProgress() {
            Status = SequenceEntityStatus.CREATED;
            blockFinished = false;
        }

        private bool HasRemainingTargets() {
            try {
                Planner planner = new Planner(DateTime.Now, profileService, GetProfilePreferences(), true);

                bool result = planner.GetPlan(null) != null;
                TSLogger.Info($"TargetSchedulerCondition check for remaining targets, continue={result}");
                return result;
            }
            catch (Exception ex) {
                TSLogger.Error($"exception determining remaining targets: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining remaining targets: {ex.Message}", ex);
            }
        }

        private bool HasActiveProjects() {
            try {
                Planner planner = new Planner(DateTime.Now, profileService, GetProfilePreferences(), true);
                bool result = planner.HasActiveProjects(null);
                TSLogger.Info($"TargetSchedulerCondition check for active projects, continue={result}");
                return result;
            }
            catch (Exception ex) {
                TSLogger.Error($"exception determining active projects: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining active projects: {ex.Message}", ex);
            }
        }

        private bool NeedsFlats() {
            try {
                bool result = new FlatsExpert().GetNeededFlats(profileService.ActiveProfile, DateTime.Now).Count > 0;
                TSLogger.Info($"TargetSchedulerCondition check for needed flats, continue={result}");
                return result;
            }
            catch (Exception ex) {
                TSLogger.Error($"exception determining needed flats: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining needed flats: {ex.Message}", ex);
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
