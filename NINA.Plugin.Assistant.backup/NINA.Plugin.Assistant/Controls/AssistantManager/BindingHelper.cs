using System.Windows;

namespace Assistant.NINAPlugin.Controls.AssistantManager {

    /// <summary>
    /// This is needed to be able to bind a VM property to the parameter of a validation rule (like date to DateCheck for
    /// DateRangeRule).
    /// 
    /// See https://social.technet.microsoft.com/wiki/contents/articles/31422.wpf-passing-a-data-bound-value-to-a-validation-rule.aspx
    /// </summary>
    public class BindingHelper : Freezable {

        protected override Freezable CreateInstanceCore() {
            return new BindingHelper();
        }

        public object Data {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingHelper), new PropertyMetadata(null));
    }
}
