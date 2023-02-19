using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;


namespace Assistant.NINAPlugin.Controls.Converters {

    public class StatusConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool status = (bool)value;
            return status ? (GeometryGroup)Application.Current.Resources["CheckMarkSVG"] :
                            (GeometryGroup)Application.Current.Resources["XMarkSVG"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class StatusMarkColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool status = (bool)value;
            return status ? "Green" : "Crimson";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
