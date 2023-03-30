using Assistant.NINAPlugin.Database;
using Assistant.NINAPlugin.Database.Schema;
using LinqKit;
using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
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

        private DateTime fromDate = DateTime.MinValue;
        public DateTime FromDate {
            get => fromDate;
            set {
                fromDate = value.Date;
                RaisePropertyChanged(nameof(FromDate));
                _ = LoadRecords();
            }
        }

        private DateTime toDate = DateTime.MinValue;
        public DateTime ToDate {
            get => toDate;
            set {
                toDate = value.AddDays(1).Date.AddSeconds(-1);
                RaisePropertyChanged(nameof(ToDate));
                _ = LoadRecords();
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
                RaisePropertyChanged(nameof(SelectedTargetId));
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

        private async Task<bool> LoadRecords() {

            if (AcquiredImageCollection == null || FromDate == DateTime.MinValue || ToDate == DateTime.MinValue) {
                return true;
            }

            string newSearchCriteraKey = GetSearchCriteraKey();
            if (newSearchCriteraKey == SearchCriteraKey) {
                return true;
            }

            await _dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {

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

                List<AcquiredImage> acquiredImages;
                using (var context = database.GetContext()) {
                    acquiredImages = context.AcquiredImageSet.AsNoTracking().AsExpandable().Where(predicate).ToList();
                }

                AcquiredImageCollection.Clear();
                acquiredImages.ForEach(a => { AcquiredImageCollection.Add(new AcquiredImageVM(a)); });

                RaisePropertyChanged(nameof(AcquiredImageCollection));
                RaisePropertyChanged(nameof(ItemsView));
            }));

            return true;
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

    public class AcquiredImageCollection : ObservableCollection<AcquiredImageVM> { }

    public class AcquiredImageVM : BaseINPC {

        private AcquiredImage acquiredImage;

        public AcquiredImageVM() { }

        public AcquiredImageVM(AcquiredImage acquiredImage) {
            this.acquiredImage = acquiredImage;

            SchedulerDatabaseInteraction database = new SchedulerDatabaseInteraction();
            using (var context = database.GetContext()) {
                Project project = context.ProjectSet.AsNoTracking().Where(p => p.Id == acquiredImage.ProjectId).FirstOrDefault();
                ProjectName = project?.Name;

                Target target = context.TargetSet.AsNoTracking().Where(t => t.Project.Id == acquiredImage.ProjectId && t.Id == acquiredImage.TargetId).FirstOrDefault();
                TargetName = target?.Name;
            }
        }

        public DateTime AcquiredDate { get { return acquiredImage.AcquiredDate; } }
        public string FilterName { get { return acquiredImage.FilterName; } }
        public string ProjectName { get; private set; }
        public string TargetName { get; private set; }
        public bool Accepted { get { return acquiredImage.Accepted; } }

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
}
