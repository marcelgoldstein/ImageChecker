using System.IO;
using System.Windows.Media.Imaging;

namespace ImageChecker.Helper;

public static class BitmapSourceToFile
{
    public static void SaveToFile(this BitmapSource image, string filepath)
    {
        if (image != null)
        {
            switch (Path.GetExtension(filepath))
            {
                case ".bmp":
                    image.SaveAsBmp(filepath);
                    break;
                case ".png":
                    image.SaveAsPng(filepath);
                    break;
                case ".jpg":
                case ".jpeg":
                case ".jfif":
                    image.SaveAsJpeg(filepath);
                    break;
                case ".tif":
                case "tiff":
                    image.SaveAsTiff(filepath);
                    break;
                default:
                    break;
            }
        }
    }

    public static void SaveAsBmp(this BitmapSource image, string filepath)
    {
        using var fileStream = new FileStream(filepath, FileMode.Create);
        var encoder = new BmpBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }

    public static void SaveAsPng(this BitmapSource image, string filepath)
    {
        using var fileStream = new FileStream(filepath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }

    public static void SaveAsJpeg(this BitmapSource image, string filepath)
    {
        using var fileStream = new FileStream(filepath, FileMode.Create);
        var encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }

    public static void SaveAsTiff(this BitmapSource image, string filepath)
    {
        using var fileStream = new FileStream(filepath, FileMode.Create);
        var encoder = new TiffBitmapEncoder
        {
            Compression = TiffCompressOption.Zip
        };
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);
    }
}
