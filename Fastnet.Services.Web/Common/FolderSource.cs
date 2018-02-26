namespace Fastnet.Services.Web
{
    public class FolderSource
    {
        public string DisplayName { get; set; }
        public string Folder { get; set; }
        public SourceType Type { get; set; }
        public string BackupFolder { get; set; }
        public string BackupDriveLabel { get; set; }
    }
}
