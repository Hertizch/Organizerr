﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Organizerr.Converters
{
    public class ImageSourceConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null) return null;

            // if no image object, return 'null' poster
            if (values[0] == DependencyProperty.UnsetValue) return new Uri($"{(string)values[1]}/Content/Images/poster-dark.png", UriKind.Absolute);

            var images = (List<RadarrSharp.Models.Image>)values[0];

            return new Uri($"{(string)values[1]}{images.FirstOrDefault(x => x.CoverType == RadarrSharp.Enums.CoverType.Poster).Url}", UriKind.Absolute);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}