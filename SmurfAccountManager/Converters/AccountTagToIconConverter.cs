using System;
using System.Globalization;
using System.Windows.Data;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Converters
{
    public class AccountTagToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AccountTag tag)
            {
                return tag switch
                {
                    AccountTag.YellowStar => "⭐",
                    AccountTag.RedCircle => "🔴",
                    AccountTag.GreenCircle => "🟢",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
