using CommunityToolkit.Mvvm.Input;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class OverrideExposureOrderViewVM : BaseVM {

        private static object lockObj = new object();
        private TargetViewVM targetViewVM;

        public OverrideExposureOrderViewVM(TargetViewVM targetViewVM, IProfileService profileService) : base(profileService) {
            this.targetViewVM = targetViewVM;

            MoveItemUpCommand = new RelayCommand<object>(MoveItemUp);
            MoveItemDownCommand = new RelayCommand<object>(MoveItemDown);
            CopyItemCommand = new RelayCommand<object>(CopyItem);
            DeleteItemCommand = new RelayCommand<object>(DeleteItem);
            InsertDitherCommand = new RelayCommand<object>(InsertDither);

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        public ICommand MoveItemUpCommand { get; private set; }
        public ICommand MoveItemDownCommand { get; private set; }
        public ICommand CopyItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }
        public ICommand InsertDitherCommand { get; private set; }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private OverrideExposureOrder overrideExposureOrder;
        public OverrideExposureOrder OverrideExposureOrder {
            get => overrideExposureOrder;
            set {
                overrideExposureOrder = value;
                DisplayOverrideExposureOrder = overrideExposureOrder.GetDisplayList();
                RaisePropertyChanged(nameof(OverrideExposureOrder));
            }
        }

        private ObservableCollection<OverrideItem> displayOverrideExposureOrder;
        public ObservableCollection<OverrideItem> DisplayOverrideExposureOrder {
            get => displayOverrideExposureOrder;
            set {
                displayOverrideExposureOrder = value;
                RaisePropertyChanged(nameof(DisplayOverrideExposureOrder));
            }
        }

        private void MoveItemUp(object obj) {
            OverrideItem item = obj as OverrideItem;
            lock (lockObj) {
                int idx = GetItemIndex(item);
                OverrideExposureOrder.OverrideItems.RemoveAt(idx);

                if (idx == 0) {
                    OverrideExposureOrder.OverrideItems.Add(item);
                }
                else {
                    OverrideExposureOrder.OverrideItems.Insert(idx - 1, item);
                }

                DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
            }
        }

        private void MoveItemDown(object obj) {
            OverrideItem item = obj as OverrideItem;
            lock (lockObj) {
                int idx = GetItemIndex(item);

                if (idx == OverrideExposureOrder.OverrideItems.Count - 1) {
                    OverrideExposureOrder.OverrideItems.RemoveAt(idx);
                    OverrideExposureOrder.OverrideItems.Insert(0, item);
                }
                else {
                    OverrideExposureOrder.OverrideItems.RemoveAt(idx);
                    OverrideExposureOrder.OverrideItems.Insert(idx + 1, item);
                }

                DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
            }
        }

        private void CopyItem(object obj) {
            OverrideItem item = obj as OverrideItem;
            lock (lockObj) {
                OverrideExposureOrder.OverrideItems.Insert(GetItemIndex(item) + 1, item.Clone());
                DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
            }
        }

        private void DeleteItem(object obj) {
            OverrideItem item = obj as OverrideItem;
            lock (lockObj) {
                OverrideExposureOrder.OverrideItems.RemoveAt(GetItemIndex(item));
                DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
            }
        }

        private void InsertDither(object obj) {
            if (obj != null) {
                OverrideItem current = obj as OverrideItem;
                lock (lockObj) {
                    OverrideExposureOrder.OverrideItems.Insert(GetItemIndex(current) + 1, new OverrideItem());
                    DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
                }
            }
            else {
                lock (lockObj) {
                    OverrideExposureOrder.OverrideItems.Add(new OverrideItem());
                    DisplayOverrideExposureOrder = OverrideExposureOrder.GetDisplayList();
                }
            }
        }

        private int GetItemIndex(OverrideItem find) {
            return OverrideExposureOrder.OverrideItems.IndexOf(find);
        }

        private void Save() {
            targetViewVM.SaveOverrideExposureOrder(OverrideExposureOrder);
            targetViewVM.ShowOverrideExposureOrderPopup = false;
        }

        private void Cancel() {
            targetViewVM.ShowOverrideExposureOrderPopup = false;
        }

    }
}
