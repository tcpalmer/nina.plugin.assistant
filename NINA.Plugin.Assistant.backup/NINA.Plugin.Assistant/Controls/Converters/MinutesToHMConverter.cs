using Assistant.NINAPlugin.Util;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.Converters {

    public class MinutesToHMConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return Utils.MtoHM((int)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
