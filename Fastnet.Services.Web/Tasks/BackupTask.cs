using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Fastnet.Services.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Services.Tasks
{
    public class BackupTask : IPipelineTask
    {
        private readonly ILogger log;
        private readonly WebDbContextFactory dbf;
        private readonly int sourceFolderId;
        private readonly ServiceOptions options;
        private ServiceDb db;
        public BackupTask(ServiceOptions options, int sourceFolderId, WebDbContextFactory dbf, ILogger log)
        {
            this.options = options;
            this.log = log;
            this.dbf = dbf;
            this.sourceFolderId = sourceFolderId;
        }
        public string Name => $"BackupTask - source folder id {sourceFolderId}";
        public TaskMethod ExecuteAsync => DoTask;
        private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            //if (options.IsBackupDestinationAvailable())
            //{
            //    using (db = dbf.GetWebDbContext<ServiceDb>())
            //    {
            //        var sf = await db.SourceFolders
            //            .Include(x => x.Backups)
            //            .SingleAsync(x => x.Id == sourceFolderId);
            //        //log.LogInformation($"Backup task for source {sf.DisplayName}");
            //        var defaultDestinationFolder = Path.Combine(options.GetDefaultBackupDestination(), sf.DisplayName);
            //        if (!Directory.Exists(defaultDestinationFolder))
            //        {
            //            Directory.CreateDirectory(defaultDestinationFolder);
            //            log.LogInformation($"{defaultDestinationFolder} created");
            //        }
            //        var isPending = await IsBackupPending(sf);
            //        if (isPending.result)
            //        {
            //            //log.LogInformation($"Backup of {sf.GetFullname()} to {destinationFolder} is required");
            //            var backup = isPending.backup;
            //            var namePart = sf.DisplayName;
            //            var datePart = $"{(backup.ScheduledOn.ToString("yyyy.MM.dd"))}";
            //            var backupFileName = $"{namePart}.{datePart}.zip";
            //            backup.FullPath = Path.Combine(defaultDestinationFolder, backupFileName);
            //            backup.State = BackupState.Started;
            //            var now = DateTimeOffset.Now;
            //            var todaysScheduledTime = new DateTimeOffset(now.Year, now.Month, now.Day, sf.ScheduledTime, 0, 0, now.Offset);
            //            log.LogInformation($"Backup of {sf.DisplayName} to {defaultDestinationFolder} started ({(todaysScheduledTime.ToString("ddMMMyyyy HH:mm:ss"))})");
            //            if (sf.Type == SourceType.Website)
            //            {
            //                TakeSiteOffline(sf);
            //            }
            //            await db.SaveChangesAsync();
            //            try
            //            {
            //                if (File.Exists(backup.FullPath))
            //                {
            //                    File.Delete(backup.FullPath);
            //                    log.LogWarning($"{backup.FullPath} deleted");
            //                }
            //                zip(sf.FullPath, backup.FullPath);
            //                backup.State = BackupState.Finished;
            //                backup.BackedUpOn = DateTimeOffset.Now;
            //                await db.SaveChangesAsync();
            //                log.LogInformation($"Backup of {sf.DisplayName} to {backup.FullPath} completed");
            //            }
            //            catch (Exception xe)
            //            {
            //                log.LogError(xe, $"backup failed {sf.DisplayName} to {backup.FullPath}");
            //                backup.State = BackupState.Failed;
            //                backup.BackedUpOn = DateTimeOffset.Now;
            //                await db.SaveChangesAsync();
            //                //throw;
            //            }
            //            finally
            //            {
            //                if (sf.Type == SourceType.Website)
            //                {
            //                    BringSiteOnline(sf);
            //                }
            //            }
            //            await PurgeBackups(sf);
            //        }
            //        else
            //        {

            //            log.LogInformation($"Backup of {sf.DisplayName} is not pending");
            //        }
            //    }
            //}
            //else
            //{
            //    foreach (var d in DriveInfo.GetDrives())
            //    {
            //        log.LogInformation($"Found drive {d.Name}, label {d.VolumeLabel}, ready = {d.IsReady}");
            //    }
            //    log.LogWarning($"Backup destination not available - no disk with volume label {options.BackupDriveLabel} found");
            //}
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                var sf = await db.SourceFolders
                    .Include(x => x.Backups)
                    .SingleAsync(x => x.Id == sourceFolderId);
                //log.LogInformation($"Backup task for source {sf.DisplayName}");
                //var defaultDestinationFolder = Path.Combine(options.GetDefaultBackupDestination(), sf.DisplayName);
                //var defaultDestinationFolder = sf.GetDestinationFolder(options);// Path.Combine(options.GetDefaultBackupDestination(), sf.DisplayName);
                (bool available, string destinationFolder) = sf.GetDestinationFolder(options);
                if (available)
                {
                    if (!Directory.Exists(destinationFolder))
                    {
                        Directory.CreateDirectory(destinationFolder);
                        log.LogInformation($"{destinationFolder} created");
                    }
                    var isPending = await IsBackupPending(sf);
                    if (isPending.result)
                    {
                        //log.LogInformation($"Backup of {sf.GetFullname()} to {destinationFolder} is required");
                        var backup = isPending.backup;
                        var namePart = sf.DisplayName;
                        var datePart = $"{(backup.ScheduledOn.ToString("yyyy.MM.dd"))}";
                        var backupFileName = $"{namePart}.{datePart}.zip";
                        backup.FullPath = Path.Combine(destinationFolder, backupFileName);
                        backup.State = BackupState.Started;
                        var now = DateTimeOffset.Now;
                        var todaysScheduledTime = new DateTimeOffset(now.Year, now.Month, now.Day, sf.ScheduledTime, 0, 0, now.Offset);
                        log.LogInformation($"Backup of {sf.DisplayName} to {destinationFolder} started ({(todaysScheduledTime.ToString("ddMMMyyyy HH:mm:ss"))})");
                        if (sf.Type == SourceType.Website)
                        {
                            TakeSiteOffline(sf);
                        }
                        await db.SaveChangesAsync();
                        try
                        {
                            if (File.Exists(backup.FullPath))
                            {
                                File.Delete(backup.FullPath);
                                log.LogWarning($"{backup.FullPath} deleted");
                            }
                            zip(sf.FullPath, backup.FullPath);
                            backup.State = BackupState.Finished;
                            backup.BackedUpOn = DateTimeOffset.Now;
                            await db.SaveChangesAsync();
                            log.LogInformation($"Backup of {sf.DisplayName} to {backup.FullPath} completed");
                        }
                        catch (Exception xe)
                        {
                            log.LogError(xe, $"backup failed {sf.DisplayName} to {backup.FullPath}");
                            backup.State = BackupState.Failed;
                            backup.BackedUpOn = DateTimeOffset.Now;
                            await db.SaveChangesAsync();
                            //throw;
                        }
                        finally
                        {
                            if (sf.Type == SourceType.Website)
                            {
                                BringSiteOnline(sf);
                            }
                        }
                        await PurgeBackups(sf);
                    }
                    else
                    {
                        log.LogInformation($"Backup of {sf.DisplayName} is not pending");
                    }
                }
                else
                {
                    log.LogWarning($"{sf.DisplayName}: {sf.FullPath} not backed up as destination is not available" );
                }
            }
            return null;
        }

        private async Task PurgeBackups(SourceFolder sf)
        {
            var numberToKeep = 5;
            if(sf.Backups.Count() > numberToKeep)
            {
                var toDelete = sf.Backups.OrderByDescending(x => x.ScheduledOn).Skip(numberToKeep).ToArray();
                foreach(var b in toDelete)
                {
                    try
                    {
                        if (File.Exists(b.FullPath))
                        {
                            db.Backups.Remove(b);
                            await db.SaveChangesAsync();
                            File.Delete(b.FullPath);
                            log.LogInformation($"Backup {b.FullPath} purged");
                        }
                    }
                    catch (Exception xe)
                    {
                        log.LogError(xe, $"File purge failed: {b.FullPath}");
                    }
                }
            }            
        }
        private void zip(string source, string destination)
        {
            ZipFile.CreateFromDirectory(source, destination);
        }
        private async Task<(bool result, Backup backup)> IsBackupPending(SourceFolder sf)
        {
            var result = false;
            var now = DateTimeOffset.Now;
            var todaysScheduledTime = new DateTimeOffset(now.Year, now.Month, now.Day, sf.ScheduledTime, 0, 0, now.Offset);
            //log.LogInformation($"{sf.GetFullname()} scheduled backup time is {(todaysScheduledTime.ToString("ddMMMyyyy HH:mm:ss"))}");
            var backup = sf.Backups.SingleOrDefault(x => x.ScheduledOn == todaysScheduledTime);
            if(backup == null)
            {
                backup = new Backup
                {
                    SourceFolder = sf,
                    ScheduledOn = todaysScheduledTime,
                    BackedUpOn = DateTimeOffset.MinValue,
                    State = BackupState.NotStarted,
                };
                await db.Backups.AddAsync(backup);
                await db.SaveChangesAsync();
            }
            result = backup.State == BackupState.NotStarted || backup.State == BackupState.Failed;
            //switch (backup.State)
            //{
            //    case BackupState.Finished:
            //        log.LogInformation($"{sf.GetFullname()} ocurred at {(backup.BackedUpOn.ToString("ddMMMyyyy HH:mm:ss"))}");
            //        break;
            //    case BackupState.Started:
            //        log.LogInformation($"{sf.GetFullname()} is in progress");
            //        break;
            //    case BackupState.NotStarted:
            //        log.LogInformation($"{sf.GetFullname()} has not started");
            //        result = true;
            //        break;
            //}
            return result ? (result, backup) : (result, null);
        }
        private void TakeSiteOffline(SourceFolder sf)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<h2>Fastnet Backup Services</h2>");
            sb.AppendLine($"<p>This site is temporarily down for maintenance</p>");
            sb.AppendLine($"<p>Please return in a short time</p>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            sb.AppendLine($"<div style='display:none'>zzzzzzzzz zzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz zzzzzzzzzzzzzzzzzz </div>");
            string html = sb.ToString();
            string appOffline = Path.Combine(sf.FullPath, "app_offline.htm");
            File.WriteAllText(appOffline, html);
        }
        private void BringSiteOnline(SourceFolder sf)
        {
            string appOffline = Path.Combine(sf.FullPath, "app_offline.htm");
            File.Delete(appOffline);
        }
    }
}
