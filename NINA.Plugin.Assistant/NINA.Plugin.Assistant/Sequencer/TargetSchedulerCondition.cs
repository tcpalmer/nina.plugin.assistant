using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility.Notification;
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
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerCondition : SequenceCondition {

        private readonly string TARGETS_REMAIN = "While Targets Remain Tonight";
        private readonly string ACTIVE_PROJECTS_REMAIN = "While Active Projects Remain";

        private IProfileService profileService;

        [ImportingConstructor]
        public TargetSchedulerCondition(IProfileService profileService) {
            this.profileService = profileService;

            Modes = new List<string>() { TARGETS_REMAIN, ACTIVE_PROJECTS_REMAIN };
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

        public static readonly bool DISABLE = true;
        public static bool WARNED = false;

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (DISABLE) {
                if (!WARNED) {
                    Notification.ShowInformation("REMINDER: TS Condition disabled, always true");
                    WARNED = true;
                }

                return true;
            }

            return SelectedMode == TARGETS_REMAIN ? HasRemainingTargets() : HasActiveProjects();
        }

        private bool HasRemainingTargets() {
            try {
                Planner planner = new Planner(DateTime.Now, profileService, GetProfilePreferences());

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
                Planner planner = new Planner(DateTime.Now, profileService, GetProfilePreferences());
                bool result = planner.HasActiveProjects(null);
                TSLogger.Info($"TargetSchedulerCondition check for active projects, continue={result}");
                return result;
            }
            catch (Exception ex) {
                TSLogger.Error($"exception determining active projects: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"TargetSchedulerCondition: exception determining active projects: {ex.Message}", ex);
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
