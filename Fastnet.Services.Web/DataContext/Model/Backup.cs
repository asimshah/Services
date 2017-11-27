using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Fastnet.Services.Data
{
    public class Backup
    {
        public int Id { get; set; }
        public DateTimeOffset ScheduledOn { get; set; }
        public DateTimeOffset BackedUpOn { get; set; }
        public BackupState State { get; set; }
        public string FullPath { get; set; }
        [JsonIgnore]
        [Timestamp]
        public byte[] TimeStamp { get; set; }

        public int SourceFolderId { get; set; }
        [JsonIgnore]
        public SourceFolder SourceFolder { get; set; }
    }
}
