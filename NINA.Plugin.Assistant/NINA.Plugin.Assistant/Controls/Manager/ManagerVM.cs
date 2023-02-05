using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Assistant.NINAPlugin.Controls.Manager {

    public class ManagerVM : INotifyPropertyChanged {

        private List<TreeDataItem> _profiles;
        public List<TreeDataItem> Profiles {
            get { return _profiles; }
            set {
                _profiles = value;
                RaisePropertyChanged(nameof(Profiles));
            }
        }

        /*
        private ContextMenu GetProfileContextMenu() {
            var contextMenu = new ContextMenu();
            contextMenu.Items.Add(new MenuItem() { Header = "New Project" });
            contextMenu.Items.Add(new MenuItem() { Header = "Paste Project" });
            return contextMenu;
        }*/

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /* TODO: tree item context menus
         * 
         * Profile:
         *  - New Project
         *  - Paste Project (disabled unless a Project is in the clipboard)
         * 
         * Project:
         *  - New Target
         *  - Paste Target (disabled unless a Target is in the clipboard)
         *  - --
         *  - Copy Project
         *  - Delete Project
         * 
         * Target:
         *  - New Filter Plan
         *  - Paste Filter Plan (disabled unless a Filter Plan is in the clipboard)
         *  - --
         *  - Copy Target
         *  - Delete Target
         * 
         * Filter Plan:
         *  - Copy Filter Plan
         *  - Delete Filter Plan
         * 
         */
    }
}
