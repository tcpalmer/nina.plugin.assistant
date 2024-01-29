using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ProfileViewVM : BaseVM {
        private AssistantManagerVM managerVM;
        private ProfileMeta profile;
        private TreeDataItem parentItem;
        private List<Project> projects;

        public ProfileMeta Profile {
            get => profile;
            set {
                profile = value;
                RaisePropertyChanged(nameof(Profile));
            }
        }

        public List<Project> Projects {
            get => projects;
            set {
                projects = value;
                RaisePropertyChanged(nameof(Projects));
            }
        }

        public bool PasteEnabled {
            get => Clipboard.HasType(TreeDataType.Project);
        }

        public ProfileViewVM(AssistantManagerVM managerVM, IProfileService profileService, TreeDataItem profileItem) : base(profileService) {
            this.managerVM = managerVM;
            Profile = (ProfileMeta)profileItem.Data;
            parentItem = profileItem;
            Projects = InitProjects(profileItem);

            ProfileSettingsCommand = new RelayCommand(ViewProfilePreferences);
            AddProjectCommand = new RelayCommand(AddProject);
            PasteProjectCommand = new RelayCommand(PasteProject);
            ImportCommand = new RelayCommand(DisplayProfileImport);
            ViewProjectCommand = new RelayCommand(ViewProject);
            CopyProjectCommand = new RelayCommand(CopyProject);
        }

        private List<Project> InitProjects(TreeDataItem profileItem) {
            List<Project> projects = new List<Project>();
            foreach (TreeDataItem item in profileItem.Items) {
                projects.Add((Project)item.Data);
            }

            return projects;
        }

        private bool showProfileImportView = false;

        public bool ShowProfileImportView {
            get => showProfileImportView;
            set {
                showProfileImportView = value;
                RaisePropertyChanged(nameof(ShowProfileImportView));
            }
        }

        private ProfileImportViewVM profileImportVM;

        public ProfileImportViewVM ProfileImportVM {
            get => profileImportVM;
            set {
                profileImportVM = value;
                RaisePropertyChanged(nameof(ProfileImportVM));
            }
        }

        public ICommand ProfileSettingsCommand { get; private set; }
        public ICommand AddProjectCommand { get; private set; }
        public ICommand PasteProjectCommand { get; private set; }
        public ICommand ImportCommand { get; private set; }
        public ICommand ViewProjectCommand { get; private set; }
        public ICommand CopyProjectCommand { get; private set; }

        private void ViewProfilePreferences(object obj) {
            managerVM.ViewProfilePreferences(Profile);
        }

        private void AddProject(object obj) {
            managerVM.AddNewProject(parentItem);
        }

        private void PasteProject(object obj) {
            managerVM.PasteProject(parentItem);
        }

        private void DisplayProfileImport(object obj) {
            ShowProfileImportView = !ShowProfileImportView;
            if (ShowProfileImportView) {
                ProfileImportVM = new ProfileImportViewVM(managerVM, parentItem, profileService);
            }
        }

        private void CopyProject(object obj) {
            Project project = obj as Project;
            if (project != null) {
                TreeDataItem item = Find(project);
                if (item != null) {
                    Clipboard.SetItem(item);
                    RaisePropertyChanged(nameof(PasteEnabled));
                }
            }
        }

        private void ViewProject(object obj) {
            Project project = obj as Project;
            if (project != null) {
                TreeDataItem item = Find(project);
                if (item != null) {
                    managerVM.NavigateTo(item);
                }
            }
        }

        private TreeDataItem Find(Project project) {
            foreach (TreeDataItem item in parentItem.Items) {
                if (((Project)item.Data).Id == project.Id) {
                    return item;
                }
            }

            return null;
        }
    }
}