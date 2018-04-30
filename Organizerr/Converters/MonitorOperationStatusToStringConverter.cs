using System;
using System.Globalization;
using System.Windows.Data;

namespace Organizerr.Converters
{
    public class MonitorOperationStatusToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return null;

            var current = (int)values[0];
            var total = (int)values[1];

            return $"Setting {current} of {total} movies as unmonitored";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
