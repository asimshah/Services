using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Fastnet.Services.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
//using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Fastnet.Services.Tasks
{
    public class ConfigureBackups : ScheduledTask, IPipelineTask
    {
        private ServiceOptions options;
        private readonly string schedule;
        private ServiceDb db;
        private readonly IServiceProvider sp;
        private readonly SchedulerService schedulerService;
        private readonly ServiceDbContextFactory dbf;
        public ConfigureBackups(ILoggerFactory loggerFactory, ServiceDbContextFactory dbf, IServiceProvider sp,// IHostedService hostedService,
            IOptions<SchedulerOptions> schedulerOptions, IOptionsMonitor<ServiceOptions> options) : base(loggerFactory)
        {
            //log.LogInformation("Configuration constructor called");
            this.dbf = dbf;
            this.options = options.CurrentValue;
            this.sp = sp;
            this.schedulerService = this.sp.GetSchedulerService();// hostedService as SchedulerService;
            options.OnChangeWithDelay((opt) =>
            {
                this.options = opt;
                SetupPipeline();
                log.Information("Configuration (appsettings) changed, starting ConfigureBackups");
                this.schedulerService.ExecuteNow<ConfigureBackups>();
            });
            var backupSchedule = schedulerOptions.Value.Schedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
            schedule = backupSchedule?.Schedule ?? "0 0 1 */12 *";// default is At 00:00 AM, on day 1 of the month, every 12 months!! not useful!
            SetupPipeline();
        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        public override string Schedule => schedule;
        public string Name => "ConfigureBackups";
        public TaskMethod ExecuteAsync => DoTask;
        private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            var configuredOn = DateTimeOffset.Now;
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                var existingSources = await db.SourceFolders.Include(x => x.Backups).ToArrayAsync();
                foreach(var existingSource in existingSources)
                {
                    if(!options.FolderSources.Any(x =>
                        string.Compare(x.DisplayName, existingSource.DisplayName, true) == 0
                        && string.Compare(x.Folder, existingSource.FullPath) == 0
                        ))
                    {
                        // remove this instance from the database
                        var backups = existingSource.Backups.ToArray();
                        db.Backups.RemoveRange(backups);
                        db.SourceFolders.Remove(existingSource);
                        log.Information($"Source {existingSource.DisplayName}, folder {existingSource.FullPath} removed");
                        await db.SaveChangesAsync();
                    }
                }
                foreach (var fs in options.FolderSources)
                {
                    var correspondingItem = await db.SourceFolders.SingleOrDefaultAsync(x => string.Compare(x.DisplayName, fs.DisplayName, true) == 0);
                    if(correspondingItem == null)
                    {
                        correspondingItem = new SourceFolder
                        {
                            BackupEnabled = true, //false,
                            DisplayName = fs.DisplayName,
                            ScheduledTime = 3,
                            ConfiguredOn = DateTimeOffset.Now
                        };
                        await db.SourceFolders.AddAsync(correspondingItem);
                    }
                    correspondingItem.Type = fs.Type;
                    correspondingItem.FullPath = fs.Folder;
                    correspondingItem.BackupDriveLabel = fs.BackupDriveLabel;
                    correspondingItem.BackupFolder = fs.BackupFolder;
                }
                var hasChanges = db.ChangeTracker.HasChanges();
                await db.SaveChangesAsync();
                foreach(var sf in db.SourceFolders)
                {
                    log.Information($"{sf.DisplayName}: source path {sf.FullPath}, type {sf.Type.ToString()}, enabled = {(sf.BackupEnabled ? "true": "false")}");
                }
                if(hasChanges)
                {
                    var rrt = this.sp.GetRealtimeTask<RealTimeReplicationTask>();
                    rrt.Restart();
                }
                else
                {
                    log.Information($"Backup and replication configuration unchanged");
                }
                //var count = db.SourceFolders.Count();
                //log.LogInformation($"found {count} source folders in database");
                return null;
            }
        }
        private void SetupPipeline()
        {
            CreatePipeline(this);
        }
    }
}
