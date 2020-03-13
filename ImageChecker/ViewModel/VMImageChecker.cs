using ImageChecker.Factory;
using ImageChecker.Helper;
using ImageChecker.Processing;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Shell;

namespace ImageChecker.ViewModel
{
    public class VMImageChecker : ViewModelBase, IDisposable
    {
        #region Properties
        #region Window
        public string WindowTitle { get { return "ImageChecker"; } }
        public string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }

        private TaskbarItemInfo windowTaskbarInfo;
        public TaskbarItemInfo WindowTaskbarInfo
        {
            get { if (windowTaskbarInfo == null) windowTaskbarInfo = new TaskbarItemInfo(); return windowTaskbarInfo; }
            set { windowTaskbarInfo = value; }
        }
        #endregion

        #region Folderselect
        private ObservableCollection<DirectoryInfo> folders;
        public ObservableCollection<DirectoryInfo> Folders
        {
            get
            {
                if (folders == null)
                    folders = new ObservableCollection<DirectoryInfo>();

                return folders;
            }
            set
            {
                if (folders != value)
                {
                    folders = value;
                    RaisePropertyChanged("Folders");
                }
            }
        }

        private DirectoryInfo selectedFolder;
        public DirectoryInfo SelectedFolder
        {
            get
            {
                return selectedFolder;
            }
            set
            {
                if (selectedFolder != value)
                {
                    selectedFolder = value;
                    RaisePropertyChanged("SelectedFolder");
                }
            }
        }

        private bool includeSubdirectories = true;
        public bool IncludeSubdirectories
        {
            get
            {
                return includeSubdirectories;
            }
            set
            {
                if (includeSubdirectories != value)
                {
                    includeSubdirectories = value;
                    RaisePropertyChanged("IncludeSubdirectories");
                }

            }
        }
        #endregion

        #region ProgressBar
        private double progressMinimum = 0;
        public double ProgressMinimum
        {
            get
            {
                return progressMinimum;
            }
            set
            {
                if (progressMinimum != value)
                {
                    progressMinimum = value;
                    RaisePropertyChanged("ProgressMinimum");
                }
            }
        }

        private double progressMaximum = 100;
        public double ProgressMaximum
        {
            get
            {
                return progressMaximum;
            }
            set
            {
                if (progressMaximum != value)
                {
                    progressMaximum = value;
                    RaisePropertyChanged("ProgressMaximum");
                }
            }
        }

        private double progressValue = 0;
        public double ProgressValue
        {
            get
            {
                return progressValue;
            }
            set
            {
                if (progressValue != value)
                {
                    progressValue = value;
                    RaisePropertyChanged("ProgressValue");
                }
            }
        }

        private string progressText = "";
        public string ProgressText
        {
            get
            {
                return progressText;
            }
            set
            {
                if (progressText != value)
                {
                    progressText = value;
                    RaisePropertyChanged("ProgressText");
                }
            }
        }

        private string progressTask = "";
        public string ProgressTask
        {
            get
            {
                return progressTask;
            }
            set
            {
                if (progressTask != value)
                {
                    progressTask = value;
                    RaisePropertyChanged("ProgressTask");
                }
            }
        }

        private long? estimatedRemainingSeconds;
        public long? EstimatedRemainingSeconds
        {
            get { return estimatedRemainingSeconds; }
            set { SetProperty(ref estimatedRemainingSeconds, value); }
        }

        #endregion

        #region Workers
        private WorkerRenameFiles workerRenameFiles;
        public WorkerRenameFiles WorkerRenameFiles
        {
            get
            {
                if (workerRenameFiles == null)
                    workerRenameFiles = new WorkerRenameFiles();

                return workerRenameFiles;
            }
            set
            {
                if (workerRenameFiles != value)
                {
                    workerRenameFiles = value;
                    RaisePropertyChanged("WorkerRenameFiles");
                }
            }
        }

        private WorkerImageComparison workerImageComparison;
        public WorkerImageComparison WorkerImageComparison
        {
            get
            {
                if (workerImageComparison == null)
                    workerImageComparison = new WorkerImageComparison();

                return workerImageComparison;
            }
            set
            {
                if (workerImageComparison != value)
                {
                    workerImageComparison = value;
                    RaisePropertyChanged("WorkerImageComparison");
                }
            }
        }
        #endregion

        #region ImageOptionen
        private List<Tuple<int, string>> preScaleOptions;
        public List<Tuple<int, string>> PreScaleOptions
        {
            get
            {
                if (preScaleOptions == null)
                {
                    preScaleOptions = new List<Tuple<int, string>>()
                    {
                        //new Tuple<int, string>(1, "64 pixels"),
                        new Tuple<int, string>(2, "128 pixels"),
                        new Tuple<int, string>(3, "256 pixels"),
                        new Tuple<int, string>(4, "512 pixels"),
                        new Tuple<int, string>(0, "original"),
                    };
                }
                return preScaleOptions;
            }
            set { SetProperty(ref preScaleOptions, value); }
        }

        private Tuple<int, string> selectedPreScaleOption;
        public Tuple<int, string> SelectedPreScaleOption
        {
            get { return selectedPreScaleOption; }
            set { SetProperty(ref selectedPreScaleOption, value); }
        }

        private double threshold;
        public double Threshold
        {
            get { return threshold; }
            set { SetProperty(ref threshold, value); }
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
                    this.WorkerImageComparison.RefreshSelectedPossibleDuplicatesCount(this.Threshold);

                    await Task.Delay(200);
                }
            });

            this.PropertyChanged += VMImageChecker_PropertyChanged;
        }
        #endregion

        #region PropertyChanged
        private void VMImageChecker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

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
                    WorkerRenameFiles.Loop = WorkerRenameFiles.KeepOriginalNames ? WorkerRenameFiles.Loop : false;
                    break;
                case "Loop":
                    WorkerRenameFiles.LoopEndless = WorkerRenameFiles.Loop ? WorkerRenameFiles.LoopEndless : false;
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
        }

        private void RemoveFolder(DirectoryInfo folder)
        {
            Folders.Remove(folder);
        }

        private void ClearFolders()
        {
            Folders.Clear();
        }

        #region CompareImages

        #endregion
        #endregion

        #region Commands
        #region Folderselect
        #region DropFolder
        private ICommand dropFolderCommand;
        public ICommand DropFolderCommand
        {
            get
            {
                if (dropFolderCommand == null)
                {
                    dropFolderCommand = new RelayCommand(param => DropFolder(param));
                }
                return dropFolderCommand;
            }
        }

        public void DropFolder(object inObject)
        {
            System.Windows.IDataObject ido = inObject as System.Windows.IDataObject;
            if (null == ido) return;

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
            return true;
        }
        #endregion

        #region AddFolder
        private ICommand openFolderAddDialogCommand;
        public ICommand OpenFolderAddDialogCommand
        {
            get
            {
                if (openFolderAddDialogCommand == null)
                {
                    openFolderAddDialogCommand = new RelayCommand(param => OpenFolderAddDialog(),
                        param => CanOpenFolderAddDialog());
                }
                return openFolderAddDialogCommand;
            }
        }

        public void OpenFolderAddDialog()
        {
            // Create a "Save As" dialog for selecting a directory (HACK)
            var dialog = new SaveFileDialog();
            dialog.Title = "Select a Directory"; // instead of default "Save As"
            dialog.Filter = "Directory|*.this.directory"; // Prevents displaying files
            dialog.FileName = "select"; // Filename will then be "select.this.directory"
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
            return true;
        }
        #endregion

        #region RemoveFolder
        private ICommand removeFolderCommand;
        public ICommand RemoveFolderCommand
        {
            get
            {
                if (removeFolderCommand == null)
                {
                    removeFolderCommand = new RelayCommand(param => RemoveFolder(),
                        param => CanRemoveFolder());
                }
                return removeFolderCommand;
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
            return true;
        }
        #endregion

        #region ClearFolder
        private ICommand clearFoldersCommand;
        public ICommand ClearFoldersCommand
        {
            get
            {
                if (clearFoldersCommand == null)
                {
                    clearFoldersCommand = new RelayCommand(param => ClearFolders(),
                        param => CanClearFolder());
                }
                return clearFoldersCommand;
            }
        }

        private void ClearFolder()
        {
            ClearFolders();
        }

        private bool CanClearFolder()
        {
            return true;
        }
        #endregion
        #endregion

        #region RenameFiles
        #region StartRenameCommand
        private ICommand startRenameCommand;
        public ICommand StartRenameCommand
        {
            get
            {
                if (startRenameCommand == null)
                {
                    startRenameCommand = new RelayCommand(param => StartRename(),
                        param => CanStartRename());
                }
                return startRenameCommand;
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
        private ICommand pauseRenameCommand;
        public ICommand PauseRenameCommand
        {
            get
            {
                if (pauseRenameCommand == null)
                {
                    pauseRenameCommand = new RelayCommand(param => PauseUnpauseRenaming(param as bool?),
                        param => CanPauseRenaming());
                }
                return pauseRenameCommand;
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
        private ICommand cancelRenameCommand;
        public ICommand CancelRenameCommand
        {
            get
            {
                if (cancelRenameCommand == null)
                {
                    cancelRenameCommand = new RelayCommand(param => CancelRenaming(),
                        param => CanCancelRenaming());
                }
                return cancelRenameCommand;
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
        private ICommand startImageComparisonCommand;
        public ICommand StartImageComparisonCommand
        {
            get
            {
                if (startImageComparisonCommand == null)
                {
                    startImageComparisonCommand = new RelayCommand(param => StartImageComparison(),
                        param => CanStartImageComparison());
                }
                return startImageComparisonCommand;
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
        private ICommand pauseImageComparisonCommand;
        public ICommand PauseImageComparisonCommand
        {
            get
            {
                if (pauseImageComparisonCommand == null)
                {
                    pauseImageComparisonCommand = new RelayCommand(param => PauseUnpauseImageComparison(param as bool?),
                        param => CanPauseImageComparison());
                }
                return pauseImageComparisonCommand;
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
        private ICommand cancelImageComparisonCommand;
        public ICommand CancelImageComparisonCommand
        {
            get
            {
                if (cancelImageComparisonCommand == null)
                {
                    cancelImageComparisonCommand = new RelayCommand(param => CancelImageComparison(),
                        param => CanCancelImageComparison());
                }
                return cancelImageComparisonCommand;
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
        private ICommand showErrorFilesCommand;
        public ICommand ShowErrorFilesCommand
        {
            get
            {
                if (showErrorFilesCommand == null)
                {
                    showErrorFilesCommand = new RelayCommand(param => ShowErrorFiles(),
                        param => CanShowErrorFiles());
                }
                return showErrorFilesCommand;
            }
        }

        private async void ShowErrorFiles()
        {
            var vm = new VMErrorFiles(WorkerImageComparison.ErrorFiles);

            Action<object, EventArgs> onClosed = (sender, args) =>
            {
                WorkerImageComparison.ErrorFiles = new ConcurrentBag<string>(WorkerImageComparison.ErrorFiles.Where(a => File.Exists(a)));
                vm.Dispose();
            };

            await WindowService.OpenWindow(vm, true, null, onClosed);
        }

        private bool CanShowErrorFiles()
        {
            return true;
        }
        #endregion

        #region ShowResults
        private ICommand showResultsCommand;
        public ICommand ShowResultsCommand
        {
            get
            {
                if (showResultsCommand == null)
                {
                    showResultsCommand = new RelayCommand(async param => await ShowResults(),
                        param => CanShowResults());
                }
                return showResultsCommand;
            }
        }

        private async Task ShowResults()
        {
            var vm = new VMResultView(WorkerImageComparison.PossibleDuplicates.Where(a => a.FLANN >= Threshold), WorkerImageComparison.Files.Count);

            Action<object, EventArgs> onClosed = (sender, args) =>
            {
                vm.Dispose();
            };

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
            if (workerImageComparison != null)
            {
                workerImageComparison.Dispose();
                workerImageComparison = null;
            }

            if (workerRenameFiles != null)
            {
                workerRenameFiles.Dispose();
                workerRenameFiles = null;
            }

            this.PropertyChanged -= VMImageChecker_PropertyChanged;
        }
        #endregion
    }
}
