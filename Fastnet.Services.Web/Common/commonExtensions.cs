using Fastnet.Services.Data;
using System;
using System.IO;
using System.Linq;

namespace Fastnet.Services.Web
{
    public static class commonExtensions
    {
        //private static bool IsBackupDestinationAvailable(this ServiceOptions options)
        //{
        //    var labels = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.VolumeLabel);
        //    var count = labels.Count(x => string.Compare(x, options.BackupDriveLabel, true) == 0);
        //    return count == 1;
        //}
        //private static bool IsBackupDriveAvailableOld(string driveLabel)
        //{
        //    var labels = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.VolumeLabel);
        //    var count = labels.Count(x =>  string.Compare(x, driveLabel, true) == 0);
        //    return count == 1;
        //}
        private static (bool, string, string) IsBackupDriveAvailable(string driveLabel)
        {
            var di = DriveInfo.GetDrives().SingleOrDefault(x => x.IsReady && string.Compare(driveLabel, x.VolumeLabel, true) == 0);
            return di == null ? (false, null, null) : (true, di.Name, di.RootDirectory.FullName);
        }
        public static (bool, string) GetDestinationFolder(this SourceFolder sf, ServiceOptions options)
        {
            string backupDriveLabel = sf.BackupDriveLabel ?? options.BackupDriveLabel;
            string backupFolder = sf.BackupFolder ?? options.BackupFolder;
            (bool available, string drive, string rootFolder) = IsBackupDriveAvailable(backupDriveLabel);
            if(available)
            {
                return (available, Path.Combine(rootFolder, backupFolder, sf.DisplayName));
            }
            return (available, null);
        }
        //public static string GetDefaultBackupDestination(this ServiceOptions options)
        //{
        //    if(IsBackupDriveAvailableOld(options.BackupDriveLabel))
        //    {
        //        var drive = DriveInfo.GetDrives().Single(x => string.Compare(x.VolumeLabel, options.BackupDriveLabel, true) == 0);
        //        return Path.Combine(drive.Name, options.BackupFolder);
        //    }
        //    throw new Exception("Backup destination not available");
        //}
    }
}
