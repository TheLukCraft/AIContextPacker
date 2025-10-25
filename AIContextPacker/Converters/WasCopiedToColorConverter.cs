using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AIContextPacker.Converters;

public class WasCopiedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool wasCopied && wasCopied)
        {
            return new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange color
        }
        
        return new SolidColorBrush(Color.FromRgb(63, 81, 181)); // Material Design Indigo
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
