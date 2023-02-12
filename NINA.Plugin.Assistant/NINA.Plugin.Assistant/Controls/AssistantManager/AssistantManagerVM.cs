using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
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

        public ICommand SelectedItemChangedCommand { get; private set; }
        private void SelectedItemChanged(object obj) {
            TreeDataItem item = obj as TreeDataItem;
            if (item != null) {
                switch (item.Type) {
                    case TreeDataType.Project:
                        activeTreeDataItem = item;
                        Project project = (Project)item.Data;
                        ProjectViewVM = new ProjectViewVM(this, project);
                        ShowProjectView = Visibility.Visible;
                        break;

                    default:
                        ShowProjectView = Visibility.Hidden;
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

        private List<TreeDataItem> LoadProjectsTree() {

            List<TreeDataItem> rootList = new List<TreeDataItem>();
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.Folder, "Profiles");
            rootList.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.Profile, profile.Name, profile);
                    profilesFolder.Items.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    foreach (Project project in projects) {
                        TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.Name, project);
                        profileItem.Items.Add(projectItem);

                        foreach (Target target in project.Targets) {
                            TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target);
                            projectItem.Items.Add(targetItem);

                            foreach (FilterPlan filterPlan in target.FilterPlans) {
                                TreeDataItem filterPlanItem = new TreeDataItem(TreeDataType.FilterPlan, filterPlan.FilterName, filterPlan);
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
        }

        public void SaveProject(Project project) {
            // TODO: move Save to dbContext
            if (project.Save()) {
                activeTreeDataItem.Data = project;
            }
            else {
                Notification.ShowError("Failed to save Assistant Project (see log for details)");
            }
        }
    }

    public enum TreeDataType {
        Folder, Profile, Project, Target, FilterPlan
    }

    public class TreeDataItem : TreeViewItem {

        public TreeDataType Type { get; }
        public object Data { get; set; }

        public TreeDataItem(TreeDataType type, string name) : this(type, name, null) { }

        public TreeDataItem(TreeDataType type, string name, object data) {
            Type = type;
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
                    Logger.Info($"PASTE: {Clipboard.GetItem().Header}");
                    break;
                case MenuItemType.Copy:
                    Logger.Info($"COPY: {context.Item.Header}");
                    Clipboard.SetItem(context.Item);
                    break;
                case MenuItemType.Delete: break;
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
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(GetMenuItem("Copy Project", MenuItemType.Copy));
                    contextMenu.Items.Add(GetMenuItem("Delete Project", MenuItemType.Delete));
                    break;
                case TreeDataType.Target:
                    contextMenu.Items.Add(GetMenuItem("New Filter Plan", MenuItemType.New));
                    contextMenu.Items.Add(GetMenuItem("Paste Filter Plan", MenuItemType.Paste));
                    contextMenu.Items.Add(new Separator());
                    contextMenu.Items.Add(GetMenuItem("Copy Target", MenuItemType.Copy));
                    contextMenu.Items.Add(GetMenuItem("Delete Target", MenuItemType.Delete));
                    break;
                case TreeDataType.FilterPlan:
                    contextMenu.Items.Add(GetMenuItem("Copy Filter Plan", MenuItemType.Copy));
                    contextMenu.Items.Add(GetMenuItem("Delete Filter Plan", MenuItemType.Delete));
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
        New, Paste, Copy, Delete
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
            return Instance.item;
        }

        private Clipboard() { }
    }
}
