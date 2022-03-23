using ImageChecker.Const;
using System.Diagnostics;
using System.IO;

namespace ImageChecker.Helper;
internal static class TempFilesHelper
{
    internal static string CreateNewBackupFilePath(string fileToBackup)
    {
        var backupFileName = Path.ChangeExtension(Guid.NewGuid().ToString(), Path.GetExtension(fileToBackup));

        return Path.Combine(GetResultViewBackupsPath(), backupFileName);
    }

    internal static string GetTempFilesRootPath()
    {
        return Path.Combine(Path.GetTempPath(), CommonConst.TEMP_FILES_ROOT, Environment.ProcessId.ToString());
    }

    internal static string GetResultViewBackupsPath()
    {
        return Path.Combine(GetTempFilesRootPath(), CommonConst.TEMP_FILES_RESULTVIEW_BACKUPS);
    }

    internal static void EnsureTempFilesRootPathExists()
    {
        if (Directory.Exists(GetTempFilesRootPath()) == false)
            Directory.CreateDirectory(GetTempFilesRootPath());
    }

    internal static void EnsureResultViewBackupDirectoryExists()
    {
        EnsureTempFilesRootPathExists();

        if (Directory.Exists(GetResultViewBackupsPath()) == false)
            Directory.CreateDirectory(GetResultViewBackupsPath());
    }

    internal static void ClearAllTempFiles()
    {
        if (Directory.Exists(GetTempFilesRootPath()))
            Directory.Delete(GetTempFilesRootPath(), true); 
    }

    internal static void ClearResultViewBackups()
    {
        if (Directory.Exists(GetResultViewBackupsPath()))
            Directory.Delete(GetResultViewBackupsPath(), true); 
    }
}
