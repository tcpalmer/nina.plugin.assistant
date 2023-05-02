using Assistant.NINAPlugin.Controls.Util;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Plan;
using Assistant.NINAPlugin.Util;
using LinqKit;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.PlanPreview {

    public class PlanPreviewerViewVM : BaseVM {

        private SchedulerDatabaseInteraction database;

        public PlanPreviewerViewVM(IProfileService profileService) : base(profileService) {
            database = new SchedulerDatabaseInteraction();
            profileService.ProfileChanged += ProfileService_ProfileChanged;
            profileService.Profiles.CollectionChanged += ProfileService_ProfileChanged;

            InitializeCriteria();

            PlanPreviewCommand = new RelayCommand(RunPlanPreview);
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
            instructionList.Clear();
            SelectedProfileId = profileService.ActiveProfile.Id.ToString();
            ProfileChoices = GetProfileChoices();
        }

        private void InitializeCriteria() {
            PlanDate = DateTime.Now.Date;
            SelectedProfileId = profileService.ActiveProfile.Id.ToString();
            ProfileChoices = GetProfileChoices();
        }

        private DateTime planDate = DateTime.MinValue;
        public DateTime PlanDate {
            get => planDate;
            set {
                planDate = value;
                RaisePropertyChanged(nameof(PlanDate));
            }
        }

        private int planHours = 13;
        public int PlanHours {
            get => planHours;
            set {
                planHours = value;
                RaisePropertyChanged(nameof(PlanHours));
            }
        }

        private int planMinutes = 0;
        public int PlanMinutes {
            get => planMinutes;
            set {
                planMinutes = value;
                RaisePropertyChanged(nameof(PlanMinutes));
            }
        }

        private int planSeconds = 0;
        public int PlanSeconds {
            get => planSeconds;
            set {
                planSeconds = value;
                RaisePropertyChanged(nameof(PlanSeconds));
            }
        }

        private AsyncObservableCollection<KeyValuePair<string, string>> profileChoices;
        public AsyncObservableCollection<KeyValuePair<string, string>> ProfileChoices {
            get {
                return profileChoices;
            }
            set {
                profileChoices = value;
                RaisePropertyChanged(nameof(ProfileChoices));
            }
        }

        private string selectedProfileId;
        public string SelectedProfileId {
            get => selectedProfileId;
            set {
                selectedProfileId = value;
                RaisePropertyChanged(nameof(SelectedProfileId));
            }
        }

        private AsyncObservableCollection<TreeViewItem> instructionList = new AsyncObservableCollection<TreeViewItem>();
        public AsyncObservableCollection<TreeViewItem> InstructionList {
            get => instructionList;
            set {
                instructionList = value;
                RaisePropertyChanged(nameof(InstructionList));
            }
        }

        public ICommand PlanPreviewCommand { get; private set; }

        private void RunPlanPreview(object obj) {
            AsyncObservableCollection<TreeViewItem> list = new AsyncObservableCollection<TreeViewItem>();
            InstructionList = list;

            if (PlanDate == DateTime.MinValue || SelectedProfileId == null) {
                return;
            }

            try {
                DateTime atDateTime = PlanDate.Date.AddHours(PlanHours).AddMinutes(PlanMinutes).AddSeconds(PlanSeconds);
                TSLogger.Debug($"running plan preview for {Utils.FormatDateTimeFull(atDateTime)}, profileId={SelectedProfileId}");

                List<IPlanProject> projects = new SchedulerPlanLoader().LoadActiveProjects(database.GetContext(), GetProfile(SelectedProfileId));

                if (projects == null) {
                    TSLogger.Debug($"no active projects for {atDateTime}, profileId={SelectedProfileId}");
                    InstructionList = list;

                    string profileName = ProfileChoices.First(p => p.Key == selectedProfileId).Value;
                    MyMessageBox.Show($"No active projects/targets were returned by the planner for {Utils.FormatDateTimeFull(atDateTime)} and{Environment.NewLine}profile '{profileName}' - or no active targets were found with active exposure plans.", "Oops");
                    return;
                }

                List<SchedulerPlan> assistantPlans = Planner.GetPerfectPlan(atDateTime, profileService, projects);

                foreach (SchedulerPlan plan in assistantPlans) {
                    TreeViewItem planItem = new TreeViewItem();

                    if (plan.WaitForNextTargetTime != null) {
                        planItem.Header = $"Wait until {Utils.FormatDateTimeFull(plan.WaitForNextTargetTime)}";

                        list.Add(planItem);
                        continue;
                    }

                    planItem.Header = GetTargetLabel(plan);
                    planItem.IsExpanded = false;
                    list.Add(planItem);
                    int ditherTrigger = 0;

                    foreach (IPlanInstruction instruction in plan.PlanInstructions) {
                        TreeViewItem instructionItem = new TreeViewItem();

                        if (instruction is PlanMessage) {
                            continue;
                        }

                        if (instruction is PlanSlew) {
                            instructionItem.Header = GetSlewLabel(plan.PlanTarget, (PlanSlew)instruction);
                            planItem.Items.Add(instructionItem);
                            continue;
                        }

                        if (instruction is PlanSwitchFilter) {
                            instructionItem.Header = $"Switch Filter: {((PlanSwitchFilter)instruction).planExposure.FilterName}";
                            planItem.Items.Add(instructionItem);
                            continue;
                        }

                        if (instruction is PlanSetReadoutMode) {
                            int? readoutMode = ((PlanSetReadoutMode)instruction).planExposure.ReadoutMode;
                            if (readoutMode != null && readoutMode > 0) {
                                instructionItem.Header = $"Set readout mode: {readoutMode}";
                                planItem.Items.Add(instructionItem);
                            }
                            continue;
                        }

                        if (instruction is PlanTakeExposure) {
                            instructionItem.Header = GetTakeExposureLabel((PlanTakeExposure)instruction);
                            planItem.Items.Add(instructionItem);

                            if (plan.PlanTarget.Project.DitherEvery > 0) {
                                if (++ditherTrigger == plan.PlanTarget.Project.DitherEvery) {
                                    planItem.Items.Add(new TreeViewItem { Header = "Dither" });
                                    ditherTrigger = 0;
                                }
                            }

                            continue;
                        }

                        TSLogger.Error($"unknown instruction type in plan preview: {instruction.GetType().FullName}");
                        throw new Exception($"unknown instruction type in plan preview: {instruction.GetType().FullName}");
                    }
                }

                InstructionList = list;
            }
            catch (Exception ex) {
                TSLogger.Error($"failed to run plan preview: {ex.Message} {ex.StackTrace}");
                InstructionList = null;
            }
        }

        private AsyncObservableCollection<KeyValuePair<string, string>> GetProfileChoices() {
            Dictionary<string, string> profiles = new Dictionary<string, string>();
            profileService.Profiles.ForEach(p => {
                profiles.Add(p.Id.ToString(), p.Name);
            });

            AsyncObservableCollection<KeyValuePair<string, string>> profileChoices = new AsyncObservableCollection<KeyValuePair<string, string>>();
            foreach (KeyValuePair<string, string> entry in profiles) {
                profileChoices.Add(new KeyValuePair<string, string>(entry.Key, entry.Value));
            }

            return profileChoices;
        }

        private IProfile GetProfile(string profileId) {
            foreach (ProfileMeta profileMeta in profileService.Profiles) {
                if (profileMeta.Id.ToString() == profileId) {
                    return ProfileLoader.Load(profileService, profileMeta);
                }
            }

            TSLogger.Error($"failed to get profile for ID={profileId}");
            throw new Exception($"failed to get profile for ID={profileId}");
        }

        private string GetTargetLabel(SchedulerPlan plan) {
            string label = $"{plan.PlanTarget.Project.Name} / {plan.PlanTarget.Name}";
            return $"{label} - start: {Utils.FormatDateTimeFull(plan.TimeInterval.StartTime)} stop: {Utils.FormatDateTimeFull(plan.TimeInterval.EndTime)}";
        }

        private string GetSlewLabel(IPlanTarget planTarget, PlanSlew planSlew) {

            string name = "Slew";
            string rotate = planTarget.Rotation != 0 ? $", Rotate: {planTarget.Rotation}°" : "";

            if (planSlew.center && planTarget.Rotation == 0) {
                name = "Slew/Center";
            }
            else if (planSlew.center && planTarget.Rotation != 0) {
                name = "Slew/Rotate/Center";
            }
            else if (planSlew.center) {
                name = "Slew/Center";
            }

            return $"{name}: {planTarget.Coordinates.RAString} {planTarget.Coordinates.DecString}{rotate}";
        }

        private string GetTakeExposureLabel(PlanTakeExposure instruction) {
            IPlanExposure planExposure = instruction.planExposure;
            StringBuilder sb = new StringBuilder();
            sb.Append("Take Exposure:");
            sb.Append($" {planExposure.ExposureLength} secs, ");
            sb.Append($" Gain={CameraDefault(planExposure.Gain)}, ");
            sb.Append($" Offset={CameraDefault(planExposure.Offset)}, ");
            sb.Append($" Binning={planExposure.BinningMode}");

            return sb.ToString();
        }

        private string CameraDefault(int? value) {
            return value != null ? value.ToString() : "(camera)";
        }
    }
}
