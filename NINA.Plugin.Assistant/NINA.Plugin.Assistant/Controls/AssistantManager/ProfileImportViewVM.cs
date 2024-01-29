using Assistant.NINAPlugin.Controls.Util;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Microsoft.WindowsAPICodePack.Dialogs;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProfileImportViewVM : BaseVM {
        private const string DEFAULT_TYPE_FILTER = "<any>";

        private AssistantManagerVM managerVM;
        private TreeDataItem profileItem;
        private string ParentProfileId;

        private Dictionary<Target, string> targetsDict;

        public ProfileImportViewVM(AssistantManagerVM managerVM, TreeDataItem profileItem, IProfileService profileService) : base(profileService) {
            this.managerVM = managerVM;
            this.profileItem = profileItem;
            ParentProfileId = (profileItem.Data as ProfileMeta).Id.ToString();

            ImportCommand = new AsyncCommand<bool>(() => Import());
            SelectFileCommand = new AsyncCommand<bool>(() => SelectFile());

            InitializeCombos();
            ImportFilePath = null;
        }

        private void InitializeCombos() {
            TypeFilterChoices = new List<string>() { DEFAULT_TYPE_FILTER };

            ProjectChoices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(-1,"Create New")
            };

            Dictionary<Project, string> projectsDict = GetProjectsDictionary();
            foreach (KeyValuePair<Project, string> entry in projectsDict) {
                ProjectChoices.Add(new KeyValuePair<int, string>(entry.Key.Id, entry.Value));
            }

            TargetChoices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(-1,"None"),
            };

            targetsDict = GetTargetsDictionary(projectsDict);
            foreach (KeyValuePair<Target, string> entry in targetsDict) {
                TargetChoices.Add(new KeyValuePair<int, string>(entry.Key.Id, entry.Value));
            }
        }

        private Dictionary<Project, string> GetProjectsDictionary() {
            List<Project> projects;
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                projects = context.ProjectSet.AsNoTracking().Where(p => p.ProfileId == ParentProfileId).OrderBy(p => p.name).ToList();
            }

            Dictionary<Project, string> dict = new Dictionary<Project, string>();
            projects.ForEach(p => { dict.Add(p, p.Name); });
            return dict;
        }

        private Dictionary<Target, string> GetTargetsDictionary(Dictionary<Project, string> projectsDict) {
            Dictionary<Target, string> dict = new Dictionary<Target, string>();
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                foreach (KeyValuePair<Project, string> entry in projectsDict) {
                    Project p = context.GetProject(entry.Key.Id);
                    foreach (Target target in p.Targets) {
                        if (target.ExposurePlans.Count > 0) {
                            dict.Add(target, target.Name);
                        }
                    }
                }
            }

            // Sort by target name
            IOrderedEnumerable<KeyValuePair<Target, string>> sortedDict = from entry in dict orderby entry.Value ascending select entry;
            return sortedDict.ToDictionary<KeyValuePair<Target, string>, Target, string>(pair => pair.Key, pair => pair.Value);
        }

        private string importFilePath;

        public string ImportFilePath {
            get => importFilePath;
            set {
                importFilePath = value;
                RaisePropertyChanged(nameof(ImportFilePath));
                TypeFilterChoices = GetTypeFilterChoices(importFilePath);
                RaisePropertyChanged(nameof(ImportEnabled));
            }
        }

        private List<string> typeFilterChoices;

        public List<string> TypeFilterChoices {
            get {
                return typeFilterChoices;
            }
            set {
                typeFilterChoices = value;
                RaisePropertyChanged(nameof(TypeFilterChoices));
            }
        }

        private string selectedTypeFilter = DEFAULT_TYPE_FILTER;

        public string SelectedTypeFilter {
            get => selectedTypeFilter;
            set {
                selectedTypeFilter = value;
                RaisePropertyChanged(nameof(SelectedTypeFilter));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> projectChoices;

        public AsyncObservableCollection<KeyValuePair<int, string>> ProjectChoices {
            get => projectChoices;
            set {
                projectChoices = value;
                RaisePropertyChanged(nameof(ProjectChoices));
            }
        }

        private int selectedProjectId = -1;

        public int SelectedProjectId {
            get => selectedProjectId;
            set {
                selectedProjectId = value;
                RaisePropertyChanged(nameof(SelectedProjectId));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> targetChoices;

        public AsyncObservableCollection<KeyValuePair<int, string>> TargetChoices {
            get => targetChoices;
            set {
                targetChoices = value;
                RaisePropertyChanged(nameof(TargetChoices));
            }
        }

        private int selectedTargetId = -1;

        public int SelectedTargetId {
            get => selectedTargetId;
            set {
                selectedTargetId = value;
                RaisePropertyChanged(nameof(SelectedTargetId));
            }
        }

        public bool ImportEnabled { get => ImportFileValid(); }

        private bool ImportFileValid() {
            if (ImportFilePath == null) {
                return false;
            }

            try { return new FileInfo(ImportFilePath).Exists == true; } catch (Exception) { return false; }
        }

        public ICommand ImportCommand { get; private set; }
        public ICommand SelectFileCommand { get; private set; }

        private Task<bool> SelectFile() {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.Title = "Select CSV File";
            dialog.Multiselect = false;
            dialog.Filters.Add(new CommonFileDialogFilter("CSV files", "*.csv"));

            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok) {
                ImportFilePath = dialog.FileName;
            }

            return Task.FromResult(true);
        }

        private Task<bool> Import() {
            TSLogger.Info($"importing targets from {importFilePath}");
            CsvTargetLoader loader = new CsvTargetLoader();

            try {
                string typeFilter = SelectedTypeFilter == DEFAULT_TYPE_FILTER ? null : SelectedTypeFilter;
                List<Target> targets = loader.Load(ImportFilePath, typeFilter);
                TSLogger.Info($"read {targets.Count} targets for import, filtered by '{typeFilter}'");

                if (targets.Count == 0) {
                    MyMessageBox.Show("No targets found for import.", "Oops");
                    return Task.FromResult(true);
                }

                if (MyMessageBox.Show($"Continue with import of {targets.Count} targets?  This cannot be undone.", "Import?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.No) {
                    TSLogger.Info("target import aborted");
                    return Task.FromResult(true);
                }

                // If have a target template, grab EPs from that and clone for each target
                if (SelectedTargetId != -1) {
                    Target templateTarget = targetsDict.Where(d => d.Key.Id == SelectedTargetId).FirstOrDefault().Key;
                    TSLogger.Info($"applying exposure plans from target '{templateTarget.Name}' to imported targets");
                    foreach (Target target in targets) {
                        target.ExposurePlans = CloneTemplateExposurePlans(templateTarget.ExposurePlans);
                    }
                }

                if (SelectedProjectId == -1) {
                    Project project = managerVM.AddNewProject(profileItem);
                    managerVM.AddTargets(project, targets);
                } else {
                    foreach (TreeDataItem item in profileItem.Items) {
                        if (item.Type == TreeDataType.Project) {
                            Project project = item.Data as Project;
                            if (project != null && project.Id == SelectedProjectId) {
                                managerVM.AddTargets(project, targets, item);
                                break;
                            }
                        }
                    }
                }
            } catch (Exception e) {
                TSLogger.Error($"Failed to read CSV file for target import: {e.Message}\n{e.StackTrace}");
                MyMessageBox.Show($"Import file cannot be read:\n{e.Message}", "Oops");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private List<string> GetTypeFilterChoices(string importFilePath) {
            try {
                if (importFilePath == null) {
                    return new List<string>() { DEFAULT_TYPE_FILTER };
                }

                CsvTargetLoader loader = new CsvTargetLoader();
                List<string> types = loader.GetUniqueTypes(importFilePath);
                return [DEFAULT_TYPE_FILTER, .. types.OrderBy(s => s).ToList()];
            } catch (Exception e) {
                TSLogger.Error($"Failed to read CSV file for target import: {e.Message}\n{e.StackTrace}");
                MyMessageBox.Show($"Import file cannot be read:\n{e.Message}", "Oops");
                return new List<string>() { DEFAULT_TYPE_FILTER };
            }
        }

        private List<ExposurePlan> CloneTemplateExposurePlans(List<ExposurePlan> exposurePlans) {
            List<ExposurePlan> list = new List<ExposurePlan>(exposurePlans.Count);
            if (exposurePlans == null || exposurePlans.Count == 0) {
                return list;
            }

            foreach (ExposurePlan ep in exposurePlans) {
                list.Add(ep.GetPasteCopy(ep.ProfileId));
            }

            return list;
        }
    }
}