using ImageChecker.Concurrent;
using ImageChecker.DataClass;
using ImageChecker.Helper;
using ImageChecker.Imaging;
using ImageChecker.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageChecker.Processing;

public class WorkerImageComparison : ViewModelBase, IDisposable
{
    #region Properties
    private bool _isComparingImages = false;
    public bool IsComparingImages
    {
        get => _isComparingImages;
        set
        {
            if (_isComparingImages != value)
            {
                _isComparingImages = value;
                RaisePropertyChanged(nameof(IsComparingImages));
            }
        }
    }

    private bool _isComparingImagesPaused = false;
    public bool IsComparingImagesPaused
    {
        get => _isComparingImagesPaused;
        set
        {
            if (_isComparingImagesPaused != value)
            {
                _isComparingImagesPaused = value;
                RaisePropertyChanged(nameof(IsComparingImagesPaused));
            }
        }
    }

    private CancellationTokenSource _ctsImageComparison;
    public CancellationTokenSource CtsImageComparison
    {
        get
        {
            if (_ctsImageComparison == null)
            {
                _ctsImageComparison = new CancellationTokenSource();
            }

            return _ctsImageComparison;
        }
        set
        {
            if (_ctsImageComparison != value)
            {
                _ctsImageComparison = value;
                RaisePropertyChanged("CtsImageComparision");
            }
        }
    }

    private PauseTokenSource _ptsImageComparison;
    public PauseTokenSource PtsImageComparison
    {
        get
        {
            if (_ptsImageComparison == null)
            {
                _ptsImageComparison = new PauseTokenSource();
            }

            return _ptsImageComparison;
        }
        set
        {
            if (_ptsImageComparison != value)
            {
                _ptsImageComparison = value;
                RaisePropertyChanged(nameof(PtsImageComparison));
            }
        }
    }

    public Progress<ProgressImageComparison> ImageComparisonProgress { get; set; }
    public IProgress<ProgressImageComparison> ImageComparisonProgressInterface { get { return ImageComparisonProgress as IProgress<ProgressImageComparison>; } }

    private ConcurrentBag<string> _errorFiles;
    public ConcurrentBag<string> ErrorFiles
    {
        get
        {
            if (_errorFiles == null)
                _errorFiles = new ConcurrentBag<string>();

            return _errorFiles;
        }
        set
        {
            if (_errorFiles != value)
            {
                _errorFiles = value;
                RaisePropertyChanged(nameof(ErrorFiles));
            }
        }
    }

    private bool _hasErrorFiles;
    public bool HasErrorFiles
    {
        get => _hasErrorFiles;
        set
        {
            if (_hasErrorFiles != value)
            {
                _hasErrorFiles = value;
                RaisePropertyChanged(nameof(HasErrorFiles));
            }
        }
    }

    private ConcurrentBag<ImageCompareResult> _possibleDuplicates;
    public ConcurrentBag<ImageCompareResult> PossibleDuplicates
    {
        get
        {
            if (_possibleDuplicates == null)
                _possibleDuplicates = new ConcurrentBag<ImageCompareResult>();

            return _possibleDuplicates;
        }
        set => SetProperty(ref _possibleDuplicates, value);
    }

    private int _selectedPossibleDuplicatesCount = -1;
    public int SelectedPossibleDuplicatesCount
    {
        get => _selectedPossibleDuplicatesCount;
        set => SetProperty(ref _selectedPossibleDuplicatesCount, value);
    }

    private string _selectedPossibleDuplicatesCountMessage;
    public string SelectedPossibleDuplicatesCountMessage
    {
        get => _selectedPossibleDuplicatesCountMessage;
        set => SetProperty(ref _selectedPossibleDuplicatesCountMessage, value);
    }

    private bool _hasPossibleDuplicates;
    public bool HasPossibleDuplicates
    {
        get => _hasPossibleDuplicates;
        set => SetProperty(ref _hasPossibleDuplicates, value);
    }

    private List<DirectoryInfo> _folders;
    public List<DirectoryInfo> Folders
    {
        get { if (_folders == null) _folders = new List<DirectoryInfo>(); return _folders; }
        set => SetProperty(ref _folders, value);
    }

    private bool _includeSubdirectories;
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set => SetProperty(ref _includeSubdirectories, value);
    }

    private bool _preResizeImages;
    public bool PreResizeImages
    {
        get => _preResizeImages;
        set => SetProperty(ref _preResizeImages, value);
    }

    private int _preResizeScale;
    public int PreResizeScale
    {
        get => _preResizeScale;
        set => SetProperty(ref _preResizeScale, value);
    }

    private double _threshold;
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value);
    }

    private List<string> _files;
    public List<string> Files
    {
        get => _files;
        set => SetProperty(ref _files, value);
    }

    #endregion

    #region Members
    private ProgressImageComparison _currentProgress;

    private readonly Stopwatch _timer = new();
    private long _fullOperationsCount = 0L;
    private long _fullOperationsCurrentCount = 0L;
    #endregion

    #region ctr
    public WorkerImageComparison()
    {
        ImageComparisonProgress = new Progress<ProgressImageComparison>();

        PropertyChanged += WorkerImageComparison_PropertyChanged;
    }

    void WorkerImageComparison_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ErrorFiles):
                HasErrorFiles = ErrorFiles.Any();
                break;
            case nameof(PossibleDuplicates):
                HasPossibleDuplicates = PossibleDuplicates.Any();
                break;
            case nameof(SelectedPossibleDuplicatesCount):
                SelectedPossibleDuplicatesCountMessage = $"show [{SelectedPossibleDuplicatesCount}] results";
                break;
            default:
                break;
        }
    }
    #endregion

    #region Methods
    public void RefreshSelectedPossibleDuplicatesCount(double treshold)
    {
        SelectedPossibleDuplicatesCount = PossibleDuplicates.Count(a => a.FLANN >= treshold);
    }

    public async Task Start()
    {
        PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();

        await ReadFileNameForImageComarisonAsync();
    }

    private async Task ReadFileNameForImageComarisonAsync()
    {
        if (!IsComparingImages)
        {
            var validExtensions = new List<string>()
            {
                ".bmp".ToUpper(),
                ".gif".ToUpper(),
                ".jpeg".ToUpper(),
                ".jpg".ToUpper(),
                ".png".ToUpper(),
                ".tif".ToUpper(),
                ".tiff".ToUpper(),
                ".jfif".ToUpper(),
                ".webp".ToUpper()
            };

            Files = Folders.SelectMany(a => a.GetFiles("*.*", IncludeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                .Where(a => validExtensions.Contains(a.Extension.ToUpper()))
                .Select(a => a.FullName).ToList();

            _currentProgress = new ProgressImageComparison(0, Files.Count, 0, "preparing", null, null);

            ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, null));

            if (Files.Count > 0)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                await CompareImagesAsync(Files);
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            }
        }
    }

    private async Task CompareImagesAsync(List<string> files)
    {
        CtsImageComparison = new CancellationTokenSource();
        PtsImageComparison = new PauseTokenSource();
        IsComparingImages = true;

        #region loading from files
        _currentProgress.Value = 0;
        _currentProgress.Operation = "loading files";
        ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, null));

        ConcurrentBag<FileImage> preLoadedFileImagesSource = new ConcurrentBag<FileImage>();
        // reset stuff
        ErrorFiles = new ConcurrentBag<string>();
        HasErrorFiles = false;
        PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();
        HasPossibleDuplicates = false;

        await Task.Run(async () =>
        {
            CustomFLANN cf = new CustomFLANN();


            int CONCURRENCY_LEVEL = Environment.ProcessorCount - 1;
            CONCURRENCY_LEVEL = CONCURRENCY_LEVEL < 1 ? 1 : CONCURRENCY_LEVEL;
            int nextIndex = 0;
            var imageTasks = new List<Task>();
            while (nextIndex < CONCURRENCY_LEVEL && nextIndex < files.Count)
            {
                imageTasks.Add(cf.ComputeSingleDescriptorsAsync(files[nextIndex], preLoadedFileImagesSource, ErrorFiles, PreResizeImages, PreResizeScale));
                nextIndex++;

                await Task.Delay(100);
            }

            while (imageTasks.Count > 0)
            {
                Task imageTask = await Task.WhenAny(imageTasks).ConfigureAwait(false);
                imageTasks.Remove(imageTask);

                _currentProgress.Value++;
                ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, null));

                if (CtsImageComparison.Token.IsCancellationRequested)
                    break;

                await PtsImageComparison.Token.WaitWhilePausedAsync();

                if (nextIndex < files.Count)
                {
                    imageTasks.Add(cf.ComputeSingleDescriptorsAsync(files[nextIndex], preLoadedFileImagesSource, _errorFiles, PreResizeImages, PreResizeScale));
                    nextIndex++;
                }
            }
        });

        if (_errorFiles.Count > 0)
            HasErrorFiles = true;

        if (CtsImageComparison.IsCancellationRequested)
        {
            IsComparingImages = false;
            _currentProgress.Operation = "loading files canceled!";
            ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, null));
            return;
        }
        #endregion

        _timer.Restart();
        CtsImageComparison = new CancellationTokenSource();
        PtsImageComparison = new PauseTokenSource();
        long fullOperationsCurrentToDoCount = 0;
        long secondsElapsed = 0;
        long secondsToGo = 0;

        _currentProgress.Value = 0;
        _currentProgress.Maximum = preLoadedFileImagesSource.Count;
        _fullOperationsCount = MathHelper.SumToN((long)_currentProgress.Maximum - 1L);
        _currentProgress.Operation = "comparing images";
        ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, null));

        await Task.Run(async () =>
        {
            List<FileImage> toDo = preLoadedFileImagesSource.ToList();

            List<WorkItem> workItems = new List<WorkItem>();

            foreach (var item in toDo)
            {
                var wi = new WorkItem
                {
                    ItemToCheck = item,
                    ItemsToCheckAgainst = toDo.Skip(toDo.IndexOf(item) + 1).ToList()
                };
                if (Path.GetExtension(wi.ItemToCheck.Filepath).ToUpper() == ".gif".ToUpper())
                    wi.ItemsToCheckAgainst.RemoveAll(a => Path.GetExtension(a.Filepath).ToUpper() != ".gif".ToUpper()); // wenn item ein .gif ist, alle nicht-.gifs nicht gegenprüfen
                else
                    wi.ItemsToCheckAgainst.RemoveAll(a => Path.GetExtension(a.Filepath).ToUpper() == ".gif".ToUpper()); // wenn item kein .gif ist, alle .gifs nicht gegenprüfen
                workItems.Add(wi);
            }

            CustomFLANN cf = new CustomFLANN();

            int CONCURRENCY_LEVEL = Environment.ProcessorCount - 1;
            CONCURRENCY_LEVEL = CONCURRENCY_LEVEL < 1 ? 1 : CONCURRENCY_LEVEL;

            int nextIndex = 0;
            var imageTasks = new List<Task>();
            while (nextIndex < CONCURRENCY_LEVEL && nextIndex < workItems.Count)
            {
                imageTasks.Add(CustomFLANN.FindMatches(workItems[nextIndex].ItemToCheck, workItems[nextIndex].ItemsToCheckAgainst, PossibleDuplicates, Threshold, CtsImageComparison.Token, PtsImageComparison.Token));
                nextIndex++;

                await Task.Delay(100);
            }

            while (imageTasks.Count > 0)
            {
                Task imageTask = await Task.WhenAny(imageTasks).ConfigureAwait(false);
                imageTasks.Remove(imageTask);

                _currentProgress.Value++;

                // gauss berechnung und stopwatch verwenden
                _fullOperationsCurrentCount = MathHelper.PartialSumToNProgress((long)_currentProgress.Maximum, (long)_currentProgress.Value);
                fullOperationsCurrentToDoCount = _fullOperationsCount - _fullOperationsCurrentCount;
                secondsElapsed = (long)(_timer.Elapsed.TotalSeconds - (PtsImageComparison.Pauses.Sum(a => a.TotalSeconds)));
                secondsToGo = (long)(((double)secondsElapsed / (double)_fullOperationsCurrentCount) * (double)fullOperationsCurrentToDoCount);

                ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, secondsToGo, secondsElapsed));

                if (CtsImageComparison.Token.IsCancellationRequested)
                    break;

                await PtsImageComparison.Token.WaitWhilePausedAsync();

                if (nextIndex < workItems.Count)
                {
                    imageTasks.Add(CustomFLANN.FindMatches(workItems[nextIndex].ItemToCheck, workItems[nextIndex].ItemsToCheckAgainst, PossibleDuplicates, Threshold, CtsImageComparison.Token, PtsImageComparison.Token));
                    nextIndex++;
                }
            }

            ClearFalsePositives();
        });


        HasPossibleDuplicates = PossibleDuplicates.Any();

        if (CtsImageComparison.IsCancellationRequested)
        {
            IsComparingImages = false;
            _currentProgress.Operation = "comparing images canceled!";
            ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, secondsElapsed));
            _timer.Stop();
            return;
        }
        else
        { // erfolgreich und vollständig durchlaufen
            IsComparingImages = false;
            _currentProgress.Operation = "comparing images completed!";
            ImageComparisonProgressInterface.Report(new ProgressImageComparison(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation, null, secondsElapsed));
            _timer.Stop();
            return;
        }
    }

    private class ClearResult
    {
        public string FilePath { get; set; }
        public ImageCompareResult ICR { get; set; }
    }

    private void ClearFalsePositives()
    {
        var dups = PossibleDuplicates.Where(a => a.FLANN < 100D).ToList();

        var flattedDups = dups.Select(a => new ClearResult { FilePath = a.FileA.Filepath, ICR = a }).Concat(dups.Select(b => new ClearResult { FilePath = b.FileB.Filepath, ICR = b }));

        var tresholdMapping = new Dictionary<double, int>()
        {
            [00D] = 4,
            [10D] = 3,
            [20D] = 2,
            [30D] = 1,
            [40D] = 1,
            [50D] = 1,
            [60D] = 1,
            [70D] = 1,
            [80D] = 1,
            [90D] = 1,
        };

        var badResults = flattedDups.GroupBy(a => new { a.ICR.FLANN, a.FilePath }).Where(a => a.Count() > (tresholdMapping.Last(b => b.Key < a.Key.FLANN).Value))
                                            .SelectMany(a => a)
                                            .Select(a => a.ICR)
                                            .Distinct();

        var icrList = PossibleDuplicates.Except(badResults).ToList();

        while (PossibleDuplicates.TryTake(out var icr))
        { } // so lange items entfernen, wie ein Item erfolgreich entfernt wurde....

        foreach (var item in icrList)
            PossibleDuplicates.Add(item);
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        if (_ctsImageComparison != null)
        {
            _ctsImageComparison.Dispose();
            _ctsImageComparison = null;
        }
    }
    #endregion
}
