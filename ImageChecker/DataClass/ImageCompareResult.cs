using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ImageChecker.DataClass;

public class ImageCompareResult : INotifyPropertyChanged
{
    public enum StateEnum
    {
        Unsolved = 0,
        Solved = 1
    }

    public FileImage FileA { get; set; }
    public FileImage FileB { get; set; }

    private double _flann;
    public double FLANN { get => _flann; set => SetProperty(ref _flann, value); }

    public float DifferencePercentage { get; set; }
    public float DuplicatePossibility { get; set; }

    private int? _levenshteinResult;
    public int? LevenshteinResult
    {
        get => _levenshteinResult;
        set => SetProperty(ref _levenshteinResult, value);
    }

    private float? _aForgeResult;
    public float? AForgeResult
    {
        get => _aForgeResult;
        set => SetProperty(ref _aForgeResult, value);
    }

    private double? _averagePixelA;
    public double? AveragePixelA { get => _averagePixelA; set => SetProperty(ref _averagePixelA, value); }

    private double? _averagePixelR;
    public double? AveragePixelR { get => _averagePixelR; set => SetProperty(ref _averagePixelR, value); }

    private double? _averagePixelG;
    public double? AveragePixelG { get => _averagePixelG; set => SetProperty(ref _averagePixelG, value); }

    private double? _averagePixelB;
    public double? AveragePixelB { get => _averagePixelB; set => SetProperty(ref _averagePixelB, value); }

    private string _smallerOne;
    public string SmallerOne
    {
        get => _smallerOne;
        set => SetProperty(ref _smallerOne, value);
    }

    private StateEnum _state;
    public StateEnum State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public bool IsSolved
    {
        get { return (!FileA.FileExists() || !FileB.FileExists()); }
    }

    public bool ImageLoadStarted { get; set; }


    #region INotifyPropertyChanged
    public void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
