using Assistant.NINAPlugin.Database.Schema;
using System;
using System.Globalization;
using System.Windows.Data;

namespace Assistant.NINAPlugin.Controls.Converters {

    public class FlatsHandlingChoicesConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            int val = (int)value;

            switch (val) {
                case Project.FLATS_HANDLING_OFF: return "Off";
                case Project.FLATS_HANDLING_TARGET_COMPLETION: return "Target Completion";
                case Project.FLATS_HANDLING_IMMEDIATE: return "Use With Immediate";
                default: return $"{val}";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return Project.FLATS_HANDLING_OFF;
            }

            string val = (string)value;
            switch (val) {
                case "Off": return Project.FLATS_HANDLING_OFF;
                case "Target Completion": return Project.FLATS_HANDLING_TARGET_COMPLETION;
                case "Use With Immediate": return Project.FLATS_HANDLING_IMMEDIATE;
                default: return int.Parse(val);
            }
        }
    }
}