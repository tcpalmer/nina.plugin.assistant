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

        private Visibility showFilterPlanView = Visibility.Hidden;
        public Visibility ShowFilterPlanView {
            get => showFilterPlanView;
            set {
                showFilterPlanView = value;
                RaisePropertyChanged(nameof(ShowFilterPlanView));
            }
        }

        private FilterPlanViewVM filterPlanViewVM;
        public FilterPlanViewVM FilterPlanViewVM {
            get => filterPlanViewVM;
            set {
                filterPlanViewVM = value;
                RaisePropertyChanged(nameof(FilterPlanViewVM));
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
                        ShowFilterPlanView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowProfileView = Visibility.Visible;
                        break;

                    case TreeDataType.Project:
                        activeTreeDataItem = item;
                        Project project = (Project)item.Data;
                        ProjectViewVM = new ProjectViewVM(this, profileService, project);
                        ShowProfileView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPlanView = Visibility.Collapsed;
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
                        ShowFilterPlanView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Visible;

                        Logger.Debug($"TARGET: {target.Name}\n{target}");

                        break;

                    case TreeDataType.FilterPlan:
                        activeTreeDataItem = item;
                        FilterPlan filterPlan = (FilterPlan)item.Data;
                        FilterPlanViewVM = new FilterPlanViewVM(this, profileService, filterPlan);
                        ShowProfileView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPlanView = Visibility.Visible;
                        break;

                    default:
                        activeTreeDataItem = null;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowFilterPlanView = Visibility.Collapsed;
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

                            foreach (FilterPlan filterPlan in target.FilterPlans) {
                                TreeDataItem filterPlanItem = new TreeDataItem(TreeDataType.FilterPlan, filterPlan.FilterName, filterPlan, targetItem);
                                targetItem.Items.Add(filterPlanItem);
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
                    // TODO: need to add Target to children of parent project??
                    // IFF we continue to manager FilterPlans in the tree under targets, then we'd need to
                    // add this target's FPs as children of targetItem
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

        // TODO: this should go away when part of integrated Target view/edit
        public void SaveFilterPlan(FilterPlan filterPlan) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                if (context.SaveFilterPlan(filterPlan)) {
                    activeTreeDataItem.Data = filterPlan;
                    activeTreeDataItem.Header = filterPlan.FilterName;
                }
                else {
                    Notification.ShowError("Failed to save Assistant Filter Plan (see log for details)");
                }
            }
        }

        // TODO: this should go away - legacy from context menu approach
        /*
        public static void PasteItem(TreeDataItem parentTreeDataItem, object parent, object existing) {
            using (var context = new AssistantDatabaseInteraction().GetContext()) {
                bool status = true;

                switch (parent) {
                    case Project project:
                        Logger.Info($"PASTE: parent is Project {project.Name}, item is {existing.GetType()}");
                        status = context.PasteTarget((Project)parent, (Target)existing);
                        break;

                    case Target target:
                        Logger.Info($"PASTE: parent is Target {target.Name}, item is {existing.GetType()}");
                        break;
                    default:
                        Logger.Info($"PASTE parent is? {parent.GetType()}, item is {existing.GetType()}");
                        break;
                }

                if (!status) {
                    Notification.ShowError("Failed to paste (see log for details)");
                }

                // TODO: we have to reload parentTreeDataItem children and get it to redisplay
                // We could have the paste op return the parent with all children (old and new)
            }
        }*/
    }

    public enum TreeDataType {
        Folder, Profile, Project, Target, FilterPlan
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

            if (Type != TreeDataType.Folder) {
                MouseRightButtonDown += TreeDataItem_MouseRightButtonDown;
            }
        }

        private static RelayCommand menuItemCommand = new RelayCommand(MenuItemCommandExecute, MenuItemCommandCanExecute);

        private static void MenuItemCommandExecute(object obj) {
            MenuItemContext context = obj as MenuItemContext;
            switch (context.Type) {
                case MenuItemType.New: break;
                case MenuItemType.Paste:
                    //AssistantManagerVM.PasteItem(context.Item, context.Item.Data, Clipboard.GetItem().Data);
                    break;
            }
        }

        private static bool MenuItemCommandCanExecute(object obj) {
            MenuItemContext context = obj as MenuItemContext;
            if (context == null) {
                return false;
            }

            if (context.Type != MenuItemType.Paste) {
                return true;
            }

            switch (context.Item.Type) {
                case TreeDataType.Profile: return Clipboard.HasType(TreeDataType.Project);
                case TreeDataType.Project: return Clipboard.HasType(TreeDataType.Target);
                case TreeDataType.Target: return Clipboard.HasType(TreeDataType.FilterPlan);
                default: return false;
            }
        }

        private void TreeDataItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            ContextMenu = GetContextMenu();
            e.Handled = true;
        }

        private ContextMenu GetContextMenu() {
            ContextMenu contextMenu = new ContextMenu();

            switch (Type) {
                case TreeDataType.Profile:
                    contextMenu.Items.Add(GetMenuItem("New Project", MenuItemType.New));
                    contextMenu.Items.Add(GetMenuItem("Paste Project", MenuItemType.Paste));
                    break;
                case TreeDataType.Project:
                    contextMenu.Items.Add(GetMenuItem("New Target", MenuItemType.New));
                    contextMenu.Items.Add(GetMenuItem("Paste Target", MenuItemType.Paste));
                    break;
                case TreeDataType.Target:
                    contextMenu.Items.Add(GetMenuItem("New Filter Plan", MenuItemType.New));
                    contextMenu.Items.Add(GetMenuItem("Paste Filter Plan", MenuItemType.Paste));
                    break;
                case TreeDataType.FilterPlan:
                    break;
                default:
                    break;
            }

            return contextMenu;
        }

        private MenuItem GetMenuItem(string header, MenuItemType type) {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = header;
            menuItem.Command = menuItemCommand;
            menuItem.CommandParameter = new MenuItemContext(type, this);
            return menuItem;
        }
    }

    public enum MenuItemType {
        New, Paste
    }

    public class MenuItemContext {
        public MenuItemType Type { get; set; }
        public TreeDataItem Item { get; set; }

        public MenuItemContext(MenuItemType type, TreeDataItem item) {
            Type = type;
            Item = item;
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
