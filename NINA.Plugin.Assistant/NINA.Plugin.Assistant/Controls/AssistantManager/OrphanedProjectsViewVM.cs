using Assistant.NINAPlugin.Database.Schema;
using LinqKit;
using NINA.Core.MyMessageBox;
using NINA.Core.Utility;
using NINA.Plugin.Assistant.Shared.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class OrphanedProjectsViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private TreeDataItem parentItem;


        public OrphanedProjectsViewVM(AssistantManagerVM managerVM, IProfileService profileService, TreeDataItem profileItem, List<Project> orphanedProjects) : base(profileService) {
            this.managerVM = managerVM;
            parentItem = profileItem;

            projects = new ObservableCollection<Project>();
            orphanedProjects.ForEach(p => projects.Add(p));

            MoveProjectCommand = new RelayCommand(MoveProject);
            DeleteProjectCommand = new RelayCommand(DeleteProject);

            InitializeProfilesList();
        }

        private ObservableCollection<Project> projects;
        public ObservableCollection<Project> Projects {
            get => projects;
            set {
                projects = value;
                RaisePropertyChanged(nameof(Projects));
            }
        }

        private AsyncObservableCollection<KeyValuePair<string, string>> profileChoices;
        public AsyncObservableCollection<KeyValuePair<string, string>> ProfileChoices {
            get {
                return profileChoices;
            }
            set {
                profileChoices = value;
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

        private void InitializeProfilesList() {
            List<ProfileMeta> profiles = new List<ProfileMeta>();
            profileService.Profiles.ForEach(p => profiles.Add(p));

            ProfileChoices = new AsyncObservableCollection<KeyValuePair<string, string>>();
            profiles.ForEach(p => ProfileChoices.Add(new KeyValuePair<string, string>(p.Id.ToString(), p.Name)));
            RaisePropertyChanged(nameof(ProfileChoices));
        }

        public ICommand MoveProjectCommand { get; private set; }
        public ICommand DeleteProjectCommand { get; private set; }

        private void MoveProject(object obj) {

            if (string.IsNullOrEmpty(SelectedProfileId)) {
                MyMessageBox.Show("You must select a destination profile to move a project.", "Oops");
                return;
            }

            Project project = obj as Project;
            if (project != null) {
                string selectedProfileName = ProfileChoices.Where(p => p.Key == SelectedProfileId).FirstOrDefault().Value;
                if (MyMessageBox.Show($"Orphaned project '{project.Name}' will be moved to profile '{selectedProfileName}'.  Are you sure?", "Move Project?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    TSLogger.Info($"moving orphaned project '{project.Name}' to '{selectedProfileName}'");
                    if (managerVM.MoveOrphanedProject(project, SelectedProfileId)) {
                        Projects.Remove(project);
                        RaisePropertyChanged(nameof(Projects));
                    }
                }
            }
        }

        private void DeleteProject(object obj) {
            Project project = obj as Project;
            if (project != null) {
                if (MyMessageBox.Show($"Orphaned project '{project.Name}' will be permanently deleted.  Are you sure?", "Delete Project?", MessageBoxButton.YesNo, MessageBoxResult.No) == MessageBoxResult.Yes) {
                    TSLogger.Info($"deleting orphaned project '{project.Name}'");
                    if (managerVM.DeleteOrphanedProject(project)) {
                        Projects.Remove(project);
                        RaisePropertyChanged(nameof(Projects));
                    }
                }
            }
        }
    }
}
