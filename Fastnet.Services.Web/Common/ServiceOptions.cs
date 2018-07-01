namespace Fastnet.Services.Web
{
    public class ServiceOptions //: Fastnet.Core.Web.ServiceOptions
    {
        public string BackupFolder { get; set; }
        public string BackupDriveLabel { get; set; }
        public bool EnsureBackupDriveReady { get; set; }
        //public ServiceSchedule[] ServiceSchedules { get; set; }
        // each folder source becomes a separate pipeline item within the backup service task
        public FolderSource[] FolderSources { get; set; }
        // each polling schedule a separate pipeline item within the polling service task
        public PollingSchedule[] PollingSchedules { get; set; }
    }
}
