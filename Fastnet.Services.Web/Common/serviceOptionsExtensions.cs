using System;
using System.IO;
using System.Linq;

namespace Fastnet.Services.Web
{
    public static class serviceOptionsExtensions
    {
        public static bool IsBackupDestinationAvailable(this ServiceOptions options)
        {
            var labels = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.VolumeLabel);
            var count = labels.Count(x => string.Compare(x, options.BackupDriveLabel, true) == 0);
            return count == 1;
        }
        public static string GetBackupDestination(this ServiceOptions options)
        {
            if(IsBackupDestinationAvailable(options))
            {
                var drive = DriveInfo.GetDrives().Single(x => string.Compare(x.VolumeLabel, options.BackupDriveLabel, true) == 0);
                return Path.Combine(drive.Name, options.BackupFolder);
            }
            throw new Exception("Backup destination not available");
        }
    }
}
