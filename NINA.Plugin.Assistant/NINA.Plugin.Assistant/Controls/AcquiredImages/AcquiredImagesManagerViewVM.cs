using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using Assistant.NINAPlugin.Util;
using LinqKit;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Assistant.NINAPlugin.Controls.AcquiredImages {

    public class AcquiredImagesManagerViewVM : BaseVM {

        private SchedulerDatabaseInteraction database;

        public AcquiredImagesManagerViewVM(IProfileService profileService) : base(profileService) {

            database = new SchedulerDatabaseInteraction();

            RefreshTableCommand = new AsyncCommand<bool>(() => RefreshTable());
            InitializeCriteria();

            AcquiredImageCollection = new AcquiredImageCollection();
            ItemsView = CollectionViewSource.GetDefaultView(AcquiredImageCollection);
            ItemsView.SortDescriptions.Clear();
            ItemsView.SortDescriptions.Add(new SortDescription("AcquiredDate", ListSortDirection.Descending));

            _ = LoadRecords();
        }

        private static readonly int FIXED_DATE_RANGE_OFF = 0;
        private static readonly int FIXED_DATE_RANGE_DEFAULT = 2;

        private void InitializeCriteria() {

            FixedDateRangeChoices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(FIXED_DATE_RANGE_OFF, ""),
                new KeyValuePair<int, string>(1, "Today"),
                new KeyValuePair<int, string>(2, "Last 2 Days"),
                new KeyValuePair<int, string>(7, "Last 7 Days"),
                new KeyValuePair<int, string>(30, "Last 30 Days"),
                new KeyValuePair<int, string>(60, "Last 60 Days"),
                new KeyValuePair<int, string>(90, "Last 90 Days"),
                new KeyValuePair<int, string>(180, "Last 180 Days"),
                new KeyValuePair<int, string>(365, "Last Year")
            };

            // Setting like this allows for initial combo selection
            SelectedFixedDateRange = FIXED_DATE_RANGE_DEFAULT;
            selectedFixedDateRange = FIXED_DATE_RANGE_DEFAULT;

            AsyncObservableCollection<KeyValuePair<int, string>> projectChoices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(0, Loc.Instance["LblAny"])
            };

            Dictionary<Project, string> dict = GetProjectsDictionary();
            foreach (KeyValuePair<Project, string> entry in dict) {
                projectChoices.Add(new KeyValuePair<int, string>(entry.Key.Id, entry.Value));
            }

            ProjectChoices = projectChoices;
            TargetChoices = GetTargetChoices(SelectedProjectId);
            FilterChoices = GetFilterChoices(SelectedTargetId);
        }

        private bool tableLoading = false;
        public bool TableLoading {
            get => tableLoading;
            set {
                tableLoading = value;
                RaisePropertyChanged(nameof(TableLoading));
            }
        }

        private ICollectionView itemsView;
        public ICollectionView ItemsView {
            get => itemsView;
            set {
                itemsView = value;
            }
        }

        private DateTime fromDate = DateTime.MinValue;
        public DateTime FromDate {
            get => fromDate;
            set {
                fromDate = value.Date;
                SelectedFixedDateRange = FIXED_DATE_RANGE_OFF;
                RaisePropertyChanged(nameof(FromDate));
                _ = LoadRecords();
            }
        }

        private DateTime toDate = DateTime.MinValue;
        public DateTime ToDate {
            get => toDate;
            set {
                toDate = value.AddDays(1).Date.AddSeconds(-1);
                SelectedFixedDateRange = FIXED_DATE_RANGE_OFF;
                RaisePropertyChanged(nameof(ToDate));
                _ = LoadRecords();
            }
        }

        private int selectedFixedDateRange;
        public int SelectedFixedDateRange {
            get => selectedFixedDateRange;
            set {
                selectedFixedDateRange = value;
                RaisePropertyChanged(nameof(SelectedFixedDateRange));

                if (selectedFixedDateRange != FIXED_DATE_RANGE_OFF) {
                    // OK ... but not setting the TIME properly.  Refactor out from and to functions
                    // Sure?
                    fromDate = DateTime.Now.AddDays((-1 * selectedFixedDateRange) + 1);
                    toDate = DateTime.Now;
                    RaisePropertyChanged(nameof(FromDate));
                    RaisePropertyChanged(nameof(ToDate));
                    _ = LoadRecords();
                }
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> fixedDateRangeChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> FixedDateRangeChoices {
            get {
                return fixedDateRangeChoices;
            }
            set {
                fixedDateRangeChoices = value;
                RaisePropertyChanged(nameof(FixedDateRangeChoices));
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> projectChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> ProjectChoices {
            get {
                return projectChoices;
            }
            set {
                projectChoices = value;
                RaisePropertyChanged(nameof(ProjectChoices));
            }
        }

        private int selectedProjectId = 0;
        public int SelectedProjectId {
            get => selectedProjectId;
            set {
                selectedProjectId = value;

                SelectedTargetId = 0;
                TargetChoices = GetTargetChoices(selectedProjectId);
                RaisePropertyChanged(nameof(SelectedProjectId));

                _ = LoadRecords();
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

                SelectedFilterId = 0;
                FilterChoices = GetFilterChoices(selectedTargetId);
                RaisePropertyChanged(nameof(SelectedTargetId));
                _ = LoadRecords();
            }
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> GetFilterChoices(int selectedTargetId) {
            List<ExposurePlan> exposurePlans;
            List<ExposureTemplate> exposureTemplates;

            AsyncObservableCollection<KeyValuePair<int, string>> choices = new AsyncObservableCollection<KeyValuePair<int, string>> {
                new KeyValuePair<int, string>(0, Loc.Instance["LblAny"])
            };

            if (SelectedProjectId == 0 || selectedTargetId == 0) {
                return choices;
            }

            using (var context = database.GetContext()) {
                Target t = context.GetTarget(SelectedProjectId, selectedTargetId);
                exposureTemplates = GetExposureTemplates();
                exposurePlans = t.ExposurePlans;
            }

            exposurePlans.ForEach(ep => {
                ExposureTemplate et = exposureTemplates.Where(et => et.Id == ep.ExposureTemplate.Id).FirstOrDefault();
                choices.Add(new KeyValuePair<int, string>(et.Id, et.FilterName));
            });

            return choices;
        }

        private AsyncObservableCollection<KeyValuePair<int, string>> filterChoices;
        public AsyncObservableCollection<KeyValuePair<int, string>> FilterChoices {
            get {
                return filterChoices;
            }
            set {
                filterChoices = value;
                RaisePropertyChanged(nameof(FilterChoices));
            }
        }

        private int selectedFilterId = 0;
        public int SelectedFilterId {
            get => selectedFilterId;
            set {
                selectedFilterId = value;
                RaisePropertyChanged(nameof(SelectedFilterId));
                _ = LoadRecords();
            }
        }

        public ICommand RefreshTableCommand { get; private set; }

        private async Task<bool> RefreshTable() {
            SearchCriteraKey = null;
            InitializeCriteria();
            await LoadRecords();
            return true;
        }

        private AcquiredImageCollection acquiredImageCollection;
        public AcquiredImageCollection AcquiredImageCollection {
            get => acquiredImageCollection;
            set {
                acquiredImageCollection = value;
                RaisePropertyChanged(nameof(AcquiredImageCollection));
            }
        }

        private static Dispatcher _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        private object lockObj = new object();

        private async Task<bool> LoadRecords() {

            return await Task.Run(() => {

                if (AcquiredImageCollection == null || FromDate == DateTime.MinValue || ToDate == DateTime.MinValue) {
                    return true;
                }

                string newSearchCriteraKey = GetSearchCriteraKey();
                if (newSearchCriteraKey == SearchCriteraKey) {
                    return true;
                }

                // Slight delay allows the UI thread to update the spinner property before the dispatcher
                // thread starts ... which seems to block the UI updates.
                TableLoading = true;
                Thread.Sleep(50);

                try {
                    SearchCriteraKey = newSearchCriteraKey;
                    var predicate = PredicateBuilder.New<AcquiredImage>();

                    long from = SchedulerDatabaseContext.DateTimeToUnixSeconds(FromDate);
                    long to = SchedulerDatabaseContext.DateTimeToUnixSeconds(ToDate);
                    predicate = predicate.And(a => a.acquiredDate >= from);
                    predicate = predicate.And(a => a.acquiredDate <= to);

                    if (SelectedProjectId != 0) {
                        predicate = predicate.And(a => a.ProjectId == SelectedProjectId);
                    }

                    if (SelectedTargetId != 0) {
                        predicate = predicate.And(a => a.TargetId == SelectedTargetId);
                    }

                    if (SelectedFilterId != 0) {
                        List<ExposureTemplate> exposureTemplates = GetExposureTemplates();
                        ExposureTemplate exposureTemplate = exposureTemplates.Where(et => et.Id == SelectedFilterId).FirstOrDefault();
                        predicate = predicate.And(a => a.FilterName == exposureTemplate.FilterName);
                    }

                    List<AcquiredImage> acquiredImages;
                    using (var context = database.GetContext()) {
                        acquiredImages = context.AcquiredImageSet.AsNoTracking().AsExpandable().Where(predicate).ToList();
                    }

                    // Create an intermediate list so we can add it to the display collection via AddRange while suppressing notifications
                    List<AcquiredImageVM> acquiredImageVMs = new List<AcquiredImageVM>(acquiredImages.Count);
                    acquiredImages.ForEach(a => { acquiredImageVMs.Add(new AcquiredImageVM(a)); });

                    _dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                        AcquiredImageCollection.Clear();
                        AcquiredImageCollection.AddRange(acquiredImageVMs);
                    }));

                }
                catch (Exception ex) {
                    TSLogger.Error($"exception loading acquired images: {ex.Message} {ex.StackTrace}");
                }
                finally {
                    RaisePropertyChanged(nameof(AcquiredImageCollection));
                    RaisePropertyChanged(nameof(ItemsView));
                    TableLoading = false;
                }

                return true;
            });
        }

        private string SearchCriteraKey;

        private string GetSearchCriteraKey() {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{FromDate:yyyy-MM-dd}_{ToDate:yyyy-MM-dd}_");
            sb.Append($"{SelectedProjectId}_{SelectedTargetId}_{SelectedFilterId}");
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

        private List<ExposureTemplate> GetExposureTemplates() {
            using (var context = database.GetContext()) {
                Project p = context.GetProject(SelectedProjectId);
                return context.GetExposureTemplates(p.ProfileId);
            }
        }
    }

    public class RangeAsyncObservableCollection<T> : ObservableCollection<T> {
        private bool _suppressNotification = false;

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (!_suppressNotification) {
                base.OnCollectionChanged(e);
            }
        }

        public new void Clear() {
            _suppressNotification = true;
            base.Clear();
            _suppressNotification = false;
        }

        public void AddRange(IEnumerable<T> list) {
            if (list == null) {
                throw new ArgumentNullException("list");
            }

            _suppressNotification = true;

            foreach (T item in list) {
                Add(item);
            }

            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public class AcquiredImageCollection : RangeAsyncObservableCollection<AcquiredImageVM> { }

    public class AcquiredImageVM : BaseINPC {

        private AcquiredImage acquiredImage;

        public AcquiredImageVM() { }

        public AcquiredImageVM(AcquiredImage acquiredImage) {
            this.acquiredImage = acquiredImage;
            string projectName;
            string targetName;

            NamesItem names = ProjectTargetNameCache.GetNames(acquiredImage.ProjectId, acquiredImage.TargetId);

            if (names == null) {
                SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
                using (var context = database.GetContext()) {
                    Project project = context.ProjectSet.AsNoTracking().Where(p => p.Id == acquiredImage.ProjectId).FirstOrDefault();
                    projectName = project?.Name;

                    Target target = context.TargetSet.AsNoTracking().Where(t => t.Project.Id == acquiredImage.ProjectId && t.Id == acquiredImage.TargetId).FirstOrDefault();
                    targetName = target?.Name;
                }

                ProjectTargetNameCache.PutNames(acquiredImage.ProjectId, acquiredImage.TargetId, projectName, targetName);
            }
            else {
                projectName = names.ProjectName;
                targetName = names.TargetName;
            }

            ProjectName = projectName;
            TargetName = targetName;
        }

        public DateTime AcquiredDate { get { return acquiredImage.AcquiredDate; } }
        public string FilterName { get { return acquiredImage.FilterName; } }
        public string ProjectName { get; private set; }
        public string TargetName { get; private set; }
        public bool Accepted { get { return acquiredImage.Accepted; } }
        public string RejectReason { get { return acquiredImage.RejectReason; } }

        public string FileName { get { return acquiredImage.Metadata.FileName; } }
        public string ExposureDuration { get { return fmt(acquiredImage.Metadata.ExposureDuration); } }

        public string Gain { get { return fmtInt(acquiredImage.Metadata.Gain); } }
        public string Offset { get { return fmtInt(acquiredImage.Metadata.Offset); } }
        public string Binning { get { return acquiredImage.Metadata.Binning; } }

        public string DetectedStars { get { return fmtInt(acquiredImage.Metadata.DetectedStars); } }
        public string HFR { get { return fmt(acquiredImage.Metadata.HFR); } }
        public string HFRStDev { get { return fmt(acquiredImage.Metadata.HFRStDev); } }

        public string ADUStDev { get { return fmt(acquiredImage.Metadata.ADUStDev); } }
        public string ADUMean { get { return fmt(acquiredImage.Metadata.ADUMean); } }
        public string ADUMedian { get { return fmt(acquiredImage.Metadata.ADUMedian); } }
        public string ADUMin { get { return fmtInt(acquiredImage.Metadata.ADUMin); } }
        public string ADUMax { get { return fmtInt(acquiredImage.Metadata.ADUMax); } }

        public string GuidingRMS { get { return fmt(acquiredImage.Metadata.GuidingRMS); } }
        public string GuidingRMSArcSec { get { return fmt(acquiredImage.Metadata.GuidingRMSArcSec); } }
        public string GuidingRMSRA { get { return fmt(acquiredImage.Metadata.GuidingRMSRA); } }
        public string GuidingRMSRAArcSec { get { return fmt(acquiredImage.Metadata.GuidingRMSRAArcSec); } }
        public string GuidingRMSDEC { get { return fmt(acquiredImage.Metadata.GuidingRMSDEC); } }
        public string GuidingRMSDECArcSec { get { return fmt(acquiredImage.Metadata.GuidingRMSDECArcSec); } }

        public string FocuserPosition { get { return fmtInt(acquiredImage.Metadata.FocuserPosition); } }
        public string FocuserTemp { get { return fmt(acquiredImage.Metadata.FocuserTemp); } }
        public string RotatorPosition { get { return fmt(acquiredImage.Metadata.RotatorPosition); } }
        public string PierSide { get { return acquiredImage.Metadata.PierSide; } }
        public string CameraTemp { get { return fmt(acquiredImage.Metadata.CameraTemp); } }
        public string CameraTargetTemp { get { return fmt(acquiredImage.Metadata.CameraTargetTemp); } }
        public string Airmass { get { return fmt(acquiredImage.Metadata.Airmass); } }

        private string fmtInt(int? i) {
            return i == null ? "" : i.ToString();
        }

        private string fmt(double d) {
            return fmt(d, "{0:0.####}");
        }

        private string fmt(double d, string format) {
            return Double.IsNaN(d) ? "" : String.Format(format, d);
        }
    }

    class ProjectTargetNameCache {

        private static readonly TimeSpan ITEM_TIMEOUT = TimeSpan.FromHours(12);
        private static readonly MemoryCache _cache = new MemoryCache("Scheduler AcquiredImages Names");

        public static NamesItem GetNames(int projectId, int targetId) {
            return (NamesItem)_cache.Get(GetCacheKey(projectId, targetId));
        }

        public static void PutNames(int projectId, int targetId, string projectName, string targetName) {
            _cache.Add(GetCacheKey(projectId, targetId), new NamesItem(projectName, targetName), DateTime.Now.Add(ITEM_TIMEOUT));
        }

        private static string GetCacheKey(int projectId, int targetId) {
            return $"{projectId}-{targetId}";
        }

        private ProjectTargetNameCache() { }
    }

    class NamesItem {
        public string ProjectName;
        public string TargetName;

        public NamesItem(string projectName, string targetName) {
            ProjectName = projectName;
            TargetName = targetName;
        }
    }
}
