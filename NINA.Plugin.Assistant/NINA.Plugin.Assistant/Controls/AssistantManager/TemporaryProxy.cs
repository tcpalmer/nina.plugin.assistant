using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System;
using System.ComponentModel;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TemporaryProxy<T> : BaseINPC where T : INotifyPropertyChanged, ICloneable {

        private T original;
        private T proxy;

        public TemporaryProxy(T original) {
            this.original = original;
            this.Proxy = (T)original.Clone();
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        private void ProxyPropertyChanged(object sender, PropertyChangedEventArgs e) {
            RaisePropertyChanged(e.PropertyName);
        }

        public T Original {
            get => original;
            set {
                original = value;
            }
        }

        public T Proxy {
            get => proxy;
            set {
                proxy = value;
                RaisePropertyChanged(nameof(Proxy));
            }
        }

        public void RestoreOnEditCancel() {
            Proxy.PropertyChanged -= ProxyPropertyChanged;
            Proxy = (T)original.Clone();
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        public T GetOriginalObject() {
            return original;
        }

        public T GetEditedObject() {
            return Proxy;
        }
    }

    public class ProjectProxy : TemporaryProxy<Project> {

        public ProjectProxy(Project project) : base(project) {
        }

        public Project Project {
            get => Proxy;
            set {
                Proxy = value;
                //RaisePropertyChanged(nameof(Proxy));
            }
        }
    }
}
