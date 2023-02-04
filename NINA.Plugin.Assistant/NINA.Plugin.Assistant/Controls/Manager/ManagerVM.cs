using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Assistant.NINAPlugin.Controls.Manager {

    public class ManagerVM : INotifyPropertyChanged {

        private List<ITreeDataItem> _profiles;
        public List<ITreeDataItem> Profiles {
            get { return _profiles; }
            set {
                _profiles = value;
                RaisePropertyChanged(nameof(Profiles));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
