using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class ExposurePlanViewVM : BaseVM {

        private AssistantManagerVM managerVM;
        private ExposurePlanProxy exposurePlanProxy;

        public ExposurePlanProxy ExposurePlanProxy {
            get => exposurePlanProxy;
            set {
                exposurePlanProxy = value;
                RaisePropertyChanged(nameof(ExposurePlanProxy));
            }
        }

        public ExposurePlanViewVM(AssistantManagerVM managerVM, IProfileService profileService, ExposurePlan exposurePlan) : base(profileService) {
            this.managerVM = managerVM;
            ExposurePlanProxy = new ExposurePlanProxy(exposurePlan);

            InitializeCombos();
            EditCommand = new RelayCommand(Edit);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void ExposurePlanProxy_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e?.PropertyName != nameof(ExposurePlanProxy.Proxy)) {
                ExposurePlanChanged = true;
            }
            else {
                RaisePropertyChanged(nameof(ExposurePlanProxy));
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

        private bool showExposurePlanEditView = false;
        public bool ShowExposurePlanEditView {
            get => showExposurePlanEditView;
            set {
                showExposurePlanEditView = value;
                RaisePropertyChanged(nameof(ShowExposurePlanEditView));
            }
        }

        private bool exposurePlanChanged = false;
        public bool ExposurePlanChanged {
            get => exposurePlanChanged;
            set {
                exposurePlanChanged = value;
                RaisePropertyChanged(nameof(ExposurePlanChanged));
            }
        }

        public ICommand EditCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void Edit(object obj) {
            Logger.Warning("ExposurePlan VM actions disabled");
            /*
            ExposurePlanProxy.PropertyChanged += ExposurePlanProxy_PropertyChanged;
            managerVM.SetEditMode(true);
            ShowExposurePlanEditView = true;
            ExposurePlanChanged = false;
            */
        }

        private void Save(object obj) {
            Logger.Warning("ExposurePlan VM actions disabled");
            /*
            managerVM.SaveExposurePlan(ExposurePlanProxy.Proxy);
            ExposurePlanProxy.OnSave();
            ExposurePlanProxy.PropertyChanged -= ExposurePlanProxy_PropertyChanged;
            ShowExposurePlanEditView = false;
            managerVM.SetEditMode(false);
            */
        }

        private void Cancel(object obj) {
            Logger.Warning("ExposurePlan VM actions disabled");
            /*
            ExposurePlanProxy.OnCancel();
            ExposurePlanProxy.PropertyChanged -= ExposurePlanProxy_PropertyChanged;
            ShowExposurePlanEditView = false;
            managerVM.SetEditMode(false);
            */
        }

    }
}
