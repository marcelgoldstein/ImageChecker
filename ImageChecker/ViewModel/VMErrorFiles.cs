using ImageChecker.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ImageChecker.ViewModel;

public sealed class VMErrorFiles : ViewModelBase, IDisposable
{
    #region Properties
    #region Window
    public static string WindowTitle { get { return "ErrorFiles"; } }
    public static string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }
    #endregion

    private ObservableCollection<FileInfo> _errorFiles;
    public ObservableCollection<FileInfo> ErrorFiles
    {
        get
        {
            if (_errorFiles == null)
                _errorFiles = new ObservableCollection<FileInfo>();

            return _errorFiles;
        }
        set
        {
            SetProperty(ref _errorFiles, value);
        }
    }
    #endregion

    #region ctr
    public VMErrorFiles(IEnumerable<string> files)
    {
        foreach (string file in files)
        {
            ErrorFiles.Add(new FileInfo(file));
        }
    }
    #endregion

    #region Commands
    #region ListBoxItems
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

    public static void DeleteFile(object file)
    {
        if (file is FileInfo fi)
        {
            FileOperationAPIWrapper.Send(fi.FullName, FileOperationAPIWrapper.FileOperationFlags.FOF_ALLOWUNDO | FileOperationAPIWrapper.FileOperationFlags.FOF_NOCONFIRMATION | FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
        }
    }

    private static bool CanDeleteFile(object file)
    {
        if (file is FileInfo fi)
        {
            return File.Exists(fi.FullName);
        }

        return false;
    }
    #endregion

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
            Path.GetDirectoryName(fi.FullName);

            Process.Start("explorer.exe", string.Format(@"/select,{0}", fi.FullName));
        }
    }

    private static bool CanOpenFolder(object file)
    {
        if (file is FileInfo fi)
        {
            return File.Exists(fi.FullName);
        }

        return false;
    }
    #endregion  
    #region OpenImage
    private ICommand _openImageCommand;
    public ICommand OpenImageCommand
    {
        get
        {
            if (_openImageCommand == null)
            {
                _openImageCommand = new RelayCommand(p => OpenImage(p),
                    p => CanOpenImage(p));
            }
            return _openImageCommand;
        }
    }

    public static void OpenImage(object file)
    {
        if (file is FileInfo fi)
        {
            Process.Start(new ProcessStartInfo(fi.FullName) { UseShellExecute = true });
        }
    }

    private static bool CanOpenImage(object file)
    {
        if (file is FileInfo fi)
        {
            return File.Exists(fi.FullName);
        }

        return false;
    }
    #endregion  
    #endregion

    #region DeleteAllFiles
    private ICommand _deleteAllFilesCommand;
    public ICommand DeleteAllFilesCommand
    {
        get
        {
            if (_deleteAllFilesCommand == null)
            {
                _deleteAllFilesCommand = new RelayCommand(p => DeleteAllFiles(),
                    p => CanDeleteAllFiles());
            }
            return _deleteAllFilesCommand;
        }
    }

    public void DeleteAllFiles()
    {
        foreach (FileInfo fi in ErrorFiles.Where(a => File.Exists(a.FullName)))
        {
            FileOperationAPIWrapper.Send(fi.FullName, FileOperationAPIWrapper.FileOperationFlags.FOF_ALLOWUNDO | FileOperationAPIWrapper.FileOperationFlags.FOF_NOCONFIRMATION | FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
        }
    }

    private bool CanDeleteAllFiles()
    {
        return ErrorFiles.Any(a => File.Exists(a.FullName));
    }
    #endregion 

    #region CutAllFiles
    private ICommand _cutAllFilesCommand;
    public ICommand CutAllFilesCommand
    {
        get
        {
            if (_cutAllFilesCommand == null)
            {
                _cutAllFilesCommand = new RelayCommand(p => CutAllFiles(),
                    p => CanCutAllFiles());
            }
            return _cutAllFilesCommand;
        }
    }

    public void CutAllFiles()
    {
        ClipboardHelper.SetFilesToClipboard(ErrorFiles.Where(a => File.Exists(a.FullName)).Select(a => a.FullName), ClipboardHelper.Operation.Cut);
    }

    private bool CanCutAllFiles()
    {
        return ErrorFiles.Any(a => File.Exists(a.FullName));
    }
    #endregion 
    #endregion

    #region IDisposable
    public void Dispose()
    {

    }
    #endregion
}
