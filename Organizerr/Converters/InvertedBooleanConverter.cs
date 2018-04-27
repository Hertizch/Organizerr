using System;
using System.Globalization;
using System.Windows.Data;

namespace Organizerr.Converters
{
    public class InvertedBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            // if value is true, return false and vice versa
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
