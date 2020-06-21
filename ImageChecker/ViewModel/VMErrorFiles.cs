using ImageChecker.Helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ImageChecker.ViewModel
{
    public class VMErrorFiles : ViewModelBase, IDisposable
    {
        #region Properties
        #region Window
        public string WindowTitle { get { return "ErrorFiles"; } }
        public string WindowIcon { get { return @"/ImageChecker;component/Icon/app.ico"; } }
        #endregion

        private ObservableCollection<FileInfo> errorFiles;
        public ObservableCollection<FileInfo> ErrorFiles
        {
            get
            {
                if (errorFiles == null)
                    errorFiles = new ObservableCollection<FileInfo>();

                return errorFiles;
            }
            set
            {
                SetProperty(ref errorFiles, value);
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
                FileOperationAPIWrapper.Send(fi.FullName, FileOperationAPIWrapper.FileOperationFlags.FOF_ALLOWUNDO | FileOperationAPIWrapper.FileOperationFlags.FOF_NOCONFIRMATION | FileOperationAPIWrapper.FileOperationFlags.FOF_SILENT);
            }
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
        #endregion

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
        #endregion  
        #region OpenImage
        private ICommand openImageCommand;
        public ICommand OpenImageCommand
        {
            get
            {
                if (openImageCommand == null)
                {
                    openImageCommand = new RelayCommand(p => OpenImage(p),
                        p => CanOpenImage(p));
                }
                return openImageCommand;
            }
        }

        public void OpenImage(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                Process.Start(new ProcessStartInfo(fi.FullName) { UseShellExecute = true });
            }
        }

        private bool CanOpenImage(object file)
        {
            FileInfo fi = file as FileInfo;

            if (fi != null)
            {
                return File.Exists(fi.FullName);
            }

            return false;
        }
        #endregion  
        #endregion

        #region DeleteAllFiles
        private ICommand deleteAllFilesCommand;
        public ICommand DeleteAllFilesCommand
        {
            get
            {
                if (deleteAllFilesCommand == null)
                {
                    deleteAllFilesCommand = new RelayCommand(p => DeleteAllFiles(),
                        p => CanDeleteAllFiles());
                }
                return deleteAllFilesCommand;
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
        private ICommand cutAllFilesCommand;
        public ICommand CutAllFilesCommand
        {
            get
            {
                if (cutAllFilesCommand == null)
                {
                    cutAllFilesCommand = new RelayCommand(p => CutAllFiles(),
                        p => CanCutAllFiles());
                }
                return cutAllFilesCommand;
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
}
