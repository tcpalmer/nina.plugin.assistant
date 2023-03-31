using Assistant.NINAPlugin.Controls.Util;
using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using LinqKit;
using NINA.Core.Model.Equipment;
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
        private SchedulerDatabaseInteraction database;

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

            database = new SchedulerDatabaseInteraction();

            SelectedItemChangedCommand = new RelayCommand(SelectedItemChanged);
        }

        public AssistantTreeViewVM ProjectsTreeViewVM {
            get => new AssistantTreeViewVM(this, profileService, "Projects", RootProjectsList, 350);
        }

        public AssistantTreeViewVM ExposureTemplatesTreeViewVM {
            get => new AssistantTreeViewVM(this, profileService, "Exposure Templates", RootExposureTemplateList, 210);
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

        private Visibility showOrphanedProjectsView = Visibility.Hidden;
        public Visibility ShowOrphanedProjectsView {
            get => showOrphanedProjectsView;
            set {
                showOrphanedProjectsView = value;
                RaisePropertyChanged(nameof(ShowOrphanedProjectsView));
            }
        }

        private OrphanedProjectsViewVM orphanedProjectsViewVM;
        public OrphanedProjectsViewVM OrphanedProjectsViewVM {
            get => orphanedProjectsViewVM;
            set {
                orphanedProjectsViewVM = value;
                RaisePropertyChanged(nameof(OrphanedProjectsViewVM));
            }
        }

        private List<Project> orphanedProjects;
        public List<Project> OrphanedProjects {
            get => orphanedProjects;
            set {
                orphanedProjects = value;
                RaisePropertyChanged(nameof(OrphanedProjects));
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

        private Visibility showExposureTemplateProfileView = Visibility.Hidden;
        public Visibility ShowExposureTemplateProfileView {
            get => showExposureTemplateProfileView;
            set {
                showExposureTemplateProfileView = value;
                RaisePropertyChanged(nameof(ShowExposureTemplateProfileView));
            }
        }

        private ExposureTemplateProfileViewVM exposureTemplateProfileViewVM;
        public ExposureTemplateProfileViewVM ExposureTemplateProfileViewVM {
            get => exposureTemplateProfileViewVM;
            set {
                exposureTemplateProfileViewVM = value;
                RaisePropertyChanged(nameof(ExposureTemplateProfileViewVM));
            }
        }

        private Visibility showExposureTemplateView = Visibility.Hidden;
        public Visibility ShowExposureTemplateView {
            get => showExposureTemplateView;
            set {
                showExposureTemplateView = value;
                RaisePropertyChanged(nameof(ShowExposureTemplateView));
            }
        }

        private ExposureTemplateViewVM exposureTemplateViewVM;
        public ExposureTemplateViewVM ExposureTemplateViewVM {
            get => exposureTemplateViewVM;
            set {
                exposureTemplateViewVM = value;
                RaisePropertyChanged(nameof(ExposureTemplateViewVM));
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
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
                        ShowProfileView = Visibility.Visible;
                        break;

                    case TreeDataType.OrphanedProjects:
                        activeTreeDataItem = item;
                        OrphanedProjectsViewVM = new OrphanedProjectsViewVM(this, profileService, item, OrphanedProjects);
                        ShowProfileView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Visible;
                        break;

                    case TreeDataType.Project:
                        activeTreeDataItem = item;
                        Project project = (Project)item.Data;
                        ProjectViewVM = new ProjectViewVM(this, profileService, project);
                        ShowProfileView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Visible;
                        break;

                    case TreeDataType.Target:
                        activeTreeDataItem = item;
                        Target target = (Target)item.Data;
                        TargetViewVM = new TargetViewVM(this, profileService, applicationMediator, framingAssistantVM, deepSkyObjectSearchVM, planetariumFactory, target);
                        ShowProfileView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Visible;
                        break;

                    case TreeDataType.ExposureTemplateProfile:
                        activeTreeDataItem = item;
                        ExposureTemplateProfileViewVM = new ExposureTemplateProfileViewVM(this, profileService, item);
                        ShowProfileView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Visible;
                        break;

                    case TreeDataType.ExposureTemplate:
                        activeTreeDataItem = item;
                        ExposureTemplate exposureTemplate = (ExposureTemplate)item.Data;
                        ExposureTemplateViewVM = new ExposureTemplateViewVM(this, profileService, exposureTemplate);
                        ShowProfileView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Visible;
                        break;

                    default:
                        activeTreeDataItem = null;
                        ShowProfileView = Visibility.Collapsed;
                        ShowOrphanedProjectsView = Visibility.Collapsed;
                        ShowProjectView = Visibility.Collapsed;
                        ShowTargetView = Visibility.Collapsed;
                        ShowExposureTemplateProfileView = Visibility.Collapsed;
                        ShowExposureTemplateView = Visibility.Collapsed;
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

        public List<TreeDataItem> Refresh(List<TreeDataItem> rootList) {
            if (rootList == RootProjectsList) {
                RootProjectsList = LoadProjectsTree();
                TreeDataItem.VisitAll(RootProjectsList[0], i => { i.IsExpanded = false; });
                return RootProjectsList;
            }

            if (rootList == RootExposureTemplateList) {
                RootExposureTemplateList = LoadExposureTemplateTree();
                TreeDataItem.VisitAll(RootExposureTemplateList[0], i => { i.IsExpanded = false; });
                return RootExposureTemplateList;
            }

            Logger.Warning("failed to determine the root list for scheduler manager");
            return null;
        }

        List<TreeDataItem> rootProjectsList;
        public List<TreeDataItem> RootProjectsList {
            get {
                if (rootProjectsList == null) {
                    rootProjectsList = LoadProjectsTree();
                }
                return rootProjectsList;
            }
            set {
                rootProjectsList = value;
                RaisePropertyChanged(nameof(RootProjectsList));
            }
        }

        List<TreeDataItem> rootExposureTemplateList;
        public List<TreeDataItem> RootExposureTemplateList {
            get {
                if (rootExposureTemplateList == null) {
                    rootExposureTemplateList = LoadExposureTemplateTree();
                }
                return rootExposureTemplateList;
            }
            set {
                rootExposureTemplateList = value;
                RaisePropertyChanged(nameof(RootExposureTemplateList));
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

                        projectItem.SortChildren();
                    }

                    profileItem.SortChildren();
                }

                // Handle 'orphaned' projects (associated profile has been deleted)
                List<Project> orphanedProjects = GetOrphanedProjects(profileService, context);
                if (orphanedProjects.Count > 0) {
                    OrphanedProjects = orphanedProjects;
                    TreeDataItem pseudoProfileItem = new TreeDataItem(TreeDataType.OrphanedProjects, "ORPHANED", null, profilesFolder);
                    profilesFolder.Items.Add(pseudoProfileItem);
                }

                profilesFolder.SortChildren();
            }

            return rootList;
        }

        private List<Project> GetOrphanedProjects(IProfileService profileService, SchedulerDatabaseContext context) {
            List<string> currentProfileIds = new List<string>();
            profileService.Profiles.ForEach(p => currentProfileIds.Add(p.Id.ToString()));
            return context.GetOrphanedProjects(currentProfileIds);
        }

        private List<TreeDataItem> LoadExposureTemplateTree() {

            List<TreeDataItem> rootList = new List<TreeDataItem>();
            TreeDataItem profilesFolder = new TreeDataItem(TreeDataType.ExposureTemplateRoot, "Profiles", null);
            rootList.Add(profilesFolder);

            using (var context = database.GetContext()) {
                foreach (ProfileMeta profile in profileService.Profiles) {
                    TreeDataItem profileItem = new TreeDataItem(TreeDataType.ExposureTemplateProfile, profile.Name, profile, profilesFolder);
                    profilesFolder.Items.Add(profileItem);

                    List<ExposureTemplate> exposureTemplates = context.GetExposureTemplates(profile.Id.ToString());
                    foreach (ExposureTemplate exposureTemplate in exposureTemplates) {
                        TreeDataItem exposureTemplateItem = new TreeDataItem(TreeDataType.ExposureTemplate, exposureTemplate.Name, exposureTemplate, profileItem);
                        profileItem.Items.Add(exposureTemplateItem);
                    }

                    // We could sort ETs into filter -> filter wheel order
                }

                profilesFolder.SortChildren();
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

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Project newProject = context.SaveProject(project);
                if (newProject != null) {
                    TreeDataItem projectItem = new TreeDataItem(TreeDataType.Project, project.Name, project, parentItem);
                    parentItem.Items.Add(projectItem);
                    projectItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to save new Scheduler Project (see log for details)");
                }
            }
        }

        public void SaveProject(Project project) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.SaveProject(project) != null) {
                    activeTreeDataItem.Data = project;
                    if (activeTreeDataItem.Header.ToString() != project.Name) {
                        activeTreeDataItem.Header = project.Name;
                        activeTreeDataItem.SortName = project.Name;
                    }
                }
                else {
                    Notification.ShowError("Failed to save Scheduler Project (see log for details)");
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
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
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
                    Notification.ShowError("Failed to paste new Scheduler Project (see log for details)");
                }
            }
        }

        public bool MoveOrphanedProject(Project project, string profileId) {
            // TODO: move the project to profileId and then delete it
            throw new NotImplementedException();
        }

        public void DeleteProject(Project project, bool isOrphan) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.DeleteProject(project)) {
                    if (!isOrphan) {
                        TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                        parentItem.Items.Remove(activeTreeDataItem);
                        parentItem.IsSelected = true;
                    }
                }
                else {
                    Notification.ShowError("Failed to delete Scheduler Project (see log for details)");
                }
            }
        }

        public void AddNewTarget(Project project) {
            Target target = new Target();
            target.Name = "<new target>";
            TreeDataItem parentItem = activeTreeDataItem;

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target newTarget = context.AddNewTarget(project, target);
                if (newTarget != null) {
                    TreeDataItem targetItem = new TreeDataItem(TreeDataType.Target, target.Name, target, parentItem);
                    parentItem.Items.Add(targetItem);
                    targetItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to add new Scheduler Target (see log for details)");
                }
            }
        }

        public void SaveTarget(Target target) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.SaveTarget(target) != null) {
                    activeTreeDataItem.Data = target;
                    if (activeTreeDataItem.Header.ToString() != target.Name) {
                        activeTreeDataItem.Header = target.Name;
                        activeTreeDataItem.SortName = target.Name;
                    }

                    // Refresh the parent project
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Data = context.GetProject(target.ProjectId);
                }
                else {
                    Notification.ShowError("Failed to save Scheduler Target (see log for details)");
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

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target newTarget = context.PasteTarget(project, source);
                if (newTarget != null) {
                    TreeDataItem newTargetItem = new TreeDataItem(TreeDataType.Target, newTarget.Name, newTarget, parentItem);
                    parentItem.Items.Add(newTargetItem);
                    newTargetItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to paste new Scheduler Project (see log for details)");
                }
            }
        }

        public void DeleteTarget(Target target) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.DeleteTarget(target)) {
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Items.Remove(activeTreeDataItem);
                    parentItem.IsSelected = true;
                }
                else {
                    Notification.ShowError("Failed to delete Scheduler Target (see log for details)");
                }
            }
        }

        public Target DeleteExposurePlan(Target target, ExposurePlan exposurePlan) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target updatedTarget = context.DeleteExposurePlan(target, exposurePlan);
                if (updatedTarget != null) {
                    activeTreeDataItem.Data = updatedTarget;
                }
                else {
                    Notification.ShowError("Failed to delete Scheduler Exposure Plan (see log for details)");
                }

                return updatedTarget;
            }
        }

        public Target ReloadTarget(Target reference) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                Target reloadedTarget = context.GetTargetByProject(reference.ProjectId, reference.Id);
                if (reloadedTarget != null) {
                    activeTreeDataItem.Data = reloadedTarget;
                }
                else {
                    Notification.ShowError("Failed to reload target (see log for details)");
                }

                return reloadedTarget;
            }
        }

        public void AddNewExposureTemplate(TreeDataItem parentItem) {
            ProfileMeta profileMeta = (ProfileMeta)parentItem.Data;

            IProfile profile = GetProfile(profileMeta.Id.ToString());
            FilterInfo filterInfo = profile?.FilterWheelSettings?.FilterWheelFilters.FirstOrDefault();
            if (filterInfo == null) {
                Logger.Error("failed to get the first filter in profile's filter wheel");
                Notification.ShowError("Scheduler: failed to get the first filter in profile's filter wheel");
                return;
            }

            ExposureTemplate exposureTemplate = new ExposureTemplate(profileMeta.Id.ToString(), "<new template>", filterInfo.Name);

            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                ExposureTemplate newExposureTemplate = context.SaveExposureTemplate(exposureTemplate);
                if (newExposureTemplate != null) {
                    TreeDataItem exposureTemplateItem = new TreeDataItem(TreeDataType.ExposureTemplate, exposureTemplate.Name, exposureTemplate, parentItem);
                    parentItem.Items.Add(exposureTemplateItem);
                    exposureTemplateItem.IsSelected = true;
                    parentItem.IsExpanded = true;
                }
                else {
                    Notification.ShowError("Failed to save new Scheduler Exposure Template (see log for details)");
                }
            }
        }

        public void PasteExposureTemplate(TreeDataItem parentItem) {
            ProfileMeta profile = (ProfileMeta)parentItem.Data;

            if (!Clipboard.HasType(TreeDataType.ExposureTemplate)) {
                Logger.Error($"expected clipboard to hold Exposure Template");
                return;
            }

            ExposureTemplate source = Clipboard.GetItem().Data as ExposureTemplate;
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                ExposureTemplate newExposureTemplate = context.PasteExposureTemplate(profile.Id.ToString(), source);
                if (newExposureTemplate != null) {
                    TreeDataItem newExposureTemplateItem = new TreeDataItem(TreeDataType.ExposureTemplate, newExposureTemplate.Name, newExposureTemplate, parentItem);
                    parentItem.Items.Add(newExposureTemplateItem);
                    newExposureTemplateItem.IsSelected = true;
                    parentItem.IsExpanded = true;

                }
                else {
                    Notification.ShowError("Failed to paste new Scheduler Exposure Template (see log for details)");
                }
            }
        }

        public void SaveExposureTemplate(ExposureTemplate exposureTemplate) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.SaveExposureTemplate(exposureTemplate) != null) {
                    activeTreeDataItem.Data = exposureTemplate;
                    activeTreeDataItem.Header = exposureTemplate.Name;
                }
                else {
                    Notification.ShowError("Failed to save Scheduler Exposure Template (see log for details)");
                }
            }
        }

        public void DeleteExposureTemplate(ExposureTemplate exposureTemplate) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                if (context.DeleteExposureTemplate(exposureTemplate)) {
                    TreeDataItem parentItem = activeTreeDataItem.TreeParent;
                    parentItem.Items.Remove(activeTreeDataItem);
                    parentItem.IsSelected = true;
                }
                else {
                    Notification.ShowError("Failed to delete Scheduler Exposure Template (see log for details)");
                }
            }
        }

        public int ExposureTemplateUsage(int exposureTemplateId) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                return context.ExposurePlanSet.Where(ep => ep.ExposureTemplateId == exposureTemplateId).ToList().Count;
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

            Logger.Error($"Scheduler: failed to load profile, id = {profileId}");
            return null;
        }

        public List<ExposureTemplate> GetExposureTemplates(IProfile profile) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                return context.GetExposureTemplates(profile.Id.ToString());
            }
        }

        public ExposureTemplate GetDefaultExposureTemplate(IProfile profile) {
            using (var context = new SchedulerDatabaseInteraction().GetContext()) {
                return context.GetExposureTemplates(profile.Id.ToString()).FirstOrDefault();
            }
        }
    }

    public enum TreeDataType {
        ProjectRoot, ExposureTemplateRoot, ProjectProfile, OrphanedProjects, ExposureTemplateProfile, Project, Target, ExposureTemplate
    }

    public class TreeDataItem : TreeViewItem, IComparable {

        public TreeDataType Type { get; }
        public TreeDataItem TreeParent { get; }
        public string SortName { get; set; }
        public object Data { get; set; }

        public TreeDataItem(TreeDataType type, string name, TreeDataItem parent) : this(type, name, null, parent) { }

        public TreeDataItem(TreeDataType type, string name, object data, TreeDataItem parent) {
            Type = type;
            TreeParent = parent;
            Data = data;
            SortName = name;
            Header = name;
        }

        public TreeDataItem GetRoot() {
            TreeDataItem item = this;
            while (item.TreeParent != null) {
                item = item.TreeParent;
            }

            return item;
        }

        public int CompareTo(object obj) {
            if (obj == null) {
                return 1;
            }

            TreeDataItem other = obj as TreeDataItem;
            return SortName.CompareTo(other.SortName);
        }

        public void SortChildren() {
            if (Items?.Count == 0) {
                return;
            }

            // This approach works to sort the tree initially.  However, it doesn't when trying to use it to resort
            // when a new item is added or item is renamed.  I think because rebuilding the list this way horks
            // the collection for subsequent view access.  I tried to go down the road of getting the ItemCollection
            // and playing nice with sorting via SortDescriptions but that didn't seem to work for TreeView.

            List<TreeDataItem> list = new List<TreeDataItem>(Items.Count);
            foreach (TreeDataItem item in Items) {
                list.Add(item);
            }

            list.Sort();
            Items.Clear();
            list.ForEach(i => Items.Add(i));
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

