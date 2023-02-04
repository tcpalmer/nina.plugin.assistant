using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using NINA.Profile;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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

            List<ITreeDataItem> profiles = LoadProjectTree();
            ((ManagerVM)DataContext).Profiles = profiles;
        }

        private List<ITreeDataItem> LoadProjectTree() {
            ProjectTreeView.Items.Clear();

            List<ITreeDataItem> profiles = new List<ITreeDataItem>();
            ITreeDataItem profilesFolder = new TreeDataItem(TreeDataType.Folder, "Profiles");
            profiles.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    ITreeDataItem profileItem = new TreeDataItem(TreeDataType.Profile, profile.Name, profile);
                    profilesFolder.Add(profileItem);

                    List<Project> projects = context.GetAllProjects(profile.Id.ToString());
                    foreach (Project project in projects) {
                        ITreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.name, project);
                        profileItem.Add(projectItem);

                        foreach (Target target in project.targets) {
                            ITreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.name, target);
                            projectItem.Add(targetItem);

                            foreach (FilterPlan filterPlan in target.filterplans) {
                                ITreeDataItem filterPlanItem = new TreeDataItem(TreeDataType.FilterPlan, filterPlan.filterName, filterPlan);
                                targetItem.Add(filterPlanItem);
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

    public interface ITreeDataItem {
        TreeDataType Type { get; }
        string Name { get; }
        object Data { get; }
        List<ITreeDataItem> Items { get; }
        void Add(ITreeDataItem item);
    }

    public class TreeDataItem : ITreeDataItem {

        public TreeDataType Type { get; }
        public string Name { get; }
        public object Data { get; }
        public List<ITreeDataItem> Items { get; }

        public TreeDataItem(TreeDataType type, string name) : this(type, name, null) { }

        public TreeDataItem(TreeDataType type, string name, object data) {
            Type = type;
            Name = name;
            Data = data;
            Items = new List<ITreeDataItem>();
        }

        public void Add(ITreeDataItem item) {
            Items.Add(item);
        }

        public override string ToString() {
            return Name;
        }
    }
}
