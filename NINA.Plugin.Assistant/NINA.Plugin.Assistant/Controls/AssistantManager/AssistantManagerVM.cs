using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class AssistantManagerVM : BaseVM {

        private AssistantDatabaseInteraction database;
        private TreeDataItem activeTreeDataItem;
        private bool isEditMode = false;

        public AssistantManagerVM(IProfileService profileService) : base(profileService) {
            database = new AssistantDatabaseInteraction();

            SelectedItemChangedCommand = new RelayCommand(SelectedItemChanged);
        }

        private Visibility showProfileView = Visibility.Hidden;
        public Visibility ShowProfileView {
            get => showProfileView;
            set {
                showProfileView = value;
                RaisePropertyChanged(nameof(ShowProfileView));
            }
        }

        private ProfileViewVM profileViewVM;
        public ProfileViewVM ProfileViewVM {
            get => profileViewVM;
            set {
                profileViewVM = value;
                RaisePropertyChanged(nameof(ProfileViewVM));
            }
        }

        private Visibility showProjectView = Visibility.Hidden;
        public Visibility ShowProjectView {
            get => showProjectView;
            set {
                showProjectView = value;
                RaisePropertyChanged(nameof(ShowProjectView));
            }
        }

        private ProjectViewVM projectViewVM;
        public ProjectViewVM ProjectViewVM {
            get => projectViewVM;
            set {
                projectViewVM = value;
                RaisePropertyChanged(nameof(ProjectViewVM));
            }
        }

        private Visibility showTargetView = Visibility.Hidden;
        public Visibility ShowTargetView {
            get => showTargetView;
            set {
                showTargetView = value;
                RaisePropertyChanged(nameof(ShowTargetView));
            }
        }

        private TargetViewVM targetViewVM;
        public TargetViewVM TargetViewVM {
            get => targetViewVM;
            set {
                targetViewVM = value;
                RaisePropertyChanged(nameof(TargetViewVM));
            }
        }

        private Visibility showExposurePlanView = Visibility.Hidden;
        public Visibility ShowExposurePlanView {
            get => showExposurePlanView;
            set {
                showExposurePlanView = value;
                RaisePropertyChanged(nameof(ShowExposurePlanView));
            }
        }

        private ExposurePlanViewVM exposurePlanViewVM;
        public ExposurePlanViewVM ExposurePlanViewVM {
            get => exposurePlanViewVM;
            set {
                exposurePlanViewVM = value;
                RaisePropertyChanged(nameof(ExposurePlanViewVM));
            }
        }

        public ICommand SelectedItemChangedCommand { get; private set; }
        private void SelectedItemChanged(object obj) {
            TreeDataItem item = obj as TreeDataItem;
            if (item != null) {
                switch (item.Type) {
                    case TreeDataType.Profile:
                        activeTreeDataItem = item;
                        ProfileViewVM = new ProfileViewVM(this, profileService, item);
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposurePlanView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowProfileView = Visibility.Visible;
                        break;

                    case TreeDataType.Project:
                        activeTreeDataItem = item;
                        Project project = (Project)item.Data;
                        ProjectViewVM = new ProjectViewVM(this, profileService, project);
                        ShowProfileView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposurePlanView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Visible;

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine($"PROJECT: {project.Name}\n{project}");
                        sb.AppendLine("-- Targets --");
                        project.Targets.ForEach(t => sb.AppendLine(t.ToString()));
                        Logger.Debug(sb.ToString());

                        break;

                    case TreeDataType.Target:
                        activeTreeDataItem = item;
                        Target target = (Target)item.Data;
                        TargetViewVM = new TargetViewVM(this, profileService, target);
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowExposurePlanView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Visible;

                        Logger.Debug($"TARGET: {target.Name}\n{target}");

                        break;

                    case TreeDataType.ExposurePlan:
                        activeTreeDataItem = item;
                        ExposurePlan exposurePlan = (ExposurePlan)item.Data;
                        ExposurePlanViewVM = new ExposurePlanViewVM(this, profileService, exposurePlan);
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposurePlanView = Visibility.Visible;
                        break;

                    default:
                        activeTreeDataItem = null;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposurePlanView = Visibility.Collapsed;
                        break;
                }
            }
        }

        List<TreeDataItem> rootProjectsList;
        public List<TreeDataItem> RootProjectsList {
            get {
                rootProjectsList = LoadProjectsTree();
                return rootProjectsList;
            }
            set {
                rootProjectsList = value;
                RaisePropertyChanged(nameof(RootProjectsList));
            }
        }

        bool treeViewEabled = true;
        public bool TreeViewEabled {
            get => treeViewEabled;
            set {
                treeViewEabled = value;
                RaisePropertyChanged(nameof(TreeViewEabled));
            }
        }

        private List<TreeDataItem> LoadProjectsTree() {

            List<TreeDataItem> rootList = new List<TreeDataItem>();
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.Folder, "Profiles", null);
            rootList.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.Profile, profile.Name, profile, profilesFolder);
                    profilesFolder.Items.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    foreach (Project project in projects) {
                        TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.Name, project, profileItem);
                        profileItem.Items.Add(projectItem);

                        foreach (Target target in project.Targets) {
                            TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target, projectItem);
                            projectItem.Items.Add(targetItem);

                            foreach (ExposurePlan exposurePlan in target.ExposurePlans) {
                                TreeDataItem exposurePlanItem = new TreeDataItem(TreeDataType.ExposurePlan, exposurePlan.FilterName, exposurePlan, targetItem);
                                targetItem.Items.Add(exposurePlanItem);
                            }
                        }
                    }
                }
            }

            return rootList;
        }

        public void SetEditMode(bool editMode) {
            isEditMode = editMode;
            TreeViewEabled = !editMode;
        }

        public void NavigateTo(TreeDataItem item) {
            TreeDataItem parent = item.TreeParent;
            while (parent != null) {
                parent.IsExpanded = true;
                parent = parent.TreeParent;
            }

            item.IsSelected = true;
        }

        public void AddNewProject(TreeDataItem parentItem) {
            ProfileMeta profile = (ProfileMeta)parentItem.Data;
            Project project = new Project(profile.Id.ToString());
            project.Name = "<new project>";
            project.StartDate = DateTime.Now;
            project.EndDate = DateTime.Now.AddMonths(1);

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                Project newProject = context.SaveProject(project);
                if (newProject != null) {
                    TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.Name, project, parentItem);
                    parentItem.Items.Add(projectItem);
                    projectItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to save new Assistant Project (see log for details)");
                }
            }
        }

        public void SaveProject(Project project) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.SaveProject(project) != null) {
                    activeTreeDataItem.Data = project;
                    activeTreeDataItem.Header = project.Name;
                }
                else {
                    Notification.ShowError("Failed to save Assistant Project (see log for details)");
                }
            }
        }

        public void PasteProject(TreeDataItem parentItem) {
            ProfileMeta profile = (ProfileMeta)parentItem.Data;

            if (!Clipboard.HasType(TreeDataType.Project)) {
                Logger.Error($"expected clipboard to hold Project");
                return;
            }

            Project source = Clipboard.GetItem().Data as Project;
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                Project newProject = context.PasteProject(profile.Id.ToString(), source);
                if (newProject != null) {
                    TreeDataItem newProjectItem = new TreeDataItem(TreeDataType.Project, newProject.Name, newProject, parentItem);
                    parentItem.Items.Add(newProjectItem);
                    newProjectItem.IsSelected = true;
                    parentItem.IsExpanded = true;

                    newProject.Targets.ForEach(target => {
                        TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target, newProjectItem);
                        newProjectItem.Items.Add(targetItem);
                    });
                }
                else {
                    Notification.ShowError("Failed to paste new Assistant Project (see log for details)");
                }
            }
        }

        public void DeleteProject(Project project) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.DeleteProject(project)) {
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Items.Remove(activeTreeDataItem);
                    parentItem.IsSelected = true;
                }
                else {
                    Notification.ShowError("Failed to delete Assistant Project (see log for details)");
                }
            }
        }

        public void AddNewTarget(Project project) {
            Target target = new Target();
            target.Name = "<new target>";
            TreeDataItem parentItem = activeTreeDataItem;

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                Target newTarget = context.AddNewTarget(project, target);
                if (newTarget != null) {
                    TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target, parentItem);
                    parentItem.Items.Add(targetItem);
                    targetItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to add new Assistant Target (see log for details)");
                }
            }
        }

        public void SaveTarget(Target target) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.SaveTarget(target) != null) {
                    activeTreeDataItem.Data = target;
                    activeTreeDataItem.Header = target.Name;

                    // Refresh the parent project
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Data = context.GetProject(target.ProjectId);
                }
                else {
                    Notification.ShowError("Failed to save Assistant Target (see log for details)");
                }
            }
        }

        public void PasteTarget(Project project) {
            if (!Clipboard.HasType(TreeDataType.Target)) {
                Logger.Error($"expected clipboard to hold Target");
                return;
            }

            Target source = Clipboard.GetItem().Data as Target;
            TreeDataItem parentItem = activeTreeDataItem;

            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                Target newTarget = context.PasteTarget(project, source);
                if (newTarget != null) {
                    TreeDataItem newTargetItem = new TreeDataItem(TreeDataType.Target, newTarget.Name, newTarget, parentItem);
                    parentItem.Items.Add(newTargetItem);
                    newTargetItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to paste new Assistant Project (see log for details)");
                }
            }
        }

        public void DeleteTarget(Target target) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.DeleteTarget(target)) {
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Items.Remove(activeTreeDataItem);
                    parentItem.IsSelected = true;
                }
                else {
                    Notification.ShowError("Failed to delete Assistant Target (see log for details)");
                }
            }
        }

        public void CopyItem() {
            Clipboard.SetItem(activeTreeDataItem);
        }

    }

    public enum TreeDataType {
        Folder, Profile, Project, Target, ExposurePlan
    }

    public class TreeDataItem : TreeViewItem {

        public TreeDataType Type { get; }
        public TreeDataItem TreeParent { get; }
        public object Data { get; set; }

        public TreeDataItem(TreeDataType type, string name, TreeDataItem parent) : this(type, name, null, parent) { }

        public TreeDataItem(TreeDataType type, string name, object data, TreeDataItem parent) {
            Type = type;
            TreeParent = parent;
            Data = data;
            Header = name;
        }
    }

    public class Clipboard {

        private static readonly Clipboard Instance = new Clipboard();
        private TreeDataItem item { get; set; }

        public static bool HasType(TreeDataType type) {
            return Instance.item?.Type == type;
        }

        public static void SetItem(TreeDataItem item) {
            Instance.item = item;
        }

        public static TreeDataItem GetItem() {
            TreeDataItem item = Instance.item;
            SetItem(null);
            return item;
        }

        private Clipboard() { }
    }
}

