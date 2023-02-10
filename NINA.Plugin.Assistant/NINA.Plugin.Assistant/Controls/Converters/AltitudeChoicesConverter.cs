using NINA.Core.Utility;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.Converters {

    public class AltitudeChoicesConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Logger.Info($"CONVERT: V={value} VT={value.GetType()}");
            double d = (double)value;
            return $"{d}°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            Logger.Info($"CONVERT BACK: V={value} VT={value.GetType()}");
            string s = (string)value;
            return double.Parse(s.TrimEnd('°'));
        }
    }
}
