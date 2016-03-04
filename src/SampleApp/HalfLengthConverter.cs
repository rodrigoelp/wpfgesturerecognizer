using System;
using System.Globalization;
using System.Windows.Data;

namespace SampleApp
{
    public class HalfLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (double) value/2.0d;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}