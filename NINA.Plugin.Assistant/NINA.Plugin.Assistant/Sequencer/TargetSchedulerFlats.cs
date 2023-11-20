using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    [ExportMetadata("Name", "Target Scheduler Flats")]
    [ExportMetadata("Description", "Flats automation for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerFlats : SequentialContainer {

        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IFilterWheelMediator filterWheelMediator;
        private IFlatDeviceMediator flatDeviceMediator;

        private Dictionary<string, FlatSpec> neededFlats = new Dictionary<string, FlatSpec>();

        [ImportingConstructor]
        public TargetSchedulerFlats(IProfileService profileService, ICameraMediator cameraMediator, IImagingMediator imagingMediator, IImageSaveMediator imageSaveMediator, IImageHistoryVM imageHistoryVM, IFilterWheelMediator filterWheelMediator, IFlatDeviceMediator flatDeviceMediator) {
            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.flatDeviceMediator = flatDeviceMediator;
        }

        public TargetSchedulerFlats(TargetSchedulerFlats cloneMe) : this(
            cloneMe.profileService,
            cloneMe.cameraMediator,
            cloneMe.imagingMediator,
            cloneMe.imageSaveMediator,
            cloneMe.imageHistoryVM,
            cloneMe.filterWheelMediator,
            cloneMe.flatDeviceMediator) {
            CopyMetaData(cloneMe);
        }

        public override object Clone() {
            return new TargetSchedulerFlats(this);
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {


            return Task.CompletedTask;
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext context) {
            this.Items.Clear();
            this.Conditions.Clear();
            this.Triggers.Clear();
        }

    }
}
