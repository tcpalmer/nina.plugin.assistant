using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    public class TemporaryProxy<T> : BaseINPC where T : class, INotifyPropertyChanged, new() {

        private T original;
        private T proxy;

        public TemporaryProxy(T original) {
            Original = original;
            Proxy = CopyEntity(original);
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        public void ProxyPropertyChanged(object sender, PropertyChangedEventArgs e) {
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
            Proxy = CopyEntity(Original);
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        public void OnSave() {
            Proxy.PropertyChanged -= ProxyPropertyChanged;
            Original = Proxy;
            Proxy = CopyEntity(Original);
            Proxy.PropertyChanged += ProxyPropertyChanged;
        }

        /// <summary>
        /// Create a shallow copy of an entity, skipping all the EF baggage.
        /// See https://stackoverflow.com/questions/12315233/entityframework-entity-proxy-error
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="source">The source entity</param>
        TEntity CopyEntity<TEntity>(TEntity source) where TEntity : class, new() {

            // Get properties from EF that are read/write and not marked with NotMappedAttribute
            var sourceProperties = typeof(TEntity)
                                    .GetProperties()
                                    .Where(p => p.CanRead && p.CanWrite &&
                                                p.GetCustomAttributes(typeof(NotMappedAttribute), true).Length == 0);
            var newObj = new TEntity();

            foreach (var property in sourceProperties) {
                property.SetValue(newObj, property.GetValue(source, null), null);
            }

            return newObj;
        }
    }

    public class ProjectProxy : TemporaryProxy<Project> {

        public ProjectProxy(Project project) : base(project) { }

        public Project Project {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

    public class TargetProxy : TemporaryProxy<Target> {

        public TargetProxy(Target target) : base(target) { }

        public Target Target {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

    public class FilterPlanProxy : TemporaryProxy<FilterPlan> {

        public FilterPlanProxy(FilterPlan filterPlan) : base(filterPlan) { }

        public FilterPlan FilterPlan {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

}
