using ImageChecker.Concurrent;
using ImageChecker.DataClass;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageChecker.Imaging;
using ImageChecker.ViewModel;
using System.Diagnostics;
using ImageChecker.Helper;

namespace ImageChecker.Processing
{
    public class WorkerImageComparison : ViewModelBase, IDisposable
    {
        #region Properties
        private bool isComparingImages = false;
        public bool IsComparingImages
        {
            get
            {
                return isComparingImages;
            }
            set
            {
                if (isComparingImages != value)
                {
                    isComparingImages = value;
                    RaisePropertyChanged("IsComparingImages");
                }
            }
        }

        private bool isComparingImagesPaused = false;
        public bool IsComparingImagesPaused
        {
            get
            {
                return isComparingImagesPaused;
            }
            set
            {
                if (isComparingImagesPaused != value)
                {
                    isComparingImagesPaused = value;
                    RaisePropertyChanged("IsComparingImagesPaused");
                }
            }
        }

        private CancellationTokenSource ctsImageComparison;
        public CancellationTokenSource CtsImageComparison
        {
            get
            {
                if (ctsImageComparison == null)
                {
                    ctsImageComparison = new CancellationTokenSource();
                }

                return ctsImageComparison;
            }
            set
            {
                if (ctsImageComparison != value)
                {
                    ctsImageComparison = value;
                    RaisePropertyChanged("CtsImageComparision");
                }
            }
        }

        private PauseTokenSource ptsImageComparison;
        public PauseTokenSource PtsImageComparison
        {
            get
            {
                if (ptsImageComparison == null)
                {
                    ptsImageComparison = new PauseTokenSource();
                }

                return ptsImageComparison;
            }
            set
            {
                if (ptsImageComparison != value)
                {
                    ptsImageComparison = value;
                    RaisePropertyChanged("PtsImageComparison");
                }
            }
        }

        public Progress<ProgressImageComparison> ImageComparisonProgress { get; set; }
        public IProgress<ProgressImageComparison> ImageComparisonProgressInterface { get { return ImageComparisonProgress as IProgress<ProgressImageComparison>; } }

        private ConcurrentBag<string> errorFiles;
        public ConcurrentBag<string> ErrorFiles
        {
            get
            {
                if (errorFiles == null)
                    errorFiles = new ConcurrentBag<string>();

                return errorFiles;
            }
            set
            {
                if (errorFiles != value)
                {
                    errorFiles = value;
                    RaisePropertyChanged("ErrorFiles");
                }
            }
        }

        private bool hasErrorFiles;
        public bool HasErrorFiles
        {
            get
            {
                return hasErrorFiles;
            }
            set
            {
                if (hasErrorFiles != value)
                {
                    hasErrorFiles = value;
                    RaisePropertyChanged("HasErrorFiles");
                }
            }
        }

        private ConcurrentBag<ImageCompareResult> possibleDuplicates;
        public ConcurrentBag<ImageCompareResult> PossibleDuplicates
        {
            get
            {
                if (possibleDuplicates == null)
                    possibleDuplicates = new ConcurrentBag<ImageCompareResult>();
                
                return possibleDuplicates;
            }
            set
            {
                SetProperty(ref possibleDuplicates, value);
            }
        }

        private int selectedPossibleDuplicatesCount = -1;
        public int SelectedPossibleDuplicatesCount
        {
            get { return selectedPossibleDuplicatesCount; }
            set { SetProperty(ref selectedPossibleDuplicatesCount, value); }
        }

        private string selectedPossibleDuplicatesCountMessage;
        public string SelectedPossibleDuplicatesCountMessage
        {
            get { return selectedPossibleDuplicatesCountMessage; }
            set { SetProperty(ref selectedPossibleDuplicatesCountMessage, value); }
        }

        private bool hasPossibleDuplicates;
        public bool HasPossibleDuplicates
        {
            get
            {
                return hasPossibleDuplicates;
            }
            set
            {
                SetProperty(ref hasPossibleDuplicates, value);
            }
        }

        private List<DirectoryInfo> folders;
        public List<DirectoryInfo> Folders
        {
            get { if (folders == null) folders = new List<DirectoryInfo>(); return folders; }
            set { SetProperty(ref folders, value); }
        }

        private bool includeSubdirectories;
        public bool IncludeSubdirectories
        {
            get { return includeSubdirectories; }
            set { SetProperty(ref includeSubdirectories, value); }
        }

        private bool preResizeImages;
        public bool PreResizeImages
        {
            get { return preResizeImages; }
            set { SetProperty(ref preResizeImages, value); }
        }

        private int preResizeScale;
        public int PreResizeScale
        {
            get { return preResizeScale; }
            set { SetProperty(ref preResizeScale, value); }
        }

        private double threshold;
        public double Threshold
        {
            get { return threshold; }
            set { SetProperty(ref threshold, value); }
        }

        private List<string> files;
        public List<string> Files
        {
            get { return files; }
            set { SetProperty(ref files, value); }
        }

        #endregion

        #region Members
        private ProgressImageComparison currentProgress;

        private Stopwatch timer = new Stopwatch();
        private long fullOperationsCount = 0L;
        private long fullOperationsCurrentCount = 0L;
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
                case "ErrorFiles":
                    HasErrorFiles = ErrorFiles.Any();
                    break;
                case "PossibleDuplicates":
                    HasPossibleDuplicates = PossibleDuplicates.Any();
                    break;
                case nameof(this.SelectedPossibleDuplicatesCount):
                    this.SelectedPossibleDuplicatesCountMessage = $"show [{this.SelectedPossibleDuplicatesCount}] results";
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Methods
        public void RefreshSelectedPossibleDuplicatesCount(double treshold)
        {
            this.SelectedPossibleDuplicatesCount = this.PossibleDuplicates.Count(a => a.FLANN >= treshold);
        }

        public async Task Start()
        {
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

                currentProgress = new ProgressImageComparison(0, Files.Count, 0, "preparing", null, null);

                ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, null));
                
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
            currentProgress.Value = 0;
            currentProgress.Operation = "loading files";
            ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, null));

            ConcurrentBag<FileImage> preLoadedFileImagesSource = new ConcurrentBag<FileImage>();
            // reset stuff
            ErrorFiles = new ConcurrentBag<string>();
            HasErrorFiles = false;
            possibleDuplicates = new ConcurrentBag<ImageCompareResult>();
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

                    currentProgress.Value++;
                    ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, null));

                    if (CtsImageComparison.Token.IsCancellationRequested)
                        break;

                    await PtsImageComparison.Token.WaitWhilePausedAsync();

                    if (nextIndex < files.Count)
                    {
                        imageTasks.Add(cf.ComputeSingleDescriptorsAsync(files[nextIndex], preLoadedFileImagesSource, errorFiles, PreResizeImages, PreResizeScale));
                        nextIndex++;
                    }
                }
            });

            if (errorFiles.Count > 0)
                HasErrorFiles = true;

            if (CtsImageComparison.IsCancellationRequested)
            {
                IsComparingImages = false;
                currentProgress.Operation = "loading files canceled!";
                ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, null));
                return;
            } 
            #endregion

            timer.Restart();
            CtsImageComparison = new CancellationTokenSource();
            PtsImageComparison = new PauseTokenSource();
            long fullOperationsCurrentToDoCount = 0;
            long secondsElapsed = 0;
            long secondsToGo = 0;

            currentProgress.Value = 0;
            currentProgress.Maximum = preLoadedFileImagesSource.Count;
            fullOperationsCount = MathHelper.SumToN((long)currentProgress.Maximum - 1L);
            currentProgress.Operation = "comparing images";
            ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, null));

            await Task.Run(async () =>
            {
                List<FileImage> toDo = preLoadedFileImagesSource.ToList();

                List<WorkItem> workItems = new List<WorkItem>();

                foreach (var item in toDo)
                {
                    var wi = new WorkItem();
                    wi.ItemToCheck = item;
                    wi.ItemsToCheckAgainst = toDo.Skip(toDo.IndexOf(item) + 1).ToList();
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
                    imageTasks.Add(cf.FindMatches(workItems[nextIndex].ItemToCheck, workItems[nextIndex].ItemsToCheckAgainst, PossibleDuplicates, Threshold, CtsImageComparison.Token, PtsImageComparison.Token));
                    nextIndex++;

                    await Task.Delay(100);
                }

                while (imageTasks.Count > 0)
                {
                    Task imageTask = await Task.WhenAny(imageTasks).ConfigureAwait(false);
                    imageTasks.Remove(imageTask);
                    
                    currentProgress.Value++;

                    // gauss berechnung und stopwatch verwenden
                    fullOperationsCurrentCount = MathHelper.PartialSumToNProgress((long)currentProgress.Maximum, (long)currentProgress.Value);
                    fullOperationsCurrentToDoCount = fullOperationsCount - fullOperationsCurrentCount;
                    secondsElapsed = (long)(timer.Elapsed.TotalSeconds - (PtsImageComparison.Pauses.Sum(a => a.TotalSeconds)));
                    secondsToGo = (long)(((double)secondsElapsed / (double)fullOperationsCurrentCount) * (double)fullOperationsCurrentToDoCount);

                    ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, secondsToGo, secondsElapsed));

                    if (CtsImageComparison.Token.IsCancellationRequested)
                        break;

                    await PtsImageComparison.Token.WaitWhilePausedAsync();

                    if (nextIndex < workItems.Count)
                    {
                        imageTasks.Add(cf.FindMatches(workItems[nextIndex].ItemToCheck, workItems[nextIndex].ItemsToCheckAgainst, PossibleDuplicates, Threshold, CtsImageComparison.Token, PtsImageComparison.Token));
                        nextIndex++;
                    }
                }

                this.ClearFalsePositives();
            });


            HasPossibleDuplicates = PossibleDuplicates.Any();
            
            if (CtsImageComparison.IsCancellationRequested)
            {
                IsComparingImages = false;
                currentProgress.Operation = "comparing images canceled!";
                ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, secondsElapsed));
                timer.Stop();
                return;
            }
            else
            { // erfolgreich und vollständig durchlaufen
                IsComparingImages = false;
                currentProgress.Operation = "comparing images completed!";
                ImageComparisonProgressInterface.Report(new ProgressImageComparison(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation, null, secondsElapsed));
                timer.Stop();
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
            var dups = this.PossibleDuplicates.Where(a => a.FLANN < 100D).ToList();

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

            var icrList = this.PossibleDuplicates.Except(badResults).ToList();

            ImageCompareResult icr;
            while (this.PossibleDuplicates.TryTake(out icr))
            { } // so lange items entfernen, wie ein Item erfolgreich entfernt wurde....

            foreach (var item in icrList)
                this.PossibleDuplicates.Add(item);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (ctsImageComparison != null)
            {
                ctsImageComparison.Dispose();
                ctsImageComparison = null;
            }
        } 
        #endregion
    }
}
