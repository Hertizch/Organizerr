using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Organizerr.Converters
{
    public class IsCutoffMetToBooleanConverter : IMultiValueConverter
    {
        private long cutoffQualityId;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return false;
            if (values.Length < 3) return false;
            if (values[1] == DependencyProperty.UnsetValue) return false;

            // Movie profile ID
            long profileId = (long)values[0];

            // MovieFile quality ID
            long currentQualityId = (long)values[1];

            // Profiles list
            var profiles = (List<RadarrSharp.Models.Profile>)values[2];

            // If profileId equals profile id in profiles list, get cutoff id
            cutoffQualityId = profiles.FirstOrDefault(x => x.Id == profileId).Cutoff.Id;

            // Return match
            return currentQualityId == cutoffQualityId;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
