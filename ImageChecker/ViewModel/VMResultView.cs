using ImageChecker.DataClass;
using ImageChecker.Helper;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ImageChecker.ViewModel
{
    public class VMResultView : ViewModelBase, IDisposable
    {
        #region const
        private readonly int preLoadRange = 10;
        #endregion const

        #region fields
        private bool isDisposed = false;

        private static SemaphoreSlim cancelSlimmy = new SemaphoreSlim(1);
        private static SemaphoreSlim workingSlimmy = new SemaphoreSlim(1);
        #endregion fields

        #region Properties
        private CancellationTokenSource preLoadImagesCancelTokenSource;

        #region Window
        public string WindowTitle { get { return "ResultView"; } }
        public string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }
        #endregion Window

        private RangeObservableCollection<ImageCompareResult> results = new RangeObservableCollection<ImageCompareResult>();
        public RangeObservableCollection<ImageCompareResult> Results
        {
            get
            {
                return results;
            }
            set
            {
                SetProperty(ref results, value);
            }
        }

        private CollectionViewSource resultsView;

        public CollectionViewSource ResultsView
        {
            get { if (resultsView == null) { resultsView = new CollectionViewSource(); resultsView.Source = Results; } return resultsView; }
            set { SetProperty(ref resultsView, value); }
        }

        private IList selectedResults;
        public IList SelectedResults
        {
            get
            {
                if (selectedResults == null)
                    selectedResults = new List<object>();

                return selectedResults;
            }
            set
            {
                SetProperty(ref selectedResults, value);
            }
        }

        private ImageCompareResult selectedResult;
        public ImageCompareResult SelectedResult
        {
            get
            {
                return selectedResult;
            }
            set
            {
                SetProperty(ref selectedResult, value);
            }
        }

        private bool isExterminationModeActive;

        public bool IsExterminationModeActive
        {
            get { return isExterminationModeActive; }
            set { SetProperty(ref isExterminationModeActive, value); }
        }

        #region Filters
        private bool filterActivated;
        public bool FilterActivated
        {
            get { return filterActivated; }
            set { SetProperty(ref filterActivated, value); }
        }

        private string fileFilter;
        public string FileFilter
        {
            get { return fileFilter; }
            set { SetProperty(ref fileFilter, value); }
        }

        private List<StatusFilter> statusFilters;
        public List<StatusFilter> StatusFilters
        {
            get { if (statusFilters == null) { statusFilters = new List<StatusFilter>() { new StatusFilter(1, "show all"), new StatusFilter(2, "show unsolved"), new StatusFilter(3, "show solved") }; } return statusFilters; }
            set { SetProperty(ref statusFilters, value); }
        }

        private StatusFilter selectedStatusFilter;
        public StatusFilter SelectedStatusFilter
        {
            get { if (selectedStatusFilter == null) selectedStatusFilter = StatusFilters.First(); return selectedStatusFilter; }
            set { SetProperty(ref selectedStatusFilter, value); }
        }
        #endregion

        #region Statistics
        private int processedFilesCount;
        public int ProcessedFilesCount
        {
            get { return processedFilesCount; }
            set { SetProperty(ref processedFilesCount, value); }
        }

        private int foundDuplicatesCount;
        public int FoundDuplicatesCount
        {
            get { return foundDuplicatesCount; }
            set { SetProperty(ref foundDuplicatesCount, value); }
        }

        private int unsolvedDuplicatesCount;
        public int UnsolvedDuplicatesCount
        {
            get { return unsolvedDuplicatesCount; }
            set { SetProperty(ref unsolvedDuplicatesCount, value); }
        }

        private int solvedDuplicatesCount;
        public int SolvedDuplicatesCount
        {
            get { return solvedDuplicatesCount; }
            set { SetProperty(ref solvedDuplicatesCount, value); }
        }
        #endregion
        #endregion Properties

        #region cts
        public VMResultView(IEnumerable<ImageCompareResult> items, int totalFilesProcessed)
        {
            PropertyChanged += VMResultView_PropertyChanged;

            this.Results.Clear();
            this.Results.AddRange(items.OrderByDescending(a => a.FLANN));

            _ = Task.Run(async () =>
            {
                foreach (var icr in Results)
                {
                    icr.FileA.File = new FileInfo(icr.FileA.Filepath);
                    icr.FileB.File = new FileInfo(icr.FileB.Filepath);
                }

                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.SelectedResult = Results.FirstOrDefault();
                });
            });

            Task.Run(async () =>
            {
                while (!isDisposed)
                {
                    RefreshImageCompareResultState();
                    UnsolvedDuplicatesCount = Results.Count(a => a.State == ImageCompareResult.StateEnum.Unsolved);
                    SolvedDuplicatesCount = Results.Count(a => a.State == ImageCompareResult.StateEnum.Solved);

                    await Task.Delay(1000);
                }
            });

            ProcessedFilesCount = totalFilesProcessed;
            FoundDuplicatesCount = items.Count();
        }

        private async void VMResultView_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SelectedResult):
                    if (SelectedResult != null)
                    {
                        try
                        {
                            await cancelSlimmy.WaitAsync();
                            this.preLoadImagesCancelTokenSource?.Cancel();
                            this.preLoadImagesCancelTokenSource = new CancellationTokenSource();
                            var ct = this.preLoadImagesCancelTokenSource.Token;
                            cancelSlimmy.Release();

                            await workingSlimmy.WaitAsync();
                            await this.LoadImagesAsync(this.SelectedResult, ct);
                        }
                        catch (OperationCanceledException)
                        { }
                        finally
                        {
                            workingSlimmy.Release();
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

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

        private void UnloadImages(List<ImageCompareResult> results)
        {
            foreach (var r in results)
            {
                if (File.Exists(@"\\?\" + r.FileA.Filepath))
                    r.FileA.BitmapImage = null;
                if (File.Exists(@"\\?\" + r.FileB.Filepath))
                    r.FileB.BitmapImage = null;

                r.ImageLoadStarted = false;
            }
        }

        private async Task LoadImagesAsync(List<ImageCompareResult> results, CancellationToken ct)
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

                var fig = fi.GroupBy(a => a.Filepath);

                foreach (var g in fig)
                {
                    ct.ThrowIfCancellationRequested();

                    var path = @"\\?\" + g.First().Filepath; // long file path syntax
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
                    this.FindSmallerOne(icr);
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
                var items = this.Results.OrderBy(a => a.FLANN).ToList();
                var currentItemIndex = items.IndexOf(icr);

                #region Load Bitmaps
                var startIndex = currentItemIndex - this.preLoadRange;
                startIndex = ((startIndex < 0) ? 0 : startIndex);

                var endIndex = currentItemIndex + this.preLoadRange;
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

                await this.LoadImagesAsync(itemsToProcess, ct);
                #endregion Load Bitmaps

                #region Unload Bitmaps
                ct.ThrowIfCancellationRequested();

                var filesToProcess = itemsToProcess.SelectMany(a => new[] { a.FileA, a.FileB }).ToList();
                var itemsToUnloadBitmap = items.Where(a => filesToProcess.Contains(a.FileA) == false && filesToProcess.Contains(a.FileB) == false).ToList();
                this.UnloadImages(itemsToUnloadBitmap);
                #endregion Unload Bitmaps
            });
        }

        private void FindSmallerOne(ImageCompareResult icr)
        {
            if (string.IsNullOrWhiteSpace(icr.SmallerOne))
            {
                icr.SmallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? "FileA" : icr.FileA.PixelCount > icr.FileB.PixelCount ? "FileB" : null;
            }
        }
        #endregion

        #region Commands
        #region ApplyFiltersCommand
        private ICommand applyFiltersCommand;
        public ICommand ApplyFiltersCommand
        {
            get
            {
                if (applyFiltersCommand == null)
                {
                    applyFiltersCommand = new RelayCommand(p => ApplyFilters(),
                        p => CanApplyFilters());
                }
                return applyFiltersCommand;
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

        private bool CanApplyFilters()
        {
            return true;
        }
        #endregion
        #region ActivateFiltersCommand
        private ICommand activateFiltersCommand;
        public ICommand ActivateFiltersCommand
        {
            get
            {
                if (activateFiltersCommand == null)
                {
                    activateFiltersCommand = new RelayCommand(p => ActivateFilters(),
                        p => CanActivateFilters());
                }
                return activateFiltersCommand;
            }
        }

        public void ActivateFilters()
        {
            // toggle filter active/deactive
            FilterActivated = !FilterActivated;

            // filter anwenden bzw. aufheben
            ApplyFilters();
        }

        private bool CanActivateFilters()
        {
            return true;
        }
        #endregion


        #region DeleteFile
        private ICommand deleteFileCommand;
        public ICommand DeleteFileCommand
        {
            get
            {
                if (deleteFileCommand == null)
                {
                    deleteFileCommand = new RelayCommand(p => DeleteFile(p),
                        p => CanDeleteFile(p));
                }
                return deleteFileCommand;
            }
        }

        public void DeleteFile(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                FileOperationAPIWrapper.Send(fi.FullName);
            }

            if (IsExterminationModeActive)
                SelectNextUnsolvedResult();
        }

        private bool CanDeleteFile(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                return File.Exists(fi.FullName);
            }

            return false;
        }
        #endregion DeleteFile

        #region MoveSelectionDown
        private ICommand moveSelectionDownCommand;
        public ICommand MoveSelectionDownCommand
        {
            get
            {
                if (moveSelectionDownCommand == null)
                {
                    moveSelectionDownCommand = new RelayCommand(p => MoveSelectionDown(),
                        p => CanMoveSelectionDown());
                }
                return moveSelectionDownCommand;
            }
        }

        public void MoveSelectionDown()
        {
            SelectNextUnsolvedResult();
        }

        private bool CanMoveSelectionDown()
        {
            return true;
        }
        #endregion MoveSelectionUp

        #region MoveSelectionUp
        private ICommand moveSelectionUpCommand;
        public ICommand MoveSelectionUpCommand
        {
            get
            {
                if (moveSelectionUpCommand == null)
                {
                    moveSelectionUpCommand = new RelayCommand(p => MoveSelectionUp(),
                        p => CanMoveSelectionUp());
                }
                return moveSelectionUpCommand;
            }
        }

        public void MoveSelectionUp()
        {
            SelectPreviousUnsolvedResult();
        }

        private bool CanMoveSelectionUp()
        {
            return true;
        }
        #endregion MoveSelectionUp

        #region OpenFolder
        private ICommand openFolderCommand;
        public ICommand OpenFolderCommand
        {
            get
            {
                if (openFolderCommand == null)
                {
                    openFolderCommand = new RelayCommand(p => OpenFolder(p),
                        p => CanOpenFolder(p));
                }
                return openFolderCommand;
            }
        }

        public void OpenFolder(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                Path.GetDirectoryName(fi.FullName);

                Process.Start("explorer.exe", string.Format(@"/select,{0}", fi.FullName));
            }
        }

        private bool CanOpenFolder(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                return File.Exists(fi.FullName);
            }

            return false;
        }
        #endregion OpenFolder

        #region ImageClick
        private ICommand imageClickCommand;
        public ICommand ImageClickCommand
        {
            get
            {
                if (imageClickCommand == null)
                {
                    imageClickCommand = new RelayCommand(p => ImageClick(p),
                        p => CanImageClick(p));
                }
                return imageClickCommand;
            }
        }

        public void ImageClick(object p)
        {
            if (p is FileImage fileImage)
            {
                if (this.IsExterminationModeActive)
                {
                    this.DeleteFile(fileImage.File);
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
                if (this.IsExterminationModeActive)
                {
                    return this.CanDeleteFile(fileImage.File);
                }
                else
                {
                    if (fileImage.File is FileInfo fi)
                    {
                        return File.Exists(fi.FullName);
                    }
                }
            }

            return false;
        }
        #endregion ImageClick

        #region CutFile
        private ICommand cutFileCommand;
        public ICommand CutFileCommand
        {
            get
            {
                if (cutFileCommand == null)
                {
                    cutFileCommand = new RelayCommand(p => CutFile(p),
                        p => CanCutFile(p));
                }
                return cutFileCommand;
            }
        }

        public void CutFile(object file)
        {
            FileInfo fi = file as FileInfo;

            ClipboardHelper.SetFileToClipboard(fi.FullName, ClipboardHelper.Operation.Cut);
        }

        private bool CanCutFile(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                return File.Exists(fi.FullName);
            }

            return false;
        }
        #endregion CutFile

        #region CutSmallerOnes
        private ICommand cutSmallerOnesCommand;
        public ICommand CutSmallerOnesCommand
        {
            get
            {
                if (cutSmallerOnesCommand == null)
                {
                    cutSmallerOnesCommand = new RelayCommand(p => CutSmallerOnes(p),
                        p => CanCutSmallerOnes(p));
                }
                return cutSmallerOnesCommand;
            }
        }

        public void CutSmallerOnes(object selectedRows)
        {
            List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();
            List<FileInfo> smallerOnes = new List<FileInfo>();

            foreach (var icr in icrs)
            {
                if (icr.FileA.PixelCount != null && icr.FileB.PixelCount != null)
                {
                    var smallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? icr.FileA.File : icr.FileB.File;

                    if (File.Exists(smallerOne.FullName))
                        smallerOnes.Add(smallerOne);
                }
            }

            ClipboardHelper.SetFilesToClipboard(smallerOnes.Select(a => a.FullName).Distinct(), ClipboardHelper.Operation.Cut);
        }

        private bool CanCutSmallerOnes(object selectedRows)
        {
            List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();

            if (icrs.Count == 0)
                return false;

            return true;
        }
        #endregion CutSmallerOnes

        #region DeleteSmallerOnes
        private ICommand deleteSmallerOnesCommand;
        public ICommand DeleteSmallerOnesCommand
        {
            get
            {
                if (deleteSmallerOnesCommand == null)
                {
                    deleteSmallerOnesCommand = new RelayCommand(p => DeleteSmallerOnes(p),
                        p => CanDeleteSmallerOnes());
                }
                return deleteSmallerOnesCommand;
            }
        }

        public void DeleteSmallerOnes(object selectedRows)
        {
            List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();
            List<FileInfo> smallerOnes = new List<FileInfo>();

            foreach (var icr in icrs)
            {
                if (icr.FileA.PixelCount != null && icr.FileB.PixelCount != null)
                {
                    var smallerOne = icr.FileA.PixelCount < icr.FileB.PixelCount ? icr.FileA.File : icr.FileB.File;

                    if (File.Exists(smallerOne.FullName))
                        smallerOnes.Add(smallerOne);
                }
            }

            foreach (var fi in smallerOnes.Select(a => a.FullName).Distinct())
            {
                FileOperationAPIWrapper.Send(fi);
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
        private ICommand restoreImageCommand;
        public ICommand RestoreImageCommand
        {
            get
            {
                if (restoreImageCommand == null)
                {
                    restoreImageCommand = new RelayCommand(p => RestoreImage(p),
                        p => CanRestoreImage(p));
                }
                return restoreImageCommand;
            }
        }

        public void RestoreImage(object fileImage)
        {
            FileImage fi = fileImage as FileImage;

            if (fi != null && fi.BitmapImage != null)
            {
                fi.BitmapImage.SaveToFile(fi.Filepath);
            }
        }

        private bool CanRestoreImage(object fileImage)
        {
            FileImage fi = fileImage as FileImage;

            if (fi == null || fi.BitmapImage == null || File.Exists(fi.Filepath))
                return false;

            return true;

        }
        #endregion RestoreImage

        #region SaveAsImage
        private ICommand saveAsImageCommand;
        public ICommand SaveAsImageCommand
        {
            get
            {
                if (saveAsImageCommand == null)
                {
                    saveAsImageCommand = new RelayCommand(p => SaveAsImage(p),
                        p => CanSaveAsImage(p));
                }
                return saveAsImageCommand;
            }
        }

        public void SaveAsImage(object fileImage)
        {
            FileImage fi = fileImage as FileImage;

            if (fi != null && fi.BitmapImage != null)
            {
                // prompt user for directory
                var dlg = new SaveFileDialog();
                dlg.FileName = fi.File.Name;
                dlg.DefaultExt = fi.File.Extension;
                dlg.Filter = "Images |*.jpg;*.png;*.bmp;*.jfif;*jpeg;*.tif;*.tiff";

                bool? result = dlg.ShowDialog();

                if (result == true)
                {
                    fi.BitmapImage.SaveToFile(dlg.FileName);
                }
            }
        }

        private bool CanSaveAsImage(object fileImage)
        {
            FileImage fi = fileImage as FileImage;

            if (fi == null || fi.BitmapImage == null)
                return false;

            return true;
        }
        #endregion SaveAsImage

        #region RestoreSelectedImages
        private ICommand restoreSelectedImagesCommand;
        public ICommand RestoreSelectedImagesCommand
        {
            get
            {
                if (restoreSelectedImagesCommand == null)
                {
                    restoreSelectedImagesCommand = new RelayCommand(p => RestoreSelectedImages(p),
                        p => CanRestoreSelectedImages(p));
                }
                return restoreSelectedImagesCommand;
            }
        }

        public void RestoreSelectedImages(object selectedRows)
        {
            List<ImageCompareResult> icrs = (selectedRows as IList).OfType<ImageCompareResult>().ToList();

            var deletedOnes = icrs.Select(a => a.FileA).Concat(icrs.Select(a => a.FileB))
                                .DistinctBy(a => a.Filepath).Where(a => File.Exists(a.Filepath) == false);

            foreach (var restoreMe in deletedOnes)
            {
                restoreMe.BitmapImage.SaveToFile(restoreMe.Filepath);
            }
        }

        private bool CanRestoreSelectedImages(object selectedRows)
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
            this.preLoadImagesCancelTokenSource?.Cancel();

            this.SelectedResult = null;

            this.PropertyChanged -= VMResultView_PropertyChanged;

            foreach (var result in this.Results)
            {
                result.FileA.Dispose();
                result.FileB.Dispose();
                result.ImageLoadStarted = false;
            }
            this.Results.Clear();

            this.isDisposed = true;
        }
        #endregion
    }
}
