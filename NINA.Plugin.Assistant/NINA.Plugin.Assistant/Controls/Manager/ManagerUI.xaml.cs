using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.Manager {

    public partial class ManagerUI : UserControl {

        private AssistantDatabaseInteraction database;
        private IProfileService profileService;
        private IProfile activeProfile;

        /* TODO: 
         *  - When a project item is selected, display project view panel and load that project and prefs
         *  - When a target item is selected, display target view panel and load that target
         *  - When a filterPlan item is selected, display filterPlan view panel and load that filterPlan
         * 
         *  - When a profile item is right-clicked, display popup to add a project
         *  - When a project item is right-clicked, display popup to add a target
         *  - When a target item is right-clicked, display popup to add a filterPlan
         */

        public ManagerUI() {
            InitializeComponent();
            this.database = new AssistantDatabaseInteraction();
            this.profileService = AssistantHelper.GetProfileService();
            this.activeProfile = profileService.ActiveProfile;

            ProjectTreeView.SelectedItemChanged += ProjectTreeView_SelectedItemChanged;
            ProjectTreeView.MouseRightButtonDown += ProjectTreeView_MouseRightButtonDown;

            List<TreeDataItem> profiles = LoadProjectTree();
            ((ManagerVM)DataContext).Profiles = profiles;
        }

        // TODO: how to handle item context menus?
        private void ProjectTreeView_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            //e.OriginalSource
            Logger.Info($"MRD: sender={sender.GetType().ToString()}");
            Logger.Info($"MRD:  eosrc={e.OriginalSource.GetType().ToString()}");

            TextBlock textBlock = e.OriginalSource as TextBlock;
            var tbp = textBlock.Parent;
            if (tbp != null) {
                Logger.Info($"MRD: TBP: {tbp?.GetType().ToString()}");
            }
            else {
                Logger.Info("TBP: null");
            }

            //var item = ProjectTreeView.SelectedItem;
            //Logger.Info($"MRD: SIT: {item?.GetType().ToString()}");
        }

        // TODO: works great to get and act on the selected item (e.g. load details into main view)
        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            TreeDataItem item = e.NewValue as TreeDataItem;
            if (item != null) {
                Logger.Info($"SIC: {item.Type} {item.DisplayName}");
            }
        }

        private List<TreeDataItem> LoadProjectTree() {
            ProjectTreeView.Items.Clear();

            List<TreeDataItem> profiles = new List<TreeDataItem>();
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.Folder, "Profiles");
            profiles.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.Profile, profile.Name, profile);
                    profilesFolder.Items.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    foreach (Project project in projects) {
                        TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.name, project);
                        profileItem.Items.Add(projectItem);

                        foreach (Target target in project.targets) {
                            TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.name, target);
                            projectItem.Items.Add(targetItem);

                            foreach (FilterPlan filterPlan in target.filterplans) {
                                TreeDataItem filterPlanItem = new TreeDataItem(TreeDataType.FilterPlan, filterPlan.filterName, filterPlan);
                                targetItem.Items.Add(filterPlanItem);
                            }
                        }
                    }
                }
            }

            return profiles;
        }

        private void LoadProjectTreeManual() {
            //ProjectTreeView.Items.Clear();

            /* TODO:
             * - How to bind a dropdown menu when certain items are clicked?
             */

            /*
             * TODO: Have to figure out how to use TreeViewItem.ItemsSource to bind the list of items under a node
             * Maybe have a simple interface for the bound data item that encapsulates the db object
             * plus a label to use.  Could also bind labels only for the folder nodes.  Has a Children list.
             * 
             * We could create a hierarchical list of these and just bind it to the root.  When items are clicked,
             * we can get the associated database object (if present) and act on that.
             */

            using (var context = database.GetContext()) {

                TreeViewItem profilesFolder = new TreeViewItem();
                profilesFolder.Header = "Profiles";
                //ProjectTreeView.Items.Add(profilesFolder);

                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeViewItem profileItem = new TreeViewItem();
                    profileItem.Header = profile.Name;
                    profilesFolder.Items.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    Logger.Debug($"loaded {projects.Count} projects for id {profile.Id.ToString()}");

                    TreeViewItem projectsFolder = new TreeViewItem();
                    if (projects.Count > 0) {
                        projectsFolder.Header = "Projects";
                        profileItem.Items.Add(projectsFolder);
                    }

                    foreach (Project project in projects) {
                        TreeViewItem projectItem = new TreeViewItem();
                        projectItem.Header = project.name;
                        projectItem.Selected += Project_Selected;
                        System.Collections.IEnumerable foo = null;
                        projectItem.ItemsSource = foo;
                        projectsFolder.Items.Add(projectItem);

                        TreeViewItem targetsFolder = new TreeViewItem();
                        if (project.targets.Count > 0) {
                            targetsFolder.Header = "Targets";
                            projectItem.Items.Add(targetsFolder);
                        }

                        foreach (Target target in project.targets) {
                            TreeViewItem targetItem = new TreeViewItem();
                            targetItem.Header = target.name;
                            targetItem.Selected += Target_Selected;
                            targetsFolder.Items.Add(targetItem);
                        }
                    }
                }
            }
        }

        private void Project_Selected(object sender, RoutedEventArgs e) {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                Logger.Debug($"project selected: {item.Header}");
            }
        }

        private void Target_Selected(object sender, RoutedEventArgs e) {
            TreeViewItem item = sender as TreeViewItem;
            if (item != null) {
                Logger.Debug($"target selected: {item.Header}");
            }
        }
    }

    public enum TreeDataType {
        Folder, Profile, Project, Target, FilterPlan
    }

    public class TreeDataItem {

        public TreeDataType Type { get; }
        public string DisplayName { get; }
        public object Data { get; }
        public List<TreeDataItem> Items { get; }

        public TreeDataItem(TreeDataType type, string name) : this(type, name, null) { }

        public TreeDataItem(TreeDataType type, string name, object data) {
            Type = type;
            DisplayName = name;
            Data = data;
            Items = new List<TreeDataItem>();
        }

        public override string ToString() {
            return DisplayName;
        }

        // This mouse click code (with associated xaml) WILL detect right mouse down on
        // one of the tree nodes.  But how would you get the TreeViewItem to add the context menu?
        private RelayCommand treeItemMouseClick;

        public ICommand TreeItemMouseClick {
            get {
                if (treeItemMouseClick == null) {
                    treeItemMouseClick = new RelayCommand(PerformTreeItemMouseClick);
                }

                return treeItemMouseClick;
            }
        }

        private void PerformTreeItemMouseClick(object commandParameter) {
            Logger.Info($"PTIMC: {Type} {DisplayName}");
        }
    }
}
