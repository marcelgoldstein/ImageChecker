using ImageChecker.Concurrent;
using ImageChecker.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageChecker.Processing
{
    public class WorkerRenameFiles : ViewModelBase, IDisposable
    {
        #region Properties
        private bool keepOriginalNames = true;
        public bool KeepOriginalNames
        {
            get
            {
                return keepOriginalNames;
            }
            set
            {
                if (keepOriginalNames != value)
                {
                    keepOriginalNames = value;
                    RaisePropertyChanged("KeepOriginalNames");
                }
            }
        }

        private bool loop = true;
        public bool Loop
        {
            get
            {
                return loop;
            }
            set
            {
                if (loop != value)
                {
                    loop = value;
                    RaisePropertyChanged("Loop");
                }
            }
        }

        private bool loopEndless;
        public bool LoopEndless
        {
            get { return loopEndless; }
            set { SetProperty(ref loopEndless, value); }
        }

        private bool renameAll = false;
        public bool RenameAll
        {
            get
            {
                return renameAll;
            }
            set
            {
                if (renameAll != value)
                {
                    renameAll = value;
                    RaisePropertyChanged("RenameAll");
                }
            }
        }

        private double fileNameLength = 10;
        public double FileNameLength
        {
            get
            {
                return fileNameLength;
            }
            set
            {
                if (fileNameLength != value)
                {
                    fileNameLength = value;
                    RaisePropertyChanged("FileNameLength");
                }
            }
        }

        private bool isRenamingFiles = false;
        public bool IsRenamingFiles
        {
            get
            {
                return isRenamingFiles;
            }
            set
            {
                if (isRenamingFiles != value)
                {
                    isRenamingFiles = value;
                    RaisePropertyChanged("IsRenamingFiles");
                }
            }
        }

        private bool isRenamingFilesPaused = false;
        public bool IsRenamingFilesPaused
        {
            get
            {
                return isRenamingFilesPaused;
            }
            set
            {
                if (isRenamingFilesPaused != value)
                {
                    isRenamingFilesPaused = value;
                    RaisePropertyChanged("IsRenamingFilesPaused");
                }
            }
        }

        private CancellationTokenSource ctsRenameFiles;
        public CancellationTokenSource CtsRenameFiles
        {
            get
            {
                if (ctsRenameFiles == null)
                {
                    ctsRenameFiles = new CancellationTokenSource();
                }

                return ctsRenameFiles;
            }
            set
            {
                if (ctsRenameFiles != value)
                {
                    ctsRenameFiles = value;
                    RaisePropertyChanged("CtsRenameFiles");
                }
            }
        }

        private PauseTokenSource ptsRenameFiles;
        public PauseTokenSource PtsRenameFiles
        {
            get
            {
                if (ptsRenameFiles == null)
                {
                    ptsRenameFiles = new PauseTokenSource();
                }

                return ptsRenameFiles;
            }
            set
            {
                if (ptsRenameFiles != value)
                {
                    ptsRenameFiles = value;
                    RaisePropertyChanged("PtsRenameFiles");
                }
            }
        }

        public Progress<ProgressRenamingFiles> RenamingProgress { get; set; }
        public IProgress<ProgressRenamingFiles> RenamingProgressInterface { get { return RenamingProgress as IProgress<ProgressRenamingFiles>; } }
        #endregion

        #region Contructor
        public WorkerRenameFiles()
        {
            RenamingProgress = new Progress<ProgressRenamingFiles>();
        }
        #endregion

        #region Members
        private IEnumerable<DirectoryInfo> folders;
        private bool includeSubdirectories;

        private ProgressRenamingFiles currentProgress;
        #endregion

        #region Methods
        public async Task RenameFilesAsync(IEnumerable<DirectoryInfo> folders, bool includeSubdirectories)
        {
            CtsRenameFiles = new CancellationTokenSource();
            PtsRenameFiles = new PauseTokenSource();
            IsRenamingFiles = true;
            currentProgress = new ProgressRenamingFiles(0, 0, 0, "preparing");
            RenamingProgressInterface.Report(new ProgressRenamingFiles(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation));

            this.folders = folders;
            this.includeSubdirectories = includeSubdirectories;

            do
            {
                var files = this.folders.SelectMany(a => a.GetFiles("*.*", this.includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                                        .Where(a => RenameAll || a.Name.Length <= FileNameLength).ToList();
                if (files.Count == 0 && !LoopEndless) break;
                if (CtsRenameFiles.Token.IsCancellationRequested) break;
                currentProgress = new ProgressRenamingFiles(0, files.Count, 0, "preparing");
                RenamingProgressInterface.Report(new ProgressRenamingFiles(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation));
                await PtsRenameFiles.Token.WaitWhilePausedAsync();
                for (int i = 0; i < files.Count; i++)
                {
                    await PtsRenameFiles.Token.WaitWhilePausedAsync();

                    if (CtsRenameFiles.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            File.Move(files[i].FullName, Path.Combine(files[i].Directory.ToString(), string.Concat(files[i].GetHashCode(), KeepOriginalNames ? files[i].Name : files[i].Extension)));
                        }
                        catch (Exception)
                        {
                        }
                        currentProgress.Value++;
                        RenamingProgressInterface.Report(new ProgressRenamingFiles(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation));
                    }
                }

                RenameAll = false;
                if (LoopEndless) await Task.Delay(200);
            } while (Loop || LoopEndless);

            if (CtsRenameFiles.Token.IsCancellationRequested)
                currentProgress.Operation = "renaming files canceled!   ";
            else
                currentProgress.Operation = "renaming files completed!   ";

            RenamingProgressInterface.Report(new ProgressRenamingFiles(currentProgress.Minimum, currentProgress.Maximum, currentProgress.Value, currentProgress.Operation));
            IsRenamingFiles = false;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (ctsRenameFiles != null)
            {
                ctsRenameFiles.Dispose();
                ctsRenameFiles = null;
            }
        } 
        #endregion
    }
}
