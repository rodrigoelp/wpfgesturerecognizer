using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SampleApp
{
    public class BooleanToScrollBarVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var enabled = bool.Parse(value.ToString());
            return enabled ? ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var visibility = (ScrollBarVisibility) value;
            return visibility == ScrollBarVisibility.Visible;
        }
    }
}