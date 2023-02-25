using System;
using System.Globalization;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.Converters {

    public class AltitudeChoicesConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double d = (double)value;
            return $"{d}°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            string s = (string)value;
            return double.Parse(s.TrimEnd('°'));
        }
    }
}
