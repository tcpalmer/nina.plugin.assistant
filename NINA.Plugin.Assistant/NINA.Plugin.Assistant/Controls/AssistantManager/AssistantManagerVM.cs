using Assistant.NINAPlugin.Controls.Util;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class AssistantManagerVM : BaseVM {

        private IApplicationMediator applicationMediator;
        private IFramingAssistantVM framingAssistantVM;
        private IDeepSkyObjectSearchVM deepSkyObjectSearchVM;
        private IPlanetariumFactory planetariumFactory;
        private AssistantDatabaseInteraction database;

        private TreeDataItem selectedTreeDataItem;
        private TreeDataItem activeTreeDataItem;

        public AssistantManagerVM(IProfileService profileService,
            IApplicationMediator applicationMediator,
            IFramingAssistantVM framingAssistantVM,
            IDeepSkyObjectSearchVM deepSkyObjectSearchVM,
            IPlanetariumFactory planetariumFactory)
            : base(profileService) {

            this.applicationMediator = applicationMediator;
            this.framingAssistantVM = framingAssistantVM;
            this.deepSkyObjectSearchVM = deepSkyObjectSearchVM;
            this.planetariumFactory = planetariumFactory;

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

        public AssistantTreeViewVM ProjectsTreeViewVM {
            get => new AssistantTreeViewVM(this, profileService, "Projects", RootProjectsList, 350);
        }

        public AssistantTreeViewVM FilterPrefsTreeViewVM {
            get => new AssistantTreeViewVM(this, profileService, "Filter Preferences", RootFilterPrefsList, 180);
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

        private Visibility showFilterPrefView = Visibility.Hidden;
        public Visibility ShowFilterPrefView {
            get => showFilterPrefView;
            set {
                showFilterPrefView = value;
                RaisePropertyChanged(nameof(ShowFilterPrefView));
            }
        }

        private FilterPrefViewVM filterPrefViewVM;
        public FilterPrefViewVM FilterPrefViewVM {
            get => filterPrefViewVM;
            set {
                filterPrefViewVM = value;
                RaisePropertyChanged(nameof(FilterPrefViewVM));
            }
        }

        public ICommand SelectedItemChangedCommand { get; private set; }

        private void SelectedItemChanged(object obj) {
            TreeDataItem item = obj as TreeDataItem;
            if (item != null) {

                DeselectOppositeTree(selectedTreeDataItem, item);
                selectedTreeDataItem = item;

                switch (item.Type) {
                    case TreeDataType.ProjectProfile:
                        activeTreeDataItem = item;
                        ProfileViewVM = new ProfileViewVM(this, profileService, item);
                        ShowTargetView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowFilterPrefView = Visibility.Collapsed;
                        ShowProfileView = Visibility.Visible;
                        break;

                    case TreeDataType.Project:
                        activeTreeDataItem = item;
                        Project project = (Project)item.Data;
                        ProjectViewVM = new ProjectViewVM(this, profileService, project);
                        ShowProfileView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPrefView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Visible;
                        break;

                    case TreeDataType.Target:
                        activeTreeDataItem = item;
                        Target target = (Target)item.Data;
                        TargetViewVM = new TargetViewVM(this, profileService, applicationMediator, framingAssistantVM, deepSkyObjectSearchVM, planetariumFactory, target);
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowFilterPrefView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Visible;
                        break;

                    case TreeDataType.FilterPref:
                        activeTreeDataItem = item;
                        FilterPreference filterPreference = (FilterPreference)item.Data;
                        FilterPrefViewVM = new FilterPrefViewVM(this, profileService, filterPreference);
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPrefView = Visibility.Visible;
                        break;

                    default:
                        activeTreeDataItem = null;
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPrefView = Visibility.Collapsed;
                        break;
                }
            }
        }

        private void DeselectOppositeTree(TreeDataItem existingItem, TreeDataItem newItem) {
            if (existingItem == null) {
                return;
            }

            TreeDataItem existingItemRoot = existingItem.GetRoot();
            TreeDataItem newItemRoot = newItem.GetRoot();
            if (existingItemRoot.Type == newItemRoot.Type) {
                return;
            }

            TreeDataItem.VisitAll(existingItemRoot, i => { i.IsSelected = false; });
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

        List<TreeDataItem> rootFilterPrefsList;
        public List<TreeDataItem> RootFilterPrefsList {
            get {
                rootFilterPrefsList = LoadFilterPrefsTree();
                return rootFilterPrefsList;
            }
            set {
                rootFilterPrefsList = value;
                RaisePropertyChanged(nameof(RootFilterPrefsList));
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
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.ProjectRoot, "Profiles", null);
            rootList.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.ProjectProfile, profile.Name, profile, profilesFolder);
                    profilesFolder.Items.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    foreach (Project project in projects) {
                        TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.Name, project, profileItem);
                        profileItem.Items.Add(projectItem);

                        List<Target> targetList = project.Targets;
                        foreach (Target target in targetList) {
                            TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target, projectItem);
                            projectItem.Items.Add(targetItem);
                        }
                    }
                }
            }

            return rootList;
        }

        private List<TreeDataItem> LoadFilterPrefsTree() {

            FilterPrefsReconciliation.ReconcileProfileFilterPrefs(profileService);

            List<TreeDataItem> rootList = new List<TreeDataItem>();
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.FilterPrefRoot, "Profiles", null);
            rootList.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.FilterPrefProfile, profile.Name, profile, profilesFolder);
                    profilesFolder.Items.Add(profileItem);

                    List<FilterPreference> filterPrefs = context.GetFilterPreferences(profile.Id.ToString());
                    foreach (FilterPreference filterPreference in filterPrefs) {
                        TreeDataItem filterPrefItem = new TreeDataItem(TreeDataType.FilterPref, filterPreference.FilterName, filterPreference, profileItem);
                        profileItem.Items.Add(filterPrefItem);
                    }
                }
            }

            return rootList;
        }

        public void SetEditMode(bool editMode) {
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

        public void SaveFilterPreference(FilterPreference filterPreference) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.SaveFilterPreference(filterPreference)) {
                    activeTreeDataItem.Data = filterPreference;
                    activeTreeDataItem.Header = filterPreference.FilterName;
                }
                else {
                    Notification.ShowError("Failed to save Assistant Filter Preference (see log for details)");
                }
            }
        }

        public Target DeleteExposurePlan(Target target, ExposurePlan exposurePlan) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                Target updatedTarget = context.DeleteExposurePlan(target, exposurePlan);
                if (updatedTarget != null) {
                    activeTreeDataItem.Data = updatedTarget;
                }
                else {
                    Notification.ShowError("Failed to delete Assistant Exposure Plan (see log for details)");
                }

                return updatedTarget;
            }
        }

        public void CopyItem() {
            Clipboard.SetItem(activeTreeDataItem);
        }

        public IProfile GetProfile(string profileId) {
            ProfileMeta profileMeta = profileService.Profiles.Where(p => p.Id.ToString() == profileId).FirstOrDefault();
            if (profileMeta != null) {
                return ProfileLoader.Load(profileService, profileMeta);
            }

            Logger.Error($"Assistant: failed to load profile, id = {profileId}");
            return null;
        }

    }

    public enum TreeDataType {
        ProjectRoot, FilterPrefRoot, ProjectProfile, FilterPrefProfile, Project, Target, FilterPref
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

        public TreeDataItem GetRoot() {
            TreeDataItem item = this;
            while (item.TreeParent != null) {
                item = item.TreeParent;
            }

            return item;
        }

        public static void VisitAll(TreeDataItem item, Action<TreeDataItem> action) {
            action(item);
            foreach (TreeDataItem child in item.Items) {
                VisitAll(child, action);
            }
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

