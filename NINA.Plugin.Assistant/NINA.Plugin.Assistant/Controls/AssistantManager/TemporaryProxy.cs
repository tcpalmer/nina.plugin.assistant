using Assistant.NINAPlugin.Database.Schema;
using NINA.Core.Utility;
using System.Collections.Generic;
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
        public virtual TEntity CopyEntity<TEntity>(TEntity source) where TEntity : class, new() {

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

        public override TEntity CopyEntity<TEntity>(TEntity sourceEntity) {
            TEntity copyEntity = base.CopyEntity(sourceEntity);
            Project source = sourceEntity as Project;
            Project copy = copyEntity as Project;

            // Deepen the copy for the RuleWeights list
            copy.RuleWeights = new List<RuleWeight>(source.RuleWeights.Count);
            source.RuleWeights.ForEach(rw => {
                rw.PropertyChanged -= ProxyPropertyChanged;
                RuleWeight copyRuleWeight = base.CopyEntity(rw);
                copyRuleWeight.PropertyChanged += ProxyPropertyChanged;
                copy.RuleWeights.Add(copyRuleWeight);
            });

            return copyEntity;
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

        public override TEntity CopyEntity<TEntity>(TEntity sourceEntity) {
            TEntity copyEntity = base.CopyEntity(sourceEntity);
            Target source = sourceEntity as Target;
            Target copy = copyEntity as Target;

            // Deepen the copy for the ExposurePlan list
            copy.ExposurePlans = new List<ExposurePlan>(source.ExposurePlans.Count);
            source.ExposurePlans.ForEach(plan => {
                plan.PropertyChanged -= ProxyPropertyChanged;
                ExposurePlan copyExposurePlan = base.CopyEntity(plan);
                copyExposurePlan.ExposureTemplate = plan.ExposureTemplate;
                copyExposurePlan.PropertyChanged += ProxyPropertyChanged;
                copy.ExposurePlans.Add(copyExposurePlan);
            });

            return copyEntity;
        }
    }

    public class ExposureTemplateProxy : TemporaryProxy<ExposureTemplate> {

        public ExposureTemplateProxy(ExposureTemplate exposureTemplate) : base(exposureTemplate) { }

        public ExposureTemplate ExposureTemplate {
            get => Proxy;
            set {
                Proxy = value;
            }
        }
    }

}
