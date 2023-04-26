using Assistant.NINAPlugin.Util;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Utility;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    /* disabling for now
    [ExportMetadata("Name", "Target Scheduler Test Trigger")]
    [ExportMetadata("Description", "Test Trigger for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Lbl_SequenceCategory_Container")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    */
    public class SchedulerTestTrigger : SequenceTrigger {

        // Tested for Target retrieval and working:
        // - In AfterParentChanged() like CenterAfterDriftTrigger
        // - In Execute(), like MeridianFlipTrigger

        private InputCoordinates Coordinates { get; set; }

        [JsonProperty]
        public int StateCounter { get; set; }

        [ImportingConstructor]
        public SchedulerTestTrigger() {
            Coordinates = new InputCoordinates();
        }

        public override Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {

            var contextCoordinates = ItemUtility.RetrieveContextCoordinates(Parent);
            if (contextCoordinates != null) {
                Coordinates.Coordinates = contextCoordinates.Coordinates;
                TSLogger.Debug($"TEST TRIGGER: p={GetParentType()} retrieved coords {CoordString()}");
            }

            TSLogger.Debug($"TEST TRIGGER: Execute p={GetParentType()} {StateCounter} {CoordString()}");
            StateCounter++;
            return Task.CompletedTask;
        }

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            TSLogger.Debug($"TEST TRIGGER: ShouldTrigger p={GetParentType()} {CoordString()}");
            return true;
        }

        public override bool ShouldTriggerAfter(ISequenceItem previousItem, ISequenceItem nextItem) {
            TSLogger.Debug($"TEST TRIGGER: ShouldTriggerAfter p={GetParentType()} {CoordString()}");
            return true;
        }

        public override void SequenceBlockInitialize() {
            TSLogger.Debug($"TEST TRIGGER: SequenceBlockInitialize p={GetParentType()}");
        }

        public override void SequenceBlockStarted() {
            TSLogger.Debug($"TEST TRIGGER: SequenceBlockStarted p={GetParentType()}");
        }

        public override void SequenceBlockFinished() {
            TSLogger.Debug($"TEST TRIGGER: SequenceBlockFinished p={GetParentType()}");
        }

        public override void SequenceBlockTeardown() {
            TSLogger.Debug($"TEST TRIGGER: SequenceBlockTeardown p={GetParentType()}");
        }

        public override void AfterParentChanged() {
            TSLogger.Debug($"TEST TRIGGER: AfterParentChanged p={GetParentType()}");

            /*
            if (Parent == null) {
                SequenceBlockTeardown();
            }
            else {
                var contextCoordinates = ItemUtility.RetrieveContextCoordinates(Parent);
                if (contextCoordinates != null) {
                    Coordinates.Coordinates = contextCoordinates.Coordinates;
                }

                TSLogger.Debug($"TEST TRIGGER: retrieved coords {CoordString()}");

                if (Parent.Status == SequenceEntityStatus.RUNNING) {
                    SequenceBlockInitialize();
                }
            }
            */
        }

        private string GetParentType() {
            return Parent != null ? Parent.GetType().ToString() : "";
        }

        private string CoordString() {
            return Coordinates == null ? "NULL" : $"{Coordinates.Coordinates.RAString} {Coordinates.Coordinates.DecString}";
        }

        public override object Clone() {
            return new SchedulerTestTrigger(this) {
                StateCounter = StateCounter,
                Coordinates = Coordinates?.Clone()
            };
        }

        private SchedulerTestTrigger(SchedulerTestTrigger cloneMe) {
            CopyMetaData(cloneMe);
        }
    }
}
