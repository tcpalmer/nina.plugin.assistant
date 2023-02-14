using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Assistant.NINAPlugin.Controls.ValidationRules {

    public class RequiredFieldValidationRule : ValidationRule {

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            if (value.ToString().Length > 0) {
                return new ValidationResult(true, null);
            }
            else {
                return new ValidationResult(false, "Required");
            }
        }
    }

    public class DoubleMinimumRule : ValidationRule {

        private MinimumValue _minimumValue;

        public MinimumValue MinimumValue {
            get { return _minimumValue; }
            set { _minimumValue = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double parameter = 0;

            try {
                if (("" + value).Length > 0) {
                    parameter = double.Parse("" + value);
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((parameter < MinimumValue.MinimumVal)) {
                return new ValidationResult(false,
                    "Please enter a value >= " + MinimumValue.MinimumVal);
            }

            return new ValidationResult(true, null);
        }
    }

    public class MinimumValue : DependencyObject {

        public double MinimumVal {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("MinimumVal", typeof(double), typeof(DoubleRangeChecker), new UIPropertyMetadata(double.MinValue));
    }

    public class DateRangeRule : ValidationRule {
        private DateCheck _dateCheck;

        public DateCheck ValidRange {
            get { return _dateCheck; }
            set { _dateCheck = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {

            if (value == null) {
                return new ValidationResult(false, "bad date for date validation");
            }

            DateTime parameter = (DateTime)value;

            if (ValidRange.CheckBefore && parameter > ValidRange.CheckDate) {
                return new ValidationResult(false, $"date must be before {ValidRange.CheckDate}");
            }

            if (!ValidRange.CheckBefore && parameter <= ValidRange.CheckDate) {
                return new ValidationResult(false, $"date must be after {ValidRange.CheckDate}");
            }

            return new ValidationResult(true, null);
        }
    }

    public class DateCheck : DependencyObject {

        public bool CheckBefore {
            get { return (bool)GetValue(CheckAfterProperty); }
            set { SetValue(CheckAfterProperty, value); }
        }

        public static readonly DependencyProperty CheckAfterProperty =
            DependencyProperty.Register("CheckBefore", typeof(bool), typeof(DateCheck), new UIPropertyMetadata(true));

        public DateTime CheckDate {
            get { return (DateTime)GetValue(CheckDateProperty); }
            set { SetValue(CheckDateProperty, value); }
        }

        public static readonly DependencyProperty CheckDateProperty =
            DependencyProperty.Register("CheckDate", typeof(DateTime), typeof(DateCheck), new UIPropertyMetadata(DateTime.MaxValue));
    }

    public class IntRangeRule : ValidationRule {
        private IntRangeChecker _validRange;

        public IntRangeChecker ValidRange {
            get { return _validRange; }
            set { _validRange = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            int parameter = 0;

            try {
                if (("" + value).Length > 0) {
                    parameter = int.Parse("" + value);
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((parameter < ValidRange.Minimum) || (parameter > ValidRange.Maximum)) {
                return new ValidationResult(false,
                    "Please enter value in the range: " + ValidRange.Minimum + " - " + ValidRange.Maximum + ".");
            }

            return new ValidationResult(true, null);
        }
    }

    public class IntRangeChecker : DependencyObject {

        public int Minimum {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(IntRangeChecker), new UIPropertyMetadata(int.MinValue));

        public int Maximum {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(IntRangeChecker), new UIPropertyMetadata(int.MaxValue));
    }

    public class DoubleRangeRule : ValidationRule {
        private DoubleRangeChecker _validRange;

        public DoubleRangeChecker ValidRange {
            get { return _validRange; }
            set { _validRange = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
            double parameter = 0;

            try {
                if (("" + value).Length > 0) {
                    parameter = double.Parse("" + value);
                }
            }
            catch (Exception e) {
                return new ValidationResult(false, "Illegal characters or " + e.Message);
            }

            if ((parameter < ValidRange.Minimum) || (parameter > ValidRange.Maximum)) {
                return new ValidationResult(false,
                    "Please enter value in the range: " + ValidRange.Minimum + " - " + ValidRange.Maximum + ".");
            }

            return new ValidationResult(true, null);
        }
    }

    public class DoubleRangeChecker : DependencyObject {

        public double Minimum {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(DoubleRangeChecker), new UIPropertyMetadata(double.MinValue));

        public double Maximum {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(DoubleRangeChecker), new UIPropertyMetadata(double.MaxValue));
    }

}
