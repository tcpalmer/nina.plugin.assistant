using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.SequenceItem;
using System;
using System.ComponentModel.Composition;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Loop while projects incomplete")]
    [ExportMetadata("Description", "Loop condition for Target Scheduler, will loop until all projects are completed")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Condition")]
    [Export(typeof(ISequenceCondition))]
    [JsonObject(MemberSerialization.OptIn)]
    public class IncompleteProjectCondition : SequenceCondition {

        private readonly IProfileService profileService;

        [ImportingConstructor]
        public IncompleteProjectCondition(IProfileService profileService) {
            this.profileService = profileService;
        }

        private IncompleteProjectCondition(IncompleteProjectCondition cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new IncompleteProjectCondition(this);
        }

        public override bool Check(ISequenceItem previousItem, ISequenceItem nextItem) {
            try
            {
                SchedulerPlanLoader loader = new SchedulerPlanLoader(profileService.ActiveProfile);
                SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
                Planner plan = new Planner(DateTime.Now, profileService, loader.GetProfilePreferences(database.GetContext()));

                return plan.ProjectsIncomplete();
            }
            catch (Exception ex)
            {
                TSLogger.Error($"exception reading database: {ex.StackTrace}");
                throw new SequenceEntityFailedException($"Scheduler: exception reading database: {ex.Message}", ex);
            }
        }

        public override string ToString() {
            return $"Condition: {nameof(IncompleteProjectCondition)}";
        }

    }
}
