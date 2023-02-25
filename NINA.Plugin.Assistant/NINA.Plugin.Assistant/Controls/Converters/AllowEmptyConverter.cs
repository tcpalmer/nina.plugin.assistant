using System;
using System.Globalization;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.Converters {

    public class AllowEmptyConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? "" : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (string.IsNullOrWhiteSpace((string)value)) {
                return null;
            }

            int result;
            if (int.TryParse((string)value, out result)) {
                return result;
            }
            else {
                return null;
            }

        }
    }
}
