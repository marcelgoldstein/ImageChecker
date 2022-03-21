using ImageChecker.Concurrent;
using ImageChecker.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ImageChecker.Processing;

public class WorkerRenameFiles : ViewModelBase, IDisposable
{
    #region Properties
    private bool _keepOriginalNames = true;
    public bool KeepOriginalNames
    {
        get => _keepOriginalNames;
        set
        {
            if (_keepOriginalNames != value)
            {
                _keepOriginalNames = value;
                RaisePropertyChanged("KeepOriginalNames");
            }
        }
    }

    private bool _loop = true;
    public bool Loop
    {
        get => _loop;
        set
        {
            if (_loop != value)
            {
                _loop = value;
                RaisePropertyChanged("Loop");
            }
        }
    }

    private bool _loopEndless;
    public bool LoopEndless
    {
        get => _loopEndless;
        set => SetProperty(ref _loopEndless, value);
    }

    private bool _renameAll = false;
    public bool RenameAll
    {
        get => _renameAll;
        set
        {
            if (_renameAll != value)
            {
                _renameAll = value;
                RaisePropertyChanged("RenameAll");
            }
        }
    }

    private double _fileNameLength = 10;
    public double FileNameLength
    {
        get => _fileNameLength;
        set
        {
            if (_fileNameLength != value)
            {
                _fileNameLength = value;
                RaisePropertyChanged("FileNameLength");
            }
        }
    }

    private bool _isRenamingFiles = false;
    public bool IsRenamingFiles
    {
        get => _isRenamingFiles;
        set
        {
            if (_isRenamingFiles != value)
            {
                _isRenamingFiles = value;
                RaisePropertyChanged("IsRenamingFiles");
            }
        }
    }

    private bool _isRenamingFilesPaused = false;
    public bool IsRenamingFilesPaused
    {
        get => _isRenamingFilesPaused;
        set
        {
            if (_isRenamingFilesPaused != value)
            {
                _isRenamingFilesPaused = value;
                RaisePropertyChanged("IsRenamingFilesPaused");
            }
        }
    }

    private CancellationTokenSource _ctsRenameFiles;
    public CancellationTokenSource CtsRenameFiles
    {
        get
        {
            if (_ctsRenameFiles == null)
            {
                _ctsRenameFiles = new CancellationTokenSource();
            }

            return _ctsRenameFiles;
        }
        set
        {
            if (_ctsRenameFiles != value)
            {
                _ctsRenameFiles = value;
                RaisePropertyChanged("CtsRenameFiles");
            }
        }
    }

    private PauseTokenSource _ptsRenameFiles;
    public PauseTokenSource PtsRenameFiles
    {
        get
        {
            if (_ptsRenameFiles == null)
            {
                _ptsRenameFiles = new PauseTokenSource();
            }

            return _ptsRenameFiles;
        }
        set
        {
            if (_ptsRenameFiles != value)
            {
                _ptsRenameFiles = value;
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
    private IEnumerable<DirectoryInfo> _folders;
    private bool _includeSubdirectories;

    private ProgressRenamingFiles _currentProgress;
    #endregion

    #region Methods
    public async Task RenameFilesAsync(IEnumerable<DirectoryInfo> folders, bool includeSubdirectories)
    {
        CtsRenameFiles = new CancellationTokenSource();
        PtsRenameFiles = new PauseTokenSource();
        IsRenamingFiles = true;
        _currentProgress = new ProgressRenamingFiles(0, 0, 0, "preparing");
        RenamingProgressInterface.Report(new ProgressRenamingFiles(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation));

        _folders = folders;
        _includeSubdirectories = includeSubdirectories;

        do
        {
            var files = _folders.SelectMany(a => a.GetFiles("*.*", _includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                                    .Where(a => RenameAll || a.Name.Length <= FileNameLength).ToList();
            if (files.Count == 0 && !LoopEndless) break;
            if (CtsRenameFiles.Token.IsCancellationRequested) break;
            _currentProgress = new ProgressRenamingFiles(0, files.Count, 0, "preparing");
            RenamingProgressInterface.Report(new ProgressRenamingFiles(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation));
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
                    _currentProgress.Value++;
                    RenamingProgressInterface.Report(new ProgressRenamingFiles(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation));
                }
            }

            RenameAll = false;
            if (LoopEndless) await Task.Delay(200);
        } while (Loop || LoopEndless);

        if (CtsRenameFiles.Token.IsCancellationRequested)
            _currentProgress.Operation = "renaming files canceled!   ";
        else
            _currentProgress.Operation = "renaming files completed!   ";

        RenamingProgressInterface.Report(new ProgressRenamingFiles(_currentProgress.Minimum, _currentProgress.Maximum, _currentProgress.Value, _currentProgress.Operation));
        IsRenamingFiles = false;
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        if (_ctsRenameFiles != null)
        {
            _ctsRenameFiles.Dispose();
            _ctsRenameFiles = null;
        }
    }
    #endregion
}
