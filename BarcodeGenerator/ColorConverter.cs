using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace BarcodeGenerator
{
    [ValueConversion(typeof(Brush), typeof(bool))]
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
     object parameter, CultureInfo culture)
        {
            bool bEnabled = (bool)value;
            return bEnabled ? Brushes.LightGreen : Brushes.LightYellow;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
