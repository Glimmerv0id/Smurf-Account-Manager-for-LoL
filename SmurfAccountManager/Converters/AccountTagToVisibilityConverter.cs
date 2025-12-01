using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Converters
{
    public class AccountTagToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AccountTag tag && tag != AccountTag.None)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
