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
            //ProjectTreeView.MouseRightButtonDown += ProjectTreeView_MouseRightButtonDown;

            List<TreeDataItem> profiles = LoadProjectTree();
            ((ManagerVM)DataContext).Profiles = profiles;
        }

        // TODO: how to handle item context menus?
        /*
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
        }*/

        // TODO: works great to get and act on the selected item (e.g. load details into main view)
        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            TreeDataItem item = e.NewValue as TreeDataItem;
            if (item != null) {
                Logger.Info($"SIC: {item.Type} {item.Header.ToString()}");
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

    // TODO:
    // Can we move following into VM?
    // Check out some NINA M, V, and VM instances for partitioning and responsibilities
    //

    public enum TreeDataType {
        Folder, Profile, Project, Target, FilterPlan
    }

    public class TreeDataItem : TreeViewItem {

        public TreeDataType Type { get; }
        public object Data { get; }

        public TreeDataItem(TreeDataType type, string name) : this(type, name, null) { }

        public TreeDataItem(TreeDataType type, string name, object data) {
            Type = type;
            Data = data;
            Header = name;

            if (Type != TreeDataType.Folder) {
                MouseRightButtonDown += TreeDataItem_MouseRightButtonDown;
            }
        }

        // This works to add the context menus
        private void TreeDataItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            Logger.Info($"TDI RMD: {Type} {Header}");

            ContextMenu = GetContextMenu();
            e.Handled = true;
        }

        private ContextMenu GetContextMenu() {
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.Items.Add(GetContextMenuItem(Type.ToString()));
            contextMenu.Items.Add(GetContextMenuItem("foo"));
            contextMenu.Items.Add(GetContextMenuItem("bar"));
            return contextMenu;
        }

        private MenuItem GetContextMenuItem(string header) {
            MenuItem menuItem = new MenuItem();
            menuItem.Header = header;
            menuItem.Click += MenuItem_Click;
            return menuItem;
        }

        // This works to get the context menu item clicked and getting the header tells you
        // which MI was clicked so can compare that string and take action
        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            Logger.Info($"MI MC: {e.OriginalSource.GetType()} {Type} {Header}");
            MenuItem menuItem = e.OriginalSource as MenuItem;
            Logger.Info($"  MIH: {menuItem.Header}");
            e.Handled = true;
        }

        public override string ToString() {
            return Header.ToString();
        }
    }
}
