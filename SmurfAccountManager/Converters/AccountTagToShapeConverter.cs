using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using SmurfAccountManager.Models;

namespace SmurfAccountManager.Converters
{
    public class AccountTagToShapeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AccountTag tag && tag != AccountTag.None)
            {
                switch (tag)
                {
                    case AccountTag.YellowStar:
                        return new Polygon
                        {
                            Points = new PointCollection 
                            { 
                                new Point(10, 0), new Point(12, 7), new Point(20, 7), 
                                new Point(14, 12), new Point(16, 20), new Point(10, 15), 
                                new Point(4, 20), new Point(6, 12), new Point(0, 7), new Point(8, 7) 
                            },
                            Fill = new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                            Width = 16,
                            Height = 16,
                            Stretch = Stretch.Uniform
                        };
                    
                    case AccountTag.RedCircle:
                        return new Ellipse
                        {
                            Width = 14,
                            Height = 14,
                            Fill = new SolidColorBrush(Color.FromRgb(255, 69, 58))
                        };
                    
                    case AccountTag.GreenCircle:
                        return new Ellipse
                        {
                            Width = 14,
                            Height = 14,
                            Fill = new SolidColorBrush(Color.FromRgb(52, 199, 89))
                        };
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
