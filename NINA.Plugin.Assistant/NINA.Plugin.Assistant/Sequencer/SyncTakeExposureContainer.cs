using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Sync;
using Assistant.NINAPlugin.Util;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Container.ExecutionStrategy;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Camera;
using NINA.Sequencer.SequenceItem.FilterWheel;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant.NINAPlugin.Sequencer {

    public class SyncTakeExposureContainer : SequenceContainer {

        private IProfileService profileService;
        private ICameraMediator cameraMediator;
        private IImagingMediator imagingMediator;
        private IImageSaveMediator imageSaveMediator;
        private IImageHistoryVM imageHistoryVM;
        private IFilterWheelMediator filterWheelMediator;
        private ISyncImageSaveWatcher syncImageSaveWatcher;
        private SyncedExposure syncedExposure;
        private Action<string> UpdateDisplayTextAction;

        private ExposurePlan exposurePlan;
        private ExposureTemplate exposureTemplate;
        private Target target;

        public SyncTakeExposureContainer(IProfileService profileService,
                                         ICameraMediator cameraMediator,
                                         IImagingMediator imagingMediator,
                                         IImageSaveMediator imageSaveMediator,
                                         IImageHistoryVM imageHistoryVM,
                                         IFilterWheelMediator filterWheelMediator,
                                         ISyncImageSaveWatcher syncImageSaveWatcher,
                                         SyncedExposure syncedExposure,
                                         Action<String> UpdateDisplayTextAction) : base(new SequentialStrategy()) {

            this.profileService = profileService;
            this.cameraMediator = cameraMediator;
            this.imagingMediator = imagingMediator;
            this.imageSaveMediator = imageSaveMediator;
            this.imageHistoryVM = imageHistoryVM;
            this.filterWheelMediator = filterWheelMediator;
            this.syncImageSaveWatcher = syncImageSaveWatcher;
            this.syncedExposure = syncedExposure;
            this.UpdateDisplayTextAction = UpdateDisplayTextAction;

            Description = "";
            Category = PlanTargetContainer.INSTRUCTION_CATEGORY;
            LoadExposureDetails();
        }

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            Add(GetSwitchFilter());
            Add(GetSetReadoutMode());
            Add(GetTakeExposure());

            return base.Execute(progress, token);
        }

        private ISequenceItem GetTakeExposure() {
            return new SyncTakeExposure(exposurePlan, exposureTemplate, target, profileService, cameraMediator, imagingMediator, imageSaveMediator, imageHistoryVM, syncImageSaveWatcher, syncedExposure, UpdateDisplayTextAction);
        }

        private void LoadExposureDetails() {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                this.exposurePlan = context.GetExposurePlan(syncedExposure.ExposurePlanDatabaseId);
                this.exposureTemplate = GetExposureTemplate(context, exposurePlan);
                this.target = GetTarget(context, exposurePlan);
            }
        }

        private ExposureTemplate GetExposureTemplate(SchedulerDatabaseContext context, ExposurePlan exposurePlan) {

            // Get the template being used by the server instance
            ExposureTemplate serverExposureTemplate = context.GetExposureTemplate(exposurePlan.ExposureTemplateId);

            // If this (client) instance has a template by the same name, use that
            List<ExposureTemplate> list = context.GetExposureTemplates(SyncClient.Instance.ProfileId);
            foreach (ExposureTemplate et in list) {
                if (et.Name == serverExposureTemplate.Name) {
                    return et;
                }
            }

            // Otherwise use the same as the server
            return serverExposureTemplate;
        }

        private Target GetTarget(SchedulerDatabaseContext context, ExposurePlan exposurePlan) {
            return context.GetTargetOnly(exposurePlan.TargetId);
        }

        private ISequenceItem GetSwitchFilter() {
            SwitchFilter switchFilter = new SwitchFilter(profileService, filterWheelMediator);
            switchFilter.Filter = Utils.LookupFilter(profileService, exposureTemplate.FilterName);
            switchFilter.Category = PlanTargetContainer.INSTRUCTION_CATEGORY;

            return switchFilter;
        }

        private ISequenceItem GetSetReadoutMode() {
            SetReadoutMode setReadoutMode = new SetReadoutMode(cameraMediator);
            setReadoutMode.Mode = GetReadoutMode(exposureTemplate.ReadoutMode);
            setReadoutMode.Category = PlanTargetContainer.INSTRUCTION_CATEGORY;
            return setReadoutMode;
        }

        private short GetReadoutMode(int? readoutMode) {
            return (short)((readoutMode == null || readoutMode < 0) ? 0 : readoutMode);
        }

        public override object Clone() {
            throw new NotImplementedException();
        }
    }
}
