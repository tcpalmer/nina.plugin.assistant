using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using LinqKit;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.AcquiredImages {

    public class AcquiredImagesManagerViewVM : BaseVM {

        private AssistantDatabaseInteraction database;

        /*
         * TODO:
         * - Row detail view
         * - Remove data older than date
         */

        public AcquiredImagesManagerViewVM(IProfileService profileService) : base(profileService) {

            database = new AssistantDatabaseInteraction();

            InitializeCriteria();
            LoadRecords();
        }

        private void InitializeCriteria() {
            FromDate = DateTime.Now.AddDays(-9);
            ToDate = DateTime.Now;

            AsyncObservableCollection<KeyValuePair<int, string>> projectChoices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(0, Loc.Instance["LblAny"])
            };

            Dictionary<Project, string> dict = GetProjectsDictionary();
            foreach (KeyValuePair<Project, string> entry in dict) {
                projectChoices.Add(new KeyValuePair<int, string>(entry.Key.Id, entry.Value));
            }

            ProjectChoices = projectChoices;
            TargetChoices = GetTargetChoices(SelectedTargetId);
        }

        private ICollectionView itemsView;
        public ICollectionView ItemsView {
            get => itemsView;
            set {
                itemsView = value;
            }
        }

        private DateTime fromDate;
        public DateTime FromDate {
            get => fromDate;
            set {
                fromDate = value.Date;
                LoadRecords();
            }
        }

        private DateTime toDate;
        public DateTime ToDate {
            get => toDate;
            set {
                toDate = value.AddDays(1).Date.AddSeconds(-1);
                LoadRecords();
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> projectChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> ProjectChoices {
            get {
                return projectChoices;
            }
            set {
                projectChoices = value;
            }
        }

        private int selectedProjectId = 0;
        public int SelectedProjectId {
            get => selectedProjectId;
            set {
                selectedProjectId = value;

                SelectedTargetId = 0;
                TargetChoices = GetTargetChoices(selectedProjectId);

                LoadRecords();
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> GetTargetChoices(int selectedProjectId) {
            List<Target> targets;
            AsyncObservableCollection<KeyValuePair<int, string>> choices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(0, Loc.Instance["LblAny"])
            };

            if (selectedProjectId == 0) {
                return choices;
            }

            using (var context = database.GetContext()) {
                targets = context.TargetSet.AsNoTracking().Where(t => t.ProjectId == selectedProjectId).ToList();
            }

            targets.ForEach(t => {
                choices.Add(new KeyValuePair<int, string>(t.Id, t.Name));
            });

            return choices;
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> targetChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> TargetChoices {
            get {
                return targetChoices;
            }
            set {
                targetChoices = value;
                RaisePropertyChanged(nameof(TargetChoices));
            }
        }

        private int selectedTargetId = 0;
        public int SelectedTargetId {
            get => selectedTargetId;
            set {
                selectedTargetId = value;
                RaisePropertyChanged(nameof(SelectedTargetId));
                LoadRecords();
            }
        }

        // TODO: make this async
        private void LoadRecords() {

            string newSearchCriteraKey = GetSearchCriteraKey();
            if (newSearchCriteraKey == SearchCriteraKey) {
                return;
            }

            SearchCriteraKey = newSearchCriteraKey;
            var predicate = PredicateBuilder.New<AcquiredImage>();

            long from = AssistantDatabaseContext.DateTimeToUnixSeconds(FromDate);
            long to = AssistantDatabaseContext.DateTimeToUnixSeconds(ToDate);
            predicate = predicate.And(a => a.acquiredDate >= from);
            predicate = predicate.And(a => a.acquiredDate <= to);

            if (SelectedProjectId != 0) {
                predicate = predicate.And(a => a.ProjectId == SelectedProjectId);
            }

            if (SelectedTargetId != 0) {
                predicate = predicate.And(a => a.TargetId == SelectedTargetId);
            }

            List<AcquiredImage> acquiredImages;
            using (var context = database.GetContext()) {
                acquiredImages = context.AcquiredImageSet.AsNoTracking().AsExpandable().Where(predicate).ToList();
            }

            List<AcquiredImageVM> acquiredImagesVM = new List<AcquiredImageVM>(acquiredImages.Count);
            acquiredImages.ForEach(a => { acquiredImagesVM.Add(new AcquiredImageVM(a)); });

            ItemsView = CollectionViewSource.GetDefaultView(acquiredImagesVM);
            ItemsView.SortDescriptions.Add(new SortDescription("AcquiredDate", ListSortDirection.Descending));

            RaisePropertyChanged(nameof(ItemsView));
        }

        private string SearchCriteraKey;

        private string GetSearchCriteraKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{FromDate:yyyy-MM-dd}_{ToDate:yyyy-MM-dd}_");
            sb.Append($"{SelectedProjectId}_{SelectedTargetId}");
            return sb.ToString();
        }

        private Dictionary<Project, string> GetProjectsDictionary() {

            List<Project> projects;
            using (var context = database.GetContext()) {
                projects = context.ProjectSet.AsNoTracking().OrderBy(p => p.name).ToList();
            }

            Dictionary<Project, string> dict = new Dictionary<Project, string>();
            projects.ForEach(p => { dict.Add(p, p.Name); });
            return dict;
        }
    }

    public class AcquiredImageVM {

        private AcquiredImage acquiredImage;

        public AcquiredImageVM(AcquiredImage acquiredImage) {
            this.acquiredImage = acquiredImage;

            AssistantDatabaseInteraction database = new AssistantDatabaseInteraction();
            using (var context = database.GetContext()) {
                Project p = context.GetProject(acquiredImage.ProjectId);
                ProjectName = p?.Name;
                Target t = context.GetTarget(acquiredImage.ProjectId, acquiredImage.TargetId);
                TargetName = t?.Name;
            }
        }

        public DateTime AcquiredDate { get { return acquiredImage.AcquiredDate; } }
        public string FilterName { get { return acquiredImage.FilterName; } }
        public string ProjectName { get; private set; }
        public string TargetName { get; private set; }
        public bool Accepted { get { return acquiredImage.Accepted; } }
        public int DetectedStars { get { return acquiredImage.Metadata.DetectedStars; } }
        public double HFR { get { return acquiredImage.Metadata.HFR; } }
    }
}
