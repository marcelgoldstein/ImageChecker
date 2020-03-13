using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace ImageChecker.DataClass
{
    public class FileImage : IDisposable, INotifyPropertyChanged
    {
        public string Filepath { get; set; }

        public Mat<float> SURFDescriptors { get; set; }

        private FileInfo file;
        public FileInfo File { get { return file; } set { SetProperty(ref file, value); } }

        private BitmapSource bitmapImage;
        public BitmapSource BitmapImage { get { return bitmapImage; } set { SetProperty(ref bitmapImage, value); } }

        private int? pixelCount;
        public int? PixelCount
        {
            get { return pixelCount; }
            set { SetProperty(ref pixelCount, value); }
        }


        #region ctor
        public FileImage(string filepath, Mat<float> matrix)
        {
            this.Filepath = filepath;
            this.SURFDescriptors = matrix;

            this.PropertyChanged += this.FileImage_PropertyChanged;
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
                case nameof(FileImage.BitmapImage):
                    if (this.BitmapImage != null && this.PixelCount == null)
                    {
                        this.PixelCount = this.BitmapImage.PixelWidth * this.BitmapImage.PixelHeight;
                    }
                    break;
                default:
                    break;
            }
        }

        #region IDisposable
        public virtual void Dispose()
        {
            if (SURFDescriptors != null)
            {
                SURFDescriptors.Dispose();
                SURFDescriptors = null;
            }

            this.BitmapImage = null;
            this.File = null;
        }
        #endregion

        #region INotifyPropertyChanged
        public void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                RaisePropertyChanged(propertyName);
            }
        }
        #endregion
    }
}
