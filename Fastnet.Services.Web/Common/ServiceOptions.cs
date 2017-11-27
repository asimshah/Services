namespace Fastnet.Services.Web
{
    public class ServiceOptions
    {
        //public string DefaultBackupLocation { get; set; }
        public string BackupFolder { get; set; }
        public string BackupDriveLabel { get; set; }
        public JobSchedule[] Schedules { get; set; }
        public FolderSource[] FolderSources { get; set; }
    }
}
