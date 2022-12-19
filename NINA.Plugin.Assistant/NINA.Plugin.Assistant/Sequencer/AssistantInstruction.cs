using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Jarvis")]
    [ExportMetadata("Description", "Run the Jarvis Assistant")]
    [ExportMetadata("Icon", "Jarvis.JarvisSVG")]
    [ExportMetadata("Category", "Sequencer Assistant")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class AssistantInstruction : SequenceItem, IValidatable {

        /*
         * Check out some methods on the parent:
         * - override GetEstimatedDuration: estimate of how long this will take
         */

        /*
         * Lifecycle:
         * - construct/clone: when added to a sequence
         * - initialize: when the sequence is started (not when execution starts)
         * - execute: when the instruction is started
         * - teardown: when the instruction has completed or canceled
         * 
         * So initialize is where we can call the Assistant to get the plan.
         * A cancel has to be handled, e.g. remove any instructions added to the sequence under the hood and clear the plan
         */

        private readonly IProfileService profileService;

        [ImportingConstructor]
        public AssistantInstruction(IProfileService profileService) {
            this.profileService = profileService;
            Logger.Debug("Jarvis ctor");
        }

        public AssistantInstruction(AssistantInstruction cloneMe) : this(cloneMe.profileService) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new AssistantInstruction(this) { };
        }

        public override void Initialize() {
            Logger.Debug("Jarvis initialize");
        }

        public override void Teardown() {
            Logger.Debug("Jarvis teardown");
        }

        public override string ToString() {
            return $"Category: {Category}, Item: {nameof(AssistantInstruction)}";
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Logger.Debug("Jarvis starting");

            for (int i = 0; i < 20; i++) {
                Thread.Sleep(1000);
                if (token.IsCancellationRequested) {
                    Logger.Debug("Jarvis canceling");
                    throw new OperationCanceledException();
                }
            }

            Logger.Debug("Jarvis complete");
            return Task.CompletedTask;
        }

        private IList<string> issues = new List<string>();
        public IList<string> Issues { get => issues; set { issues = value; RaisePropertyChanged(); } }

        public bool Validate() {
            // TODO: see RoboCopyStart for howto
            return true;
        }

    }
}
