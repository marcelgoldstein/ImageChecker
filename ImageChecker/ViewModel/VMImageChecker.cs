using ImageChecker.DataClass;
using ImageChecker.Factory;
using ImageChecker.Helper;
using ImageChecker.Processing;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shell;

namespace ImageChecker.ViewModel;

public sealed class VMImageChecker : ViewModelBase, IDisposable
{
    #region Const
    private const string PROJECT_PAGE_URL = "https://github.com/marcelgoldstein/ImageChecker";
    #endregion

    #region Properties
    #region Window
    public static string WindowTitle { get { return $"{Assembly.GetEntryAssembly().GetName().Name} v{Assembly.GetEntryAssembly().GetName().Version}"; } }
    public static string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }

    private TaskbarItemInfo _windowTaskbarInfo;
    public TaskbarItemInfo WindowTaskbarInfo
    {
        get { if (_windowTaskbarInfo == null) _windowTaskbarInfo = new TaskbarItemInfo(); return _windowTaskbarInfo; }
        set => _windowTaskbarInfo = value;
    }
    #endregion

    #region Folderselect
    private ObservableCollection<DirectoryInfo> _folders;
    public ObservableCollection<DirectoryInfo> Folders
    {
        get
        {
            if (_folders == null)
                _folders = new ObservableCollection<DirectoryInfo>();

            return _folders;
        }
        set
        {
            if (_folders != value)
            {
                _folders = value;
                RaisePropertyChanged(nameof(Folders));
            }
        }
    }

    private DirectoryInfo _selectedFolder;
    public DirectoryInfo SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (_selectedFolder != value)
            {
                _selectedFolder = value;
                RaisePropertyChanged(nameof(SelectedFolder));
            }
        }
    }

    private bool _includeSubdirectories = true;
    public bool IncludeSubdirectories
    {
        get => _includeSubdirectories;
        set
        {
            if (_includeSubdirectories != value)
            {
                _includeSubdirectories = value;
                RaisePropertyChanged(nameof(IncludeSubdirectories));
            }

        }
    }
    #endregion

    #region ProgressBar
    private double _progressMinimum = 0;
    public double ProgressMinimum
    {
        get => _progressMinimum;
        set
        {
            if (_progressMinimum != value)
            {
                _progressMinimum = value;
                RaisePropertyChanged(nameof(ProgressMinimum));
            }
        }
    }

    private double _progressMaximum = 100;
    public double ProgressMaximum
    {
        get => _progressMaximum;
        set
        {
            if (_progressMaximum != value)
            {
                _progressMaximum = value;
                RaisePropertyChanged(nameof(ProgressMaximum));
            }
        }
    }

    private double _progressValue = 0;
    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            if (_progressValue != value)
            {
                _progressValue = value;
                RaisePropertyChanged(nameof(ProgressValue));
            }
        }
    }

    private string _progressText = "";
    public string ProgressText
    {
        get => _progressText;
        set
        {
            if (_progressText != value)
            {
                _progressText = value;
                RaisePropertyChanged(nameof(ProgressText));
            }
        }
    }

    private string _progressTask = "";
    public string ProgressTask
    {
        get => _progressTask;
        set
        {
            if (_progressTask != value)
            {
                _progressTask = value;
                RaisePropertyChanged(nameof(ProgressTask));
            }
        }
    }

    private long? _estimatedRemainingSeconds;
    public long? EstimatedRemainingSeconds
    {
        get => _estimatedRemainingSeconds;
        set => SetProperty(ref _estimatedRemainingSeconds, value);
    }

    #endregion

    #region Workers
    private WorkerRenameFiles _workerRenameFiles;
    public WorkerRenameFiles WorkerRenameFiles
    {
        get
        {
            if (_workerRenameFiles == null)
                _workerRenameFiles = new WorkerRenameFiles();

            return _workerRenameFiles;
        }
        set
        {
            if (_workerRenameFiles != value)
            {
                _workerRenameFiles = value;
                RaisePropertyChanged(nameof(WorkerRenameFiles));
            }
        }
    }

    private WorkerImageComparison _workerImageComparison;
    public WorkerImageComparison WorkerImageComparison
    {
        get
        {
            if (_workerImageComparison == null)
                _workerImageComparison = new WorkerImageComparison();

            return _workerImageComparison;
        }
        set
        {
            if (_workerImageComparison != value)
            {
                _workerImageComparison = value;
                RaisePropertyChanged(nameof(WorkerImageComparison));
            }
        }
    }
    #endregion

    #region ImageOptionen
    private List<Tuple<int, string>> _preScaleOptions;
    public List<Tuple<int, string>> PreScaleOptions
    {
        get
        {
            if (_preScaleOptions == null)
            {
                _preScaleOptions = new List<Tuple<int, string>>()
                {
                    //new Tuple<int, string>(1, "64 pixels"),
                    new Tuple<int, string>(2, "128 pixels"),
                    new Tuple<int, string>(3, "256 pixels"),
                    new Tuple<int, string>(4, "512 pixels"),
                    new Tuple<int, string>(0, "original"),
                };
            }
            return _preScaleOptions;
        }
        set => SetProperty(ref _preScaleOptions, value);
    }

    private Tuple<int, string> _selectedPreScaleOption;
    public Tuple<int, string> SelectedPreScaleOption
    {
        get => _selectedPreScaleOption;
        set => SetProperty(ref _selectedPreScaleOption, value);
    }

    private double _threshold;
    public double Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value);
    }

    #endregion
    #endregion

    #region ctor
    public VMImageChecker()
    {
        SelectedPreScaleOption = PreScaleOptions[0];
        Threshold = 40;

        WorkerRenameFiles.PropertyChanged += RenameFilesWorker_PropertyChanged;
        WorkerRenameFiles.RenamingProgress.ProgressChanged += RenamingProgress_ProgressChanged;

        WorkerImageComparison.PropertyChanged += WorkerImageComparison_PropertyChanged;
        WorkerImageComparison.ImageComparisonProgress.ProgressChanged += ImageComparisonProgress_ProgressChanged;

        Task.Run(async () =>
        {
            while (true)
            {
                WorkerImageComparison.RefreshSelectedPossibleDuplicatesCount(Threshold);

                await Task.Delay(200);
            }
        });

        PropertyChanged += VMImageChecker_PropertyChanged;
    }
    #endregion

    #region PropertyChanged
    private void VMImageChecker_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IncludeSubdirectories):
                WorkerImageComparison.PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();
                break;
        }
    }
    #endregion

    #region Events
    #region Worker
    #region WorkerRenameFiles
    void RenamingProgress_ProgressChanged(object sender, ProgressRenamingFiles e)
    {
        ProgressMinimum = e.Minimum;
        ProgressMaximum = e.Maximum;
        ProgressValue = e.Value;
        UpdateProgressTextRenameFiles(e);

        WindowTaskbarInfo.Dispatcher.Invoke(() =>
        {
            if (WorkerRenameFiles.CtsRenameFiles.IsCancellationRequested)
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Indeterminate; // normal, weil ein cancel mehr so ein stop bedeutet
            }
            else if (WorkerRenameFiles.PtsRenameFiles.IsPauseRequested)
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            }

            WindowTaskbarInfo.ProgressValue = e.Value / e.Maximum;
        });
    }

    void RenameFilesWorker_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "KeepOriginalNames":
                WorkerRenameFiles.Loop = WorkerRenameFiles.KeepOriginalNames && WorkerRenameFiles.Loop;
                break;
            case "Loop":
                WorkerRenameFiles.LoopEndless = WorkerRenameFiles.Loop && WorkerRenameFiles.LoopEndless;
                break;
            case "IsRenamingFiles":
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });

                break;
            default:
                break;
        }
    }
    #endregion

    #region WorkerImageComparison
    void ImageComparisonProgress_ProgressChanged(object sender, ProgressImageComparison e)
    {
        ProgressMinimum = e.Minimum;
        ProgressMaximum = e.Maximum;
        ProgressValue = e.Value;
        EstimatedRemainingSeconds = e.EstimatedRemainingSeconds;
        UpdateProgressTextImageComparison(e);

        WindowTaskbarInfo.Dispatcher.Invoke(() =>
        {
            if (WorkerImageComparison.CtsImageComparison.IsCancellationRequested)
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Error;
            }
            else if (WorkerImageComparison.PtsImageComparison.IsPauseRequested)
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
            }

            WindowTaskbarInfo.ProgressValue = e.Value / e.Maximum;
        });
    }

    void WorkerImageComparison_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "IsComparingImages":
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
                break;
            case "HasErrorFiles":
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CommandManager.InvalidateRequerySuggested();
                });
                break;
            default:
                break;
        }
    }
    #endregion
    #endregion
    #endregion

    #region Methods
    private void UpdateProgressTextImageComparison(ProgressImageComparison pic)
    {
        string strErs = string.Empty;
        string strElapsedTime = string.Empty;

        if (pic.EstimatedRemainingSeconds != null)
        {
            strErs = string.Format("      finished in: {0:00}:{1:00}:{2:00}", pic.EstimatedRemainingSeconds / 3600, (pic.EstimatedRemainingSeconds / 60) % 60, pic.EstimatedRemainingSeconds % 60);
        }

        if (pic.TotalRunningSeconds != null)
        {
            strElapsedTime = string.Format("      elapsed: {0:00}:{1:00}:{2:00}", pic.TotalRunningSeconds / 3600, (pic.TotalRunningSeconds / 60) % 60, pic.TotalRunningSeconds % 60);
        }


        ProgressText = string.Format("{2}   {0} / {1}{3}{4}", pic.Value, pic.Maximum, pic.Operation, strErs, strElapsedTime);
    }

    private void UpdateProgressTextRenameFiles(ProgressRenamingFiles prf)
    {
        ProgressText = string.Format("{2}   {0} / {1}", prf.Value, prf.Maximum, prf.Operation);
    }

    internal void AddFolder(string dropedPath)
    {
        // unterscheidung auf datei oder ordner
        if (File.GetAttributes(dropedPath).HasFlag(FileAttributes.Directory))
        { // ordner
            DirectoryInfo dirInfo = new DirectoryInfo(dropedPath);
            if (dirInfo.Exists)
            {
                if (!Folders.Any(a => a.FullName == dirInfo.FullName))
                {
                    Folders.Add(dirInfo);
                }
            }
        }
        else
        { // datei
            DirectoryInfo dirInfo = new DirectoryInfo(Path.GetDirectoryName(dropedPath));
            if (dirInfo.Exists)
            {
                if (!Folders.Any(a => a.FullName == dirInfo.FullName))
                {
                    Folders.Add(dirInfo);
                }
            }
        }

        WorkerImageComparison.PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();
    }

    private void RemoveFolder(DirectoryInfo folder)
    {
        Folders.Remove(folder);
        WorkerImageComparison.PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();
    }

    private void ClearFolders()
    {
        Folders.Clear();
        WorkerImageComparison.PossibleDuplicates = new ConcurrentBag<ImageCompareResult>();
    }

    #region CompareImages

    #endregion
    #endregion

    #region Commands
    #region Menu
    #region OpenProjectPage
    private ICommand _openProjectPageCommand;
    public ICommand OpenProjectPageCommand
    {
        get
        {
            if (_openProjectPageCommand == null)
            {
                _openProjectPageCommand = new RelayCommand(p => OpenProjectPage(), p => CanOpenProjectPage());
            }
            return _openProjectPageCommand;
        }
    }

    public static void OpenProjectPage()
    {
        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = PROJECT_PAGE_URL,
            UseShellExecute = true
        };
        Process.Start(psi);
    }

    private static bool CanOpenProjectPage()
    {
        return true;
    }
    #endregion OpenProjectPage
    #endregion Menu

    #region Folderselect
    #region DropFolder
    private ICommand _dropFolderCommand;
    public ICommand DropFolderCommand
    {
        get
        {
            if (_dropFolderCommand == null)
            {
                _dropFolderCommand = new RelayCommand(p => DropFolder(p), p => CanDropFolder());
            }
            return _dropFolderCommand;
        }
    }

    public void DropFolder(object inObject)
    {
        if (CanDropFolder() == false)
            return; // because the CanExecute does not get invoke while dragging, the Execute needs to be prevented when CanExecute returns false

        if (inObject is not System.Windows.IDataObject ido) return;

        if (ido.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            string[] data = (string[])ido.GetData(System.Windows.DataFormats.FileDrop);

            for (int i = 0; i < (int)data.Length; i++)
            {
                AddFolder(data[i]);
            }
        }
    }

    private bool CanDropFolder()
    {
        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        if (WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region AddFolder
    private ICommand _openFolderAddDialogCommand;
    public ICommand OpenFolderAddDialogCommand
    {
        get
        {
            if (_openFolderAddDialogCommand == null)
            {
                _openFolderAddDialogCommand = new RelayCommand(param => OpenFolderAddDialog(),
                    param => CanOpenFolderAddDialog());
            }
            return _openFolderAddDialogCommand;
        }
    }

    public void OpenFolderAddDialog()
    {
        // Create a "Save As" dialog for selecting a directory (HACK)
        var dialog = new SaveFileDialog
        {
            Title = "Select a Directory", // instead of default "Save As"
            Filter = "Directory|*.this.directory", // Prevents displaying files
            FileName = "select" // Filename will then be "select.this.directory"
        };
        if (dialog.ShowDialog() == true)
        {
            string path = dialog.FileName;
            // Remove fake filename from resulting path
            path = path.Replace("\\select.this.directory", "");
            path = path.Replace(".this.directory", "");

            AddFolder(path);
        }
    }

    private bool CanOpenFolderAddDialog()
    {
        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        if (WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region RemoveFolder
    private ICommand _removeFolderCommand;
    public ICommand RemoveFolderCommand
    {
        get
        {
            if (_removeFolderCommand == null)
            {
                _removeFolderCommand = new RelayCommand(param => RemoveFolder(),
                    param => CanRemoveFolder());
            }
            return _removeFolderCommand;
        }
    }

    private void RemoveFolder()
    {
        if (SelectedFolder != null)
        {
            RemoveFolder(SelectedFolder);
        }
    }

    private bool CanRemoveFolder()
    {
        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        if (WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region ClearFolder
    private ICommand _clearFoldersCommand;
    public ICommand ClearFoldersCommand
    {
        get
        {
            if (_clearFoldersCommand == null)
            {
                _clearFoldersCommand = new RelayCommand(param => ClearFolders(),
                    param => CanClearFolder());
            }
            return _clearFoldersCommand;
        }
    }

    private bool CanClearFolder()
    {
        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        if (WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion
    #endregion

    #region RenameFiles
    #region StartRenameCommand
    private ICommand _startRenameCommand;
    public ICommand StartRenameCommand
    {
        get
        {
            if (_startRenameCommand == null)
            {
                _startRenameCommand = new RelayCommand(param => StartRename(),
                    param => CanStartRename());
            }
            return _startRenameCommand;
        }
    }

    private void StartRename()
    {
        var t = Task.Run(async () =>
        {
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            });
            await WorkerRenameFiles.RenameFilesAsync(Folders, IncludeSubdirectories);
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.None;
            });
        });
    }

    private bool CanStartRename()
    {
        if (WorkerImageComparison.IsComparingImages)
            return false;

        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        return true;
    }
    #endregion

    #region PauseRenameCommand
    private ICommand _pauseRenameCommand;
    public ICommand PauseRenameCommand
    {
        get
        {
            if (_pauseRenameCommand == null)
            {
                _pauseRenameCommand = new RelayCommand(param => PauseUnpauseRenaming(param as bool?),
                    param => CanPauseRenaming());
            }
            return _pauseRenameCommand;
        }
    }

    private void PauseUnpauseRenaming(bool? isPause)
    {
        if (isPause ?? false)
        {
            UnpauseRenaming();
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            });
        }
        else
        {
            PauseRenaming();
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Paused;
            });
        }
    }

    private void PauseRenaming()
    {
        WorkerRenameFiles.PtsRenameFiles.Pause();
        WorkerRenameFiles.IsRenamingFilesPaused = true;
    }

    private void UnpauseRenaming()
    {
        WorkerRenameFiles.PtsRenameFiles.Unpause();
        WorkerRenameFiles.IsRenamingFilesPaused = false;
    }

    private bool CanPauseRenaming()
    {
        if (!WorkerRenameFiles.IsRenamingFiles)
            return false;

        return true;
    }
    #endregion

    #region CancelRenameCommand
    private ICommand _cancelRenameCommand;
    public ICommand CancelRenameCommand
    {
        get
        {
            if (_cancelRenameCommand == null)
            {
                _cancelRenameCommand = new RelayCommand(param => CancelRenaming(),
                    param => CanCancelRenaming());
            }
            return _cancelRenameCommand;
        }
    }

    private void CancelRenaming()
    {
        WorkerRenameFiles.CtsRenameFiles.Cancel();

        if (WorkerRenameFiles.IsRenamingFilesPaused)
        {
            UnpauseRenaming();
        }

        WindowTaskbarInfo.Dispatcher.Invoke(() =>
        {
            WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.None;
        });
    }

    private bool CanCancelRenaming()
    {
        if (!WorkerRenameFiles.IsRenamingFiles)
            return false;

        return true;
    }
    #endregion
    #endregion

    #region ImageComparison
    #region StartImageComparisonCommand
    private ICommand _startImageComparisonCommand;
    public ICommand StartImageComparisonCommand
    {
        get
        {
            if (_startImageComparisonCommand == null)
            {
                _startImageComparisonCommand = new RelayCommand(param => StartImageComparison(),
                    param => CanStartImageComparison());
            }
            return _startImageComparisonCommand;
        }
    }

    private async void StartImageComparison()
    {
        WorkerImageComparison.Folders = Folders.ToList();
        WorkerImageComparison.IncludeSubdirectories = IncludeSubdirectories;

        switch (SelectedPreScaleOption.Item1)
        {
            case 0: // original
                WorkerImageComparison.PreResizeImages = false;
                break;
            case 1: // 64
                WorkerImageComparison.PreResizeImages = true;
                WorkerImageComparison.PreResizeScale = 64;
                break;
            case 2: // 128
                WorkerImageComparison.PreResizeImages = true;
                WorkerImageComparison.PreResizeScale = 128;
                break;
            case 3: // 256
                WorkerImageComparison.PreResizeImages = true;
                WorkerImageComparison.PreResizeScale = 256;
                break;
            case 4: // 512
                WorkerImageComparison.PreResizeImages = true;
                WorkerImageComparison.PreResizeScale = 512;
                break;
            default:
                break;
        }

        WorkerImageComparison.Threshold = 7D;

        WindowTaskbarInfo.Dispatcher.Invoke(() =>
        {
            WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
        });

        await WorkerImageComparison.Start();
    }

    private bool CanStartImageComparison()
    {
        if (WorkerRenameFiles.IsRenamingFiles)
            return false;

        if (WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region PauseImageComparisonCommand
    private ICommand _pauseImageComparisonCommand;
    public ICommand PauseImageComparisonCommand
    {
        get
        {
            if (_pauseImageComparisonCommand == null)
            {
                _pauseImageComparisonCommand = new RelayCommand(param => PauseUnpauseImageComparison(param as bool?),
                    param => CanPauseImageComparison());
            }
            return _pauseImageComparisonCommand;
        }
    }

    private void PauseUnpauseImageComparison(bool? isPause)
    {
        if (isPause ?? false)
        {
            UnpauseImageComparison();
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Normal;
            });
        }
        else
        {
            PauseImageComparison();
            WindowTaskbarInfo.Dispatcher.Invoke(() =>
            {
                WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Paused;
            });
        }
    }

    private void PauseImageComparison()
    {
        WorkerImageComparison.PtsImageComparison.Pause();
        WorkerImageComparison.IsComparingImagesPaused = true;
    }

    private void UnpauseImageComparison()
    {
        WorkerImageComparison.PtsImageComparison.Unpause();
        WorkerImageComparison.IsComparingImagesPaused = false;
    }

    private bool CanPauseImageComparison()
    {
        if (!WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region CancelImageComparisonCommand
    private ICommand _cancelImageComparisonCommand;
    public ICommand CancelImageComparisonCommand
    {
        get
        {
            if (_cancelImageComparisonCommand == null)
            {
                _cancelImageComparisonCommand = new RelayCommand(param => CancelImageComparison(),
                    param => CanCancelImageComparison());
            }
            return _cancelImageComparisonCommand;
        }
    }

    private void CancelImageComparison()
    {
        WorkerImageComparison.CtsImageComparison.Cancel();

        if (WorkerImageComparison.IsComparingImagesPaused)
        {
            UnpauseImageComparison();
        }

        WindowTaskbarInfo.Dispatcher.Invoke(() =>
        {
            WindowTaskbarInfo.ProgressState = TaskbarItemProgressState.Error;
        });
    }

    private bool CanCancelImageComparison()
    {
        if (!WorkerImageComparison.IsComparingImages)
            return false;

        return true;
    }
    #endregion

    #region ShowErrorFilesCommand
    private ICommand _showErrorFilesCommand;
    public ICommand ShowErrorFilesCommand
    {
        get
        {
            if (_showErrorFilesCommand == null)
            {
                _showErrorFilesCommand = new RelayCommand(param => ShowErrorFiles(),
                    param => CanShowErrorFiles());
            }
            return _showErrorFilesCommand;
        }
    }

    private async void ShowErrorFiles()
    {
        var vm = new VMErrorFiles(WorkerImageComparison.ErrorFiles);

        void onClosed(object sender, EventArgs args)
        {
            WorkerImageComparison.ErrorFiles = new ConcurrentBag<string>(WorkerImageComparison.ErrorFiles.Where(a => File.Exists(a)));
            vm.Dispose();
        }

        await WindowService.OpenWindow(vm, true, null, onClosed);
    }

    private static bool CanShowErrorFiles()
    {
        return true;
    }
    #endregion

    #region ShowResults
    private ICommand _showResultsCommand;
    public ICommand ShowResultsCommand
    {
        get
        {
            if (_showResultsCommand == null)
            {
                _showResultsCommand = new RelayCommand(async param => await ShowResults(),
                    param => CanShowResults());
            }
            return _showResultsCommand;
        }
    }

    private async Task ShowResults()
    {
        var vm = new VMResultView(WorkerImageComparison.PossibleDuplicates.Where(a => a.FLANN >= Threshold), WorkerImageComparison.Files.Count);

        void onClosed(object sender, EventArgs args)
        {
            vm.Dispose();
        }

        await WindowService.OpenWindow(vm, true, null, onClosed);
    }

    private bool CanShowResults()
    {
        if (WorkerImageComparison.IsComparingImages)
            return false;

        if (!WorkerImageComparison.HasPossibleDuplicates)
            return false;

        if (WorkerImageComparison.SelectedPossibleDuplicatesCount == 0)
            return false;

        return true;
    }
    #endregion
    #endregion
    #endregion

    #region IDisposable
    public void Dispose()
    {
        if (_workerImageComparison != null)
        {
            _workerImageComparison.Dispose();
            _workerImageComparison = null;
        }

        if (_workerRenameFiles != null)
        {
            _workerRenameFiles.Dispose();
            _workerRenameFiles = null;
        }

        PropertyChanged -= VMImageChecker_PropertyChanged;
    }
    #endregion
}
