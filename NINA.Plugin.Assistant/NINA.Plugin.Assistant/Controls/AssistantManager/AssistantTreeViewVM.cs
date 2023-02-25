using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class AssistantTreeViewVM : BaseVM {

        public AssistantTreeViewVM(AssistantManagerVM managerVM, IProfileService profileService, string name, List<TreeDataItem> rootList, int height) : base(profileService) {
            ParentVM = managerVM;
            RootList = rootList;
            Name = name;
            Height = height;

            ExpandAllCommand = new RelayCommand(ExpandAll);
            CollapseAllCommand = new RelayCommand(CollapseAll);
        }

        public AssistantManagerVM ParentVM { get; private set; }

        private List<TreeDataItem> rootList;
        public List<TreeDataItem> RootList { get => rootList; set => rootList = value; }

        public string Name { get; private set; }
        public int Height { get; private set; }

        public ICommand ExpandAllCommand { get; private set; }
        public ICommand CollapseAllCommand { get; private set; }

        private void ExpandAll(object obj) {
            TreeDataItem.VisitAll(RootList[0], i => { i.IsExpanded = true; });
        }

        private void CollapseAll(object obj) {
            TreeDataItem.VisitAll(RootList[0], i => { i.IsExpanded = false; });
        }
    }
}
