using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ImageChecker.Converter;

public class SizeCompareBorderVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        // 0 => IsExterminationModeActive
        // 1 => Bitmap
        if (values.Length == 2 && values[0] is bool b && b && values[1] is BitmapImage)
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
