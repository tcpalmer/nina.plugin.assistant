using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System;
using System.ComponentModel;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TemporaryProxy<T> : BaseINPC where T : INotifyPropertyChanged, ICloneable {

        private T original;
        private T proxy;

        public TemporaryProxy(T original) {
            Original = original;
            Proxy = (T)original.Clone();
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

        public void OnCancel() {
            Proxy.PropertyChanged -= ProxyPropertyChanged;
            Proxy = (T)Original.Clone();
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        public void OnSave() {
            Proxy.PropertyChanged -= ProxyPropertyChanged;
            Original = Proxy;
            Proxy = (T)Original.Clone();
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }
    }

    public class ProjectProxy : TemporaryProxy<Project> {

        public ProjectProxy(Project project) : base(project) {
        }

        public Project Project {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

    public class TargetProxy : TemporaryProxy<Target> {

        public TargetProxy(Target target) : base(target) {
        }

        public Target Target {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

    public class FilterPlanProxy : TemporaryProxy<FilterPlan> {

        public FilterPlanProxy(FilterPlan filterPlan) : base(filterPlan) {
        }

        public FilterPlan FilterPlan {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

}
