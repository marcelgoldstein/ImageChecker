using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ImageChecker.DataClass;

public sealed class FileImage : IDisposable, INotifyPropertyChanged
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


    #region ctor
    public FileImage(string filepath, Mat<float> matrix)
    {
        Filepath = filepath;
        SURFDescriptors = matrix;

        PropertyChanged += FileImage_PropertyChanged;
    }
    #endregion ctor

    public bool FileExists()
    {
        return System.IO.File.Exists(Filepath);
    }

    private void FileImage_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BitmapImage):
                if (BitmapImage != null && PixelCount == null)
                {
                    PixelCount = BitmapImage.PixelWidth * BitmapImage.PixelHeight;
                }
                break;
            default:
                break;
        }
    }

    #region IDisposable
    public void Dispose()
    {
        if (SURFDescriptors != null)
        {
            SURFDescriptors.Dispose();
            SURFDescriptors = null;
        }

        BitmapImage = null;
        File = null;
    }
    #endregion

    #region INotifyPropertyChanged
    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            RaisePropertyChanged(propertyName);
        }
    }
    #endregion
}
