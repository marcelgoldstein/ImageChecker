using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageChecker.DataClass
{
    public class ImageCompareResult : INotifyPropertyChanged
    {
        public enum StateEnum
        {
            Unsolved = 0,
            Solved = 1
        }

        public FileImage FileA { get; set; }
        public FileImage FileB { get; set; }

        private double flann;
        public double FLANN { get { return flann; } set { SetProperty(ref flann, value); } }

        public float DifferencePercentage { get; set; }
        public float DuplicatePossibility { get; set; }

        private int? levenshteinResult;
        public int? LevenshteinResult
        {
            get
            {
                return levenshteinResult;
            }
            set
            {
                SetProperty(ref levenshteinResult, value);
            }
        }

        private float? aForgeResult;
        public float? AForgeResult
        {
            get
            {
                return aForgeResult;
            }
            set
            {
                SetProperty(ref aForgeResult, value);
            }
        }

        private double? averagePixelA;
        public double? AveragePixelA { get { return averagePixelA; } set { SetProperty(ref averagePixelA, value); } }

        private double? averagePixelR;
        public double? AveragePixelR { get { return averagePixelR; } set { SetProperty(ref averagePixelR, value); } }

        private double? averagePixelG;
        public double? AveragePixelG { get { return averagePixelG; } set { SetProperty(ref averagePixelG, value); } }

        private double? averagePixelB;
        public double? AveragePixelB { get { return averagePixelB; } set { SetProperty(ref averagePixelB, value); } }

        private string smallerOne;
        public string SmallerOne
        {
            get { return smallerOne; }
            set { SetProperty(ref smallerOne, value); }
        }

        private StateEnum state;
        public StateEnum State
        {
            get { return state; }
            set { SetProperty(ref state, value); }
        }

        public bool IsSolved
        {
            get { return (!FileA.FileExists() || !FileB.FileExists()); }
        }

        public bool ImageLoadStarted { get; set; }


        #region INotifyPropertyChanged
        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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
