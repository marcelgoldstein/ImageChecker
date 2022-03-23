using ImageChecker.Const;
using ImageChecker.DataClass;
using ImageChecker.Helper;
using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageChecker.ViewModel;

public sealed class VMResultView : ViewModelBase, IDisposable
{
    #region const
    private readonly int _preLoadRange = 10;
    #endregion const

    #region fields
    private bool _isDisposed = false;

    private static readonly SemaphoreSlim _cancelSlimmy = new(1);
    private static readonly SemaphoreSlim _workingSlimmy = new(1);
    #endregion fields

    #region Properties
    private CancellationTokenSource _preLoadImagesCancelTokenSource;

    #region Window
    public static string WindowTitle { get { return "ResultView"; } }
    public static string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }
    #endregion Window

    private RangeObservableCollection<ImageCompareResult> _results = new();
    public RangeObservableCollection<ImageCompareResult> Results
    {
        get => _results;
        set => SetProperty(ref _results, value);
    }

    private CollectionViewSource _resultsView;

    public CollectionViewSource ResultsView
    {
        get => _resultsView ??= new CollectionViewSource { Source = Results };
        set => SetProperty(ref _resultsView, value);
    }

    private IList _selectedResults;
    public IList SelectedResults
    {
        get => _selectedResults ??= new List<object>();
        set => SetProperty(ref _selectedResults, value);
    }

    private ImageCompareResult _selectedResult;
    public ImageCompareResult SelectedResult
    {
        get => _selectedResult;
        set => SetProperty(ref _selectedResult, value);
    }

    private bool _isExterminationModeActive;

    public bool IsExterminationModeActive
    {
        get => _isExterminationModeActive;
        set => SetProperty(ref _isExterminationModeActive, value);
    }

    #region Filters
    private bool _filterActivated;
    public bool FilterActivated
    {
        get => _filterActivated;
        set => SetProperty(ref _filterActivated, value);
    }

    private string _fileFilter;
    public string FileFilter
    {
        get => _fileFilter;
        set => SetProperty(ref _fileFilter, value);
    }

    private List<StatusFilter> _statusFilters;
    public List<StatusFilter> StatusFilters
    {
        get { if (_statusFilters == null) { _statusFilters = new List<StatusFilter>() { new StatusFilter(1, "show all"), new StatusFilter(2, "show unsolved"), new StatusFilter(3, "show solved") }; } return _statusFilters; }
        set => SetProperty(ref _statusFilters, value);
    }

    private StatusFilter _selectedStatusFilter;
    public StatusFilter SelectedStatusFilter
    {
        get { if (_selectedStatusFilter == default) _selectedStatusFilter = StatusFilters.First(); return _selectedStatusFilter; }
        set => SetProperty(ref _selectedStatusFilter, value);
    }
    #endregion Filters

    #region Statistics
    private int _processedFilesCount;
    public int ProcessedFilesCount
    {
        get => _processedFilesCount;
        set => SetProperty(ref _processedFilesCount, value);
    }

    private int _foundDuplicatesCount;
    public int FoundDuplicatesCount
    {
        get => _foundDuplicatesCount;
        set => SetProperty(ref _foundDuplicatesCount, value);
    }

    private int _unsolvedDuplicatesCount;
    public int UnsolvedDuplicatesCount
    {
        get => _unsolvedDuplicatesCount;
        set => SetProperty(ref _unsolvedDuplicatesCount, value);
    }

    private int _solvedDuplicatesCount;
    public int SolvedDuplicatesCount
    {
        get => _solvedDuplicatesCount;
        set => SetProperty(ref _solvedDuplicatesCount, value);
    }
    #endregion Statistics
    #endregion Properties

    #region ctor
    public VMResultView(IEnumerable<ImageCompareResult> items, int totalFilesProcessed)
    {
        PropertyChanged += VMResultView_PropertyChanged;

        Results.Clear();
        Results.AddRange(items.OrderByDescending(a => a.FLANN));

        _ = Task.Run(async () =>
        {
            foreach (var icr in Results)
            {
                icr.FileA.File = new FileInfo(icr.FileA.Filepath);
                icr.FileB.File = new FileInfo(icr.FileB.Filepath);
            }

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                SelectedResult = Results.FirstOrDefault();
            });
        });

        _ = Task.Run(async () =>
        {
            while (!_isDisposed)
            {
                RefreshImageCompareResultState();
                UnsolvedDuplicatesCount = Results.Count(a => a.State == ImageCompareResult.StateEnum.Unsolved);
                SolvedDuplicatesCount = Results.Count(a => a.State == ImageCompareResult.StateEnum.Solved);

                await Task.Delay(1000);
            }
        });

        TempFilesHelper.EnsureResultViewBackupDirectoryExists();

        ProcessedFilesCount = totalFilesProcessed;
        FoundDuplicatesCount = items.Count();
    }
    #endregion ctor

    #region Event-Handler
    private async void VMResultView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedResult):
                if (SelectedResult != null)
                {
                    try
                    {
                        await _cancelSlimmy.WaitAsync();
                        _preLoadImagesCancelTokenSource?.Cancel();
                        _preLoadImagesCancelTokenSource = new CancellationTokenSource();
                        var ct = _preLoadImagesCancelTokenSource.Token;
                        _cancelSlimmy.Release();

                        await _workingSlimmy.WaitAsync();
                        await LoadImagesAsync(SelectedResult, ct);
                    }
                    catch (OperationCanceledException)
                    { }
                    finally
                    {
                        _workingSlimmy.Release();
                    }
                }
                break;
        }
    }
    #endregion Event-Handler

    #region Methods
    private void RefreshImageCompareResultState()
    {
        foreach (var r in Results)
        {
            r.State = r.IsSolved ? ImageCompareResult.StateEnum.Solved : ImageCompareResult.StateEnum.Unsolved;
        }
    }

    private void SelectNextUnsolvedResult()
    {
        if (SelectedResult != null && Results != null)
        {
            var item = SelectedResult; // save it cause of the bug?

            var view = ResultsView.View.OfType<ImageCompareResult>().ToList(); // this line magically changes the 'SelectedResult' to the first in the listing...???? -> its a bug, not a feature

            item = view.Skip(view.IndexOf(item) + 1).FirstOrDefault(a => a.IsSolved == false);

            if (item != null)
            {
                SelectedResult = item;
            }
            else
            {
                item = view.FirstOrDefault(a => a.IsSolved == false);

                if (item != null)
                {
                    SelectedResult = item;
                }
            }

            // scroll into view

        }
    }

    private void SelectPreviousUnsolvedResult()
    {
        if (SelectedResult != null && Results != null)
        {
            var item = SelectedResult; // save it cause of the bug?

            var view = ResultsView.View.OfType<ImageCompareResult>().ToList(); // this line magically changes the 'SelectedResult' to the first in the listing...???? -> its a bug, not a feature

            item = view.Take(view.IndexOf(item)).LastOrDefault(a => a.IsSolved == false);

            if (item != null)
            {
                SelectedResult = item;
            }
            else
            {
                item = view.LastOrDefault(a => a.IsSolved == false);

                if (item != null)
                {
                    SelectedResult = item;
                }
            }

            // scroll into view

        }
    }

    private static void UnloadImages(List<ImageCompareResult> results)
    {
        foreach (var r in results)
        {
            r.FileA.BitmapImage = null;
            r.FileB.BitmapImage = null;

            r.ImageLoadStarted = false;
        }
    }

    private static async Task LoadImagesAsync(List<ImageCompareResult> results, CancellationToken ct)
    {
        var resultsToProcess = results.Where(a => a.ImageLoadStarted == false).ToList();

        if (resultsToProcess.Count == 0)
            return;

        foreach (var r in resultsToProcess)
        {
            r.ImageLoadStarted = true;
        }

        try
        {
            var fi = resultsToProcess
                .Select(a => a.FileA).Concat(resultsToProcess.Select(a => a.FileB))
                .Where(a => a.BitmapImage == null);

            var fig = fi.GroupBy(a => (a.Filepath, a.BackupFilePath));

            foreach (var g in fig)
            {
                ct.ThrowIfCancellationRequested();

                var fiToUse = g.First();
                var path = string.Empty;
                if (File.Exists(CommonConst.LONG_PATH_PREFIX + fiToUse.Filepath))
                    path = CommonConst.LONG_PATH_PREFIX + fiToUse.Filepath;
                else if(File.Exists(CommonConst.LONG_PATH_PREFIX + fiToUse.BackupFilePath))
                    path = CommonConst.LONG_PATH_PREFIX + fiToUse.BackupFilePath;

                if (File.Exists(path))
                {
                    var bmi = new BitmapImage();
                    using (var fs = File.OpenRead(path))
                    {
                        bmi.BeginInit();
                        bmi.StreamSource = fs;
                        bmi.CacheOption = BitmapCacheOption.OnLoad;
                        bmi.EndInit();
                    }

                    bmi.Freeze();
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var f in g)
                        {
                            f.BitmapImage = bmi;
                        }
                    });
                }
            }
        }
        finally
        {
            foreach (var icr in resultsToProcess)
            {
                FindSmallerOne(icr);
            }

            // ImageLoadStarted zurücksetzen, wenn nicht das Bitmap von FileA und FileB != null ist
            foreach (var r in resultsToProcess)
            {
                if (r.FileA?.BitmapImage == null || r.FileB?.BitmapImage == null)
                {
                    r.ImageLoadStarted = false;
                }
            }
        }
    }

    private async Task LoadImagesAsync(ImageCompareResult icr, CancellationToken ct)
    {
        await Task.Run(async () =>
        {
            var items = Results.OrderBy(a => a.FLANN).ToList();
            var currentItemIndex = items.IndexOf(icr);

            #region Load Bitmaps
            var startIndex = currentItemIndex - _preLoadRange;
            startIndex = ((startIndex < 0) ? 0 : startIndex);

            var endIndex = currentItemIndex + _preLoadRange;
            endIndex = ((endIndex > (items.Count - 1)) ? (items.Count - 1) : endIndex);

            var itemsToProcess = new List<ImageCompareResult>();
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i == currentItemIndex)
                { // das SelectedItem auf Position 1 setzen, damit dieses als erstes verarbeitet wird
                    itemsToProcess.Insert(0, items[i]);
                }
                else
                {
                    itemsToProcess.Add(items[i]);
                }
            }

            await LoadImagesAsync(itemsToProcess, ct);
            #endregion Load Bitmaps

            #region Unload Bitmaps
            ct.ThrowIfCancellationRequested();

            var filesToProcess = itemsToProcess.SelectMany(a => new[] { a.FileA, a.FileB }).ToList();
            var itemsToUnloadBitmap = items.Where(a => filesToProcess.Contains(a.FileA) == false && filesToProcess.Contains(a.FileB) == false).ToList();
            UnloadImages(itemsToUnloadBitmap);
            #endregion Unload Bitmaps
        }, ct);
    }

    private static void FindSmallerOne(ImageCompareResult icr)
    {
        if (string.IsNullOrWhiteSpace(icr.SmallerOne))
        {
            icr.SmallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? "FileA" : icr.FileA.PixelCount > icr.FileB.PixelCount ? "FileB" : null;
        }
    }

    private static async Task<bool> TryCreateBackupAsync(FileImage fi)
    {
        return await Task.Run(() =>
        {
            if (fi.BackupFilePath != null && File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath))
                return true; // backup already exists

            if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.File.FullName) == false)
                return false; // backup not possible without source file

            var backupFilePath = TempFilesHelper.CreateNewBackupFilePath(CommonConst.LONG_PATH_PREFIX + fi.File.FullName);
            File.Copy(CommonConst.LONG_PATH_PREFIX + fi.File.FullName, backupFilePath, true);
            fi.BackupFilePath = backupFilePath;

            return true;
        });
    }
    #endregion Methods

    #region Commands
    #region ApplyFiltersCommand
    private ICommand _applyFiltersCommand;
    public ICommand ApplyFiltersCommand
    {
        get
        {
            if (_applyFiltersCommand == null)
            {
                _applyFiltersCommand = new RelayCommand(p => ApplyFilters(),
                    p => CanApplyFilters());
            }
            return _applyFiltersCommand;
        }
    }

    public void ApplyFilters()
    {
        if (!FilterActivated)
        { // alle filter entfernen
            ResultsView.View.Filter = null;
        }
        else
        { // anwenden
            ResultsView.View.Filter =
                new Predicate<object>(a =>
                    {
                        var b = a as ImageCompareResult;

                        return (
                            (SelectedStatusFilter.ID == 1
                            ||
                                (
                                    (SelectedStatusFilter.ID == 2 && b.IsSolved == false)
                                        ||
                                    (SelectedStatusFilter.ID == 3 && b.IsSolved == true)
                                )
                            )
                            &&
                            (
                                (
                                    string.IsNullOrWhiteSpace(FileFilter)
                                    ||
                                        (b.FileA.File.Name.ToUpper().Contains(FileFilter.ToUpper()))
                                        ||
                                        (b.FileB.File.Name.ToUpper().Contains(FileFilter.ToUpper()))
                                )
                            )
                        );
                    }
                );
        }
    }

    private static bool CanApplyFilters()
    {
        return true;
    }
    #endregion
    #region ActivateFiltersCommand
    private ICommand _activateFiltersCommand;
    public ICommand ActivateFiltersCommand
    {
        get
        {
            if (_activateFiltersCommand == null)
            {
                _activateFiltersCommand = new RelayCommand(p => ActivateFilters(),
                    p => CanActivateFilters());
            }
            return _activateFiltersCommand;
        }
    }

    public void ActivateFilters()
    {
        // toggle filter active/deactive
        FilterActivated = !FilterActivated;

        // filter anwenden bzw. aufheben
        ApplyFilters();
    }

    private static bool CanActivateFilters()
    {
        return true;
    }
    #endregion ActivateFiltersCommand


    #region DeleteFile
    private ICommand _deleteFileCommand;
    public ICommand DeleteFileCommand
    {
        get
        {
            if (_deleteFileCommand == null)
            {
                _deleteFileCommand = new RelayCommand(p => DeleteFile(p),
                    p => CanDeleteFile(p));
            }
            return _deleteFileCommand;
        }
    }

    public async void DeleteFile(object fileImage)
    {
        if (fileImage is FileImage fi)
        {
            _ = await TryCreateBackupAsync(fi);

            FileOperationAPIWrapper.Send(fi.File.FullName, FileOperationAPIWrapper.FileOperationFlags.FOF_ALLOWUNDO | FileOperationAPIWrapper.FileOperationFlags.FOF_NOCONFIRMATION | FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
        }

        if (IsExterminationModeActive)
            SelectNextUnsolvedResult();
    }

    private static bool CanDeleteFile(object fileImage)
    {
        if (fileImage is FileImage fi)
        {
            return File.Exists(CommonConst.LONG_PATH_PREFIX + fi.File.FullName);
        }

        return false;
    }
    #endregion DeleteFile

    #region MoveSelectionDown
    private ICommand _moveSelectionDownCommand;
    public ICommand MoveSelectionDownCommand
    {
        get
        {
            if (_moveSelectionDownCommand == null)
            {
                _moveSelectionDownCommand = new RelayCommand(p => MoveSelectionDown(),
                    p => CanMoveSelectionDown());
            }
            return _moveSelectionDownCommand;
        }
    }

    public void MoveSelectionDown()
    {
        SelectNextUnsolvedResult();
    }

    private static bool CanMoveSelectionDown()
    {
        return true;
    }
    #endregion MoveSelectionUp

    #region MoveSelectionUp
    private ICommand _moveSelectionUpCommand;
    public ICommand MoveSelectionUpCommand
    {
        get
        {
            if (_moveSelectionUpCommand == null)
            {
                _moveSelectionUpCommand = new RelayCommand(p => MoveSelectionUp(),
                    p => CanMoveSelectionUp());
            }
            return _moveSelectionUpCommand;
        }
    }

    public void MoveSelectionUp()
    {
        SelectPreviousUnsolvedResult();
    }

    private static bool CanMoveSelectionUp()
    {
        return true;
    }
    #endregion MoveSelectionUp

    #region OpenFolder
    private ICommand _openFolderCommand;
    public ICommand OpenFolderCommand
    {
        get
        {
            if (_openFolderCommand == null)
            {
                _openFolderCommand = new RelayCommand(p => OpenFolder(p),
                    p => CanOpenFolder(p));
            }
            return _openFolderCommand;
        }
    }

    public static void OpenFolder(object file)
    {
        if (file is FileInfo fi)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = string.Format(@"/select,{0}", fi.FullName),
                UseShellExecute = true
            });
        }
    }

    private static bool CanOpenFolder(object file)
    {
        if (file is FileInfo fi)
        {
            return File.Exists(CommonConst.LONG_PATH_PREFIX + fi.FullName);
        }

        return false;
    }
    #endregion OpenFolder

    #region ImageClick
    private ICommand _imageClickCommand;
    public ICommand ImageClickCommand
    {
        get
        {
            if (_imageClickCommand == null)
            {
                _imageClickCommand = new RelayCommand(p => ImageClick(p),
                    p => CanImageClick(p));
            }
            return _imageClickCommand;
        }
    }

    public void ImageClick(object p)
    {
        if (p is FileImage fileImage)
        {
            if (IsExterminationModeActive)
            {
                DeleteFile(fileImage);
            }
            else
            {
                if (fileImage.File is FileInfo fi)
                {
                    Process.Start(new ProcessStartInfo(fi.FullName) { UseShellExecute = true });
                }
            }
        }
    }

    private bool CanImageClick(object p)
    {
        if (p is FileImage fileImage)
        {
            if (IsExterminationModeActive)
            {
                return CanDeleteFile(fileImage);
            }
            else
            {
                if (fileImage.File is FileInfo fi)
                {
                    return File.Exists(CommonConst.LONG_PATH_PREFIX + fi.FullName);
                }
            }
        }

        return false;
    }
    #endregion ImageClick

    #region CutFile
    private ICommand _cutFileCommand;
    public ICommand CutFileCommand
    {
        get
        {
            if (_cutFileCommand == null)
            {
                _cutFileCommand = new RelayCommand(p => CutFile(p),
                    p => CanCutFile(p));
            }
            return _cutFileCommand;
        }
    }

    public static async void CutFile(object fileImage)
    {
        if (fileImage is FileImage fi)
        {
            _ = await TryCreateBackupAsync(fi);

            ClipboardHelper.SetFileToClipboard(fi.File.FullName, ClipboardHelper.Operation.Cut);
        }
    }

    private static bool CanCutFile(object fileImage)
    {
        if (fileImage is FileImage fi)
        {
            return File.Exists(CommonConst.LONG_PATH_PREFIX + fi.File.FullName);
        }

        return false;
    }
    #endregion CutFile

    #region CutSmallerOnes
    private ICommand _cutSmallerOnesCommand;
    public ICommand CutSmallerOnesCommand
    {
        get
        {
            if (_cutSmallerOnesCommand == null)
            {
                _cutSmallerOnesCommand = new RelayCommand(p => CutSmallerOnes(p),
                    p => CanCutSmallerOnes(p));
            }
            return _cutSmallerOnesCommand;
        }
    }

    public static async void CutSmallerOnes(object selectedRows)
    {
        List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();
        List<FileImage> smallerOnes = new List<FileImage>();

        foreach (var icr in icrs)
        {
            if (icr.FileA.PixelCount != null && icr.FileB.PixelCount != null)
            {
                var smallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? icr.FileA : icr.FileB;

                if (File.Exists(CommonConst.LONG_PATH_PREFIX + smallerOne.File.FullName))
                    smallerOnes.Add(smallerOne);
            }
        }

        foreach (var fileImage in smallerOnes)
        {
            _ = await TryCreateBackupAsync(fileImage);
        }

        ClipboardHelper.SetFilesToClipboard(smallerOnes.Select(a => a.File.FullName).Distinct(), ClipboardHelper.Operation.Cut);
    }

    private static bool CanCutSmallerOnes(object selectedRows)
    {
        List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();

        if (icrs.Count == 0)
            return false;

        return true;
    }
    #endregion CutSmallerOnes

    #region DeleteSmallerOnes
    private ICommand _deleteSmallerOnesCommand;
    public ICommand DeleteSmallerOnesCommand
    {
        get
        {
            if (_deleteSmallerOnesCommand == null)
            {
                _deleteSmallerOnesCommand = new RelayCommand(p => DeleteSmallerOnes(p),
                    p => CanDeleteSmallerOnes());
            }
            return _deleteSmallerOnesCommand;
        }
    }

    public static void DeleteSmallerOnes(object selectedRows)
    {
        List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();
        List<FileInfo> smallerOnes = new List<FileInfo>();

        foreach (var icr in icrs)
        {
            if (icr.FileA.PixelCount != null && icr.FileB.PixelCount != null)
            {
                var smallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? icr.FileA.File : icr.FileB.File;

                if (File.Exists(CommonConst.LONG_PATH_PREFIX + smallerOne.FullName))
                    smallerOnes.Add(smallerOne);
            }
        }

        foreach (var fi in smallerOnes.Select(a => a.FullName).Distinct())
        {
            FileOperationAPIWrapper.Send(fi, FileOperationAPIWrapper.FileOperationFlags.FOF_ALLOWUNDO | FileOperationAPIWrapper.FileOperationFlags.FOF_NOCONFIRMATION | FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
        }
    }

    private bool CanDeleteSmallerOnes()
    {
        if (SelectedResults.Count == 0)
            return false;

        return true;
    }
    #endregion DeleteSmallerOnes

    #region RestoreImage
    private ICommand _restoreImageCommand;
    public ICommand RestoreImageCommand
    {
        get
        {
            if (_restoreImageCommand == null)
            {
                _restoreImageCommand = new RelayCommand(p => RestoreImage(p),
                    p => CanRestoreImage(p));
            }
            return _restoreImageCommand;
        }
    }

    public static void RestoreImage(object fileImage)
    {
        if (fileImage is FileImage fi && File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath))
        {
            File.Copy(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath, CommonConst.LONG_PATH_PREFIX + fi.Filepath, true);
        }
    }

    private static bool CanRestoreImage(object fileImage)
    {
        if (fileImage is FileImage fi == false)
            return false;

        if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.Filepath))
            return false; // no need to restore

        if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath) == false)
            return false; // can not restore without backup

        return true;

    }
    #endregion RestoreImage

    #region SaveAsImage
    private ICommand _saveAsImageCommand;
    public ICommand SaveAsImageCommand
    {
        get
        {
            if (_saveAsImageCommand == null)
            {
                _saveAsImageCommand = new RelayCommand(p => SaveAsImage(p),
                    p => CanSaveAsImage(p));
            }
            return _saveAsImageCommand;
        }
    }

    public static void SaveAsImage(object fileImage)
    {
        if (fileImage is FileImage fi && (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.Filepath) || File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath)))
        {
            // prompt user for directory
            var dlg = new SaveFileDialog
            {
                FileName = fi.File.Name,
                DefaultExt = fi.File.Extension,
                Filter = $"Images |*{fi.File.Extension}"
            };

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.Filepath))
                {
                    File.Copy(CommonConst.LONG_PATH_PREFIX + fi.Filepath, CommonConst.LONG_PATH_PREFIX + dlg.FileName, true);
                }
                else if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath))
                {
                    File.Copy(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath, dlg.FileName, true);
                }
            }
        }
    }

    private static bool CanSaveAsImage(object fileImage)
    {
        if (fileImage is FileImage fi == false)
            return false;

        if (File.Exists(CommonConst.LONG_PATH_PREFIX + fi.Filepath) == false && File.Exists(CommonConst.LONG_PATH_PREFIX + fi.BackupFilePath) == false)
            return false;

        return true;
    }
    #endregion SaveAsImage

    #region RestoreSelectedImages
    private ICommand _restoreSelectedImagesCommand;
    public ICommand RestoreSelectedImagesCommand
    {
        get
        {
            if (_restoreSelectedImagesCommand == null)
            {
                _restoreSelectedImagesCommand = new RelayCommand(p => RestoreSelectedImages(p),
                    p => CanRestoreSelectedImages(p));
            }
            return _restoreSelectedImagesCommand;
        }
    }

    public static void RestoreSelectedImages(object selectedRows)
    {
        List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();

        var deletedOnes = icrs.Select(a => a.FileA).Concat(icrs.Select(a => a.FileB))
                            .DistinctBy(a => a.Filepath).Where(a => File.Exists(CommonConst.LONG_PATH_PREFIX + a.Filepath) == false);

        foreach (var restoreMe in deletedOnes)
        {
            if (File.Exists(CommonConst.LONG_PATH_PREFIX + restoreMe.BackupFilePath))
            {
                File.Copy(CommonConst.LONG_PATH_PREFIX + restoreMe.BackupFilePath, CommonConst.LONG_PATH_PREFIX + restoreMe.Filepath, true);
            }
        }
    }

    private static bool CanRestoreSelectedImages(object selectedRows)
    {
        List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();

        if (icrs.Count == 0)
            return false;

        return true;
    }
    #endregion RestoreSelectedImages
    #endregion Commands

    #region IDisposable
    public void Dispose()
    {
        _preLoadImagesCancelTokenSource?.Cancel();

        SelectedResult = null;

        PropertyChanged -= VMResultView_PropertyChanged;

        foreach (var result in Results)
        {
            result.FileA.Dispose();
            result.FileB.Dispose();
            result.ImageLoadStarted = false;
        }
        Results.Clear();

        _isDisposed = true;
    }
    #endregion IDisposable
}
