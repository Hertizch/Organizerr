using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Organizerr.Converters
{
    public class ExistsInCollectionToBooleanConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return false;

            var tmdbId = (long)values[0];
            var collection = (ObservableCollection<RadarrSharp.Models.Movie>)values[1];

            if (collection == null) return false;
            if (collection.Count == 0) return false;

            //Debug.WriteLine($"ExistsInCollectionToBooleanConverter: tmdbId: {tmdbId} (collection: {collection.Count})");

            // if exists, return false
            if (collection.Any(x => x.TmdbId == tmdbId))
            {
                //Debug.WriteLine($"ExistsInCollectionToBooleanConverter: {tmdbId}: false");
                return false;
            }
                

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
