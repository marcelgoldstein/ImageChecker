using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ImageChecker.Converter;

public class NullReplaceImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BitmapImage)
        {
            return value;
        }
        else
        {
            return new BitmapImage(new Uri("/ImageChecker;component/Images/noImage.jpg", UriKind.Relative));
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
