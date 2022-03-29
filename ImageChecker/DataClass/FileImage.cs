using ImageChecker.Const;
using ImageChecker.Helper;
using OpenCvSharp;
using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageChecker.DataClass;

public sealed class FileImage : ABindableBase
{
    public string Filepath { get; set; }

    public Mat<float> SURFDescriptors { get; set; }

    private FileInfo _file;
    public FileInfo File { get => _file; set => SetProperty(ref _file, value); }

    private BitmapSource _bitmapImage;
    public BitmapSource BitmapImage { get => _bitmapImage; set => SetProperty(ref _bitmapImage, value); }

    private int? _pixelCount;
    public int? PixelCount
    {
        get => _pixelCount;
        set => SetProperty(ref _pixelCount, value);
    }

    public string BackupFilePath { get; internal set; }

    #region ctor
    public FileImage(string filepath, Mat<float> matrix)
    {
        Filepath = filepath;
        SURFDescriptors = matrix;
    }
    #endregion ctor

    public bool FileExists()
    {
        return System.IO.File.Exists(CommonConst.LONG_PATH_PREFIX + Filepath);
    }

    protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(sender, e);

        switch (e.PropertyName)
        {
            case nameof(BitmapImage):
                if (BitmapImage != null && PixelCount == null)
                {
                    PixelCount = BitmapImage.PixelWidth * BitmapImage.PixelHeight;
                }
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (SURFDescriptors != null)
        {
            SURFDescriptors.Dispose();
            SURFDescriptors = null;
        }

        BitmapImage = null;
        File = null;

        base.Dispose(disposing);
    }
}
