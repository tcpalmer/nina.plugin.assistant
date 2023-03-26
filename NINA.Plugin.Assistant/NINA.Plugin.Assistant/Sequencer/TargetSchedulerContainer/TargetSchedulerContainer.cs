using Assistant.NINAPlugin.Astrometry;
using Assistant.NINAPlugin.Plan;
using Newtonsoft.Json;
using NINA.Astrometry;
using NINA.Astrometry.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Assistant.NINAPlugin.Sequencer {

    /// <summary>
    /// Cribbed from NINA.Sequencer.Container.DeepSkyObjectContainer
    /// </summary>
    [ExportMetadata("Name", "Target Scheduler Container")]
    [ExportMetadata("Description", "Container for Target Scheduler")]
    [ExportMetadata("Icon", "Scheduler.SchedulerSVG")]
    [ExportMetadata("Category", "Target Scheduler")]
    [Export(typeof(ISequenceItem))]
    [Export(typeof(ISequenceContainer))]
    [JsonObject(MemberSerialization.OptIn)]
    public class TargetSchedulerContainer : SequenceContainer, IDeepSkyObjectContainer {
        private readonly IProfileService profileService;
        private readonly IFramingAssistantVM framingAssistantVM;
        private readonly IApplicationMediator applicationMediator;

        private object lockObj = new object();
        private TargetSchedulerContainerStrategy containerStrategy;
        private INighttimeCalculator nighttimeCalculator;
        private InputTarget target;

        [ImportingConstructor]
        public TargetSchedulerContainer(
                IProfileService profileService,
                INighttimeCalculator nighttimeCalculator,
                IFramingAssistantVM framingAssistantVM,
                IApplicationMediator applicationMediator) : base(new TargetSchedulerContainerStrategy()) {

            this.profileService = profileService;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;

            this.profileService = profileService;
            this.nighttimeCalculator = nighttimeCalculator;
            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;

            this.containerStrategy = (TargetSchedulerContainerStrategy)Strategy;

            Task.Run(() => NighttimeData = nighttimeCalculator.Calculate());
            Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon);
            SetTarget(DateTime.Now, null);

            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.LocationChanged), ProfileService_LocationChanged);
            WeakEventManager<IProfileService, EventArgs>.AddHandler(profileService, nameof(profileService.HorizonChanged), ProfileService_HorizonChanged);
            WeakEventManager<INighttimeCalculator, EventArgs>.AddHandler(nighttimeCalculator, nameof(nighttimeCalculator.OnReferenceDayChanged), NighttimeCalculator_OnReferenceDayChanged);

            // TODO: Need to figure out a way to block or warn about adding Loop Conditions
            // Following doesn't work, nor does trying to override the Add()
            //((ObservableCollection<ISequenceCondition>)Conditions).CollectionChanged += LoopConditions_CollectionChanged;
        }

        private void LoopConditions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            Logger.Debug("IN LoopConditions_CollectionChanged");
            Notification.ShowWarning("you don't want to do this");
        }

        /*
         * TODO:
         *   - Validation: confirm TS instruction is one of the instructions.  Do any other containers do validation?
         *     Just implement IValidatable
         *   - We might need to think about doing a lock on the Target.  Since triggers like MF could be accessing it,
         *     we need to ensure a smooth transition when it changes.  Maybe for NighttimeData too.
         */

        public void ClearTarget() {
            lock (lockObj) {
                Target = null;
                ProjectTargetDisplay = "";
                CoordinatesDisplay = "";
                StopAtDisplay = "";

                RaisePropertyChanged(nameof(ProjectTargetDisplay));
                RaisePropertyChanged(nameof(CoordinatesDisplay));
                RaisePropertyChanged(nameof(StopAtDisplay));
                RaisePropertyChanged(nameof(NighttimeData));
                RaisePropertyChanged(nameof(Target));
            }
        }

        public void SetTarget(DateTime atTime, IPlanTarget planTarget) {
            lock (lockObj) {
                IProfile activeProfile = profileService.ActiveProfile;
                DateTime referenceDate = NighttimeCalculator.GetReferenceDate(atTime);
                //CustomHorizon customHorizon = GetCustomHorizon(activeProfile, planTarget.Project);
                CustomHorizon customHorizon = GetCustomHorizon(activeProfile, null);

                InputTarget inputTarget = new InputTarget(
                    Angle.ByDegree(activeProfile.AstrometrySettings.Latitude),
                    Angle.ByDegree(activeProfile.AstrometrySettings.Longitude),
                    customHorizon);

                Coordinates coords = new Coordinates(Angle.ByDegree(15), Angle.ByDegree(5), Epoch.J2000);

                inputTarget.DeepSkyObject = GetDeepSkyObject(referenceDate, activeProfile, planTarget, customHorizon);
                inputTarget.TargetName = "MyTarget";
                inputTarget.InputCoordinates = new InputCoordinates(coords);
                inputTarget.Rotation = 12;
                //inputTarget.TargetName = planTarget.Name;
                //inputTarget.InputCoordinates = new InputCoordinates(planTarget.Coordinates);
                //inputTarget.Rotation = planTarget.Rotation;
                Target = inputTarget;

                ProjectTargetDisplay = "MyProject / MyTarget";
                CoordinatesDisplay = $"{inputTarget.InputCoordinates.RAHours}h  {inputTarget.InputCoordinates.RAMinutes}m  {inputTarget.InputCoordinates.RASeconds}s   " +
                                    $"{inputTarget.InputCoordinates.DecDegrees}°  {inputTarget.InputCoordinates.DecMinutes}'  {inputTarget.InputCoordinates.DecSeconds}\",   " +
                                    $"Rotation FIXROT°";
                //StopAtDisplay = $"{planTarget.EndTime:HH:mm:ss}";
                StopAtDisplay = $"{DateTime.Now.AddMinutes(10):HH:mm:ss}";
            }
        }

        public void RunTriggers() {
            containerStrategy.RunTriggers();
        }

        public NighttimeData NighttimeData { get; private set; }

        public InputTarget Target {
            get => target;
            set {
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.RemoveHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                target = value;
                if (Target != null) {
                    WeakEventManager<InputTarget, EventArgs>.AddHandler(Target, nameof(Target.CoordinatesChanged), Target_OnCoordinatesChanged);
                }
                RaisePropertyChanged();
            }
        }

        public string ProjectTargetDisplay { get; private set; }
        public string CoordinatesDisplay { get; private set; }
        public string StopAtDisplay { get; private set; }

        private void Target_OnCoordinatesChanged(object sender, EventArgs e) {
            AfterParentChanged();
        }

        private void NighttimeCalculator_OnReferenceDayChanged(object sender, EventArgs e) {
            NighttimeData = nighttimeCalculator.Calculate();
            RaisePropertyChanged(nameof(NighttimeData));
        }

        private void ProfileService_HorizonChanged(object sender, EventArgs e) {
            Target?.DeepSkyObject?.SetCustomHorizon(profileService.ActiveProfile.AstrometrySettings.Horizon);
        }

        private void ProfileService_LocationChanged(object sender, EventArgs e) {
            Target?.SetPosition(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude));
        }

        private CustomHorizon GetCustomHorizon(IProfile activeProfile, IPlanProject project) {
            /*
            CustomHorizon customHorizon = project.UseCustomHorizon && activeProfile.AstrometrySettings.Horizon != null ?
                activeProfile.AstrometrySettings.Horizon :
                HorizonDefinition.GetConstantHorizon(project.MinimumAltitude);
            return customHorizon;
            */
            return HorizonDefinition.GetConstantHorizon(10);
        }

        private DeepSkyObject GetDeepSkyObject(DateTime referenceDate, IProfile activeProfile, IPlanTarget planTarget, CustomHorizon customHorizon) {
            /*
            DeepSkyObject dso = new DeepSkyObject(string.Empty, planTarget.Coordinates, null, customHorizon);
            dso.Name = planTarget.Name;
            dso.SetDateAndPosition(referenceDate, activeProfile.AstrometrySettings.Latitude, activeProfile.AstrometrySettings.Longitude);
            dso.Refresh();
            return dso;
            */
            Coordinates coords = new Coordinates(Angle.ByDegree(15), Angle.ByDegree(5), Epoch.J2000);
            DeepSkyObject dso = new DeepSkyObject(string.Empty, coords, null, customHorizon);
            dso.Name = "My Target";
            dso.SetDateAndPosition(referenceDate, activeProfile.AstrometrySettings.Latitude, activeProfile.AstrometrySettings.Longitude);
            dso.Refresh();
            return dso;
        }

        public override object Clone() {
            var clone = new TargetSchedulerContainer(profileService, nighttimeCalculator, framingAssistantVM, applicationMediator) {
                Icon = Icon,
                Name = Name,
                Category = Category,
                Description = Description,
                Items = new ObservableCollection<ISequenceItem>(Items.Select(i => i.Clone() as ISequenceItem)),
                Triggers = new ObservableCollection<ISequenceTrigger>(Triggers.Select(t => t.Clone() as ISequenceTrigger)),
                Conditions = new ObservableCollection<ISequenceCondition>(Conditions.Select(t => t.Clone() as ISequenceCondition)),
                Target = new InputTarget(Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Latitude), Angle.ByDegree(profileService.ActiveProfile.AstrometrySettings.Longitude), profileService.ActiveProfile.AstrometrySettings.Horizon)
            };

            clone.Target.TargetName = this.Target.TargetName;
            clone.Target.InputCoordinates.Coordinates = this.Target.InputCoordinates.Coordinates.Transform(Epoch.J2000);
            clone.Target.Rotation = this.Target.Rotation;

            foreach (var item in clone.Items) {
                item.AttachNewParent(clone);
            }

            foreach (var condition in clone.Conditions) {
                condition.AttachNewParent(clone);
            }

            foreach (var trigger in clone.Triggers) {
                trigger.AttachNewParent(clone);
            }

            return clone;
        }

        public override string ToString() {
            var baseString = base.ToString();
            return $"{baseString}, Target: {Target?.TargetName} {Target?.InputCoordinates?.Coordinates} {Target?.Rotation}";
        }
    }
}
