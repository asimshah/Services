using Fastnet.Services.Web;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Services.Data
{
    public class SourceFolder
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string FullPath { get; set; }
        public bool BackupEnabled { get; set; }
        public SourceType Type { get; set; }
        public int ScheduledTime { get; set; } // always an hour i.e ranges from 0 - 23. This is the hour in UTC at which backup should take place, normally 3
        public DateTimeOffset ConfiguredOn { get; set; }
        public string BackupDriveLabel { get; set; }
        public string BackupFolder { get; set; }
        public ICollection<Backup> Backups { get; set; }
        [JsonIgnore]
        [Timestamp]
        public byte[] TimeStamp { get; set; }
    }
}
