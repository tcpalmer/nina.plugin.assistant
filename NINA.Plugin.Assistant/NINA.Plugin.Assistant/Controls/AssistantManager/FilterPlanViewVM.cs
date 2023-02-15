using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class FilterPlanViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private FilterPlanProxy filterPlanProxy;

        public FilterPlanProxy FilterPlanProxy {
            get => filterPlanProxy;
            set {
                filterPlanProxy = value;
                RaisePropertyChanged(nameof(FilterPlanProxy));
            }
        }

        public FilterPlanViewVM(AssistantManagerVM managerVM, IProfileService profileService, FilterPlan filterPlan) : base(profileService) {
            this.managerVM = managerVM;
            FilterPlanProxy = new FilterPlanProxy(filterPlan);

            InitializeCombos();
            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void FilterPlanProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(FilterPlanProxy.Proxy)) {
                FilterPlanChanged = true;
            }
            else {
                RaisePropertyChanged(nameof(FilterPlanProxy));
            }
        }

        private void InitializeCombos() {
            BinningModeChoices = new List<BinningMode> {
                    new BinningMode(1,1),
                    new BinningMode(2,2),
                    new BinningMode(3,3),
                    new BinningMode(4,4),
            };
        }

        private List<BinningMode> _binningModeChoices;
        public List<BinningMode> BinningModeChoices {
            get => _binningModeChoices;
            set {
                _binningModeChoices = value;
                RaisePropertyChanged(nameof(BinningModeChoices));
            }
        }

        private bool showFilterPlanEditView = false;
        public bool ShowFilterPlanEditView {
            get => showFilterPlanEditView;
            set {
                showFilterPlanEditView = value;
                RaisePropertyChanged(nameof(ShowFilterPlanEditView));
            }
        }

        private bool filterPlanChanged = false;
        public bool FilterPlanChanged {
            get => filterPlanChanged;
            set {
                filterPlanChanged = value;
                RaisePropertyChanged(nameof(FilterPlanChanged));
            }
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void Edit(object obj) {
            FilterPlanProxy.PropertyChanged += FilterPlanProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowFilterPlanEditView = true;
            FilterPlanChanged = false;
        }

        private void Save(object obj) {
            managerVM.SaveFilterPlan(FilterPlanProxy.Proxy);
            FilterPlanProxy.OnSave();
            FilterPlanProxy.PropertyChanged -= FilterPlanProxy_PropertyChanged;
            ShowFilterPlanEditView = false;
            managerVM.SetEditMode(false);
        }

        private void Cancel(object obj) {
            FilterPlanProxy.OnCancel();
            FilterPlanProxy.PropertyChanged -= FilterPlanProxy_PropertyChanged;
            ShowFilterPlanEditView = false;
            managerVM.SetEditMode(false);
        }

    }
}
