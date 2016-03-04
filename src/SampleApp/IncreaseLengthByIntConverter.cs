using System;
using System.Globalization;
using System.Windows.Data;

namespace SampleApp
{
    public class IncreaseLengthByIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            int increaseBy;
            int.TryParse(parameter.ToString(), out increaseBy);
            return (double) value + increaseBy;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}