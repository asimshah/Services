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
        private readonly SchedulerService schedulerService;
        private readonly ServiceDbContextFactory dbf;
        //public ConfigureBackups(ILoggerFactory loggerFactory, WebDbContextFactory dbf, IHostedService hostedService,
        //    IOptionsMonitor<ServiceOptions> options) : base(loggerFactory)
        public ConfigureBackups(ILoggerFactory loggerFactory, ServiceDbContextFactory dbf, IHostedService hostedService,
            IOptionsMonitor<ServiceOptions> options) : base(loggerFactory)
        {
            //log.LogInformation("Configuration constructor called");
            this.dbf = dbf;
            this.options = options.CurrentValue;
            this.schedulerService = hostedService as SchedulerService;
            options.OnChangeWithDelay((opt) =>
            {
                this.options = opt;
                SetupPipeline();
                log.LogInformation("Configuration (appsettings) changed, starting ConfigureBackups");
                this.schedulerService.ExecuteNow<ConfigureBackups>();
            });
            var backupSchedule = this.options.ServiceSchedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
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
                        log.LogInformation($"Source {existingSource.DisplayName}, folder {existingSource.FullPath} removed");
                        await db.SaveChangesAsync();
                    }
                }
                foreach (var fs in options.FolderSources)
                {
                    var correspondingItem = existingSources.SingleOrDefault(x => string.Compare(x.DisplayName, fs.DisplayName, true) == 0);
                    if(correspondingItem == null)
                    {
                        correspondingItem = new SourceFolder
                        {
                            BackupEnabled = false,
                            DisplayName = fs.DisplayName,
                            FullPath = fs.Folder,
                            ScheduledTime = 3,
                            Type = fs.Type,
                            ConfiguredOn = DateTimeOffset.Now
                        };
                        await db.SourceFolders.AddAsync(correspondingItem);
                    }
                    correspondingItem.BackupDriveLabel = fs.BackupDriveLabel;
                    correspondingItem.BackupFolder = fs.BackupFolder;
                }
                //var staleList = await db.SourceFolders
                //    .Include(x => x.Backups)
                //    .Where(x => x.ConfiguredOn < configuredOn).ToArrayAsync();
                //foreach (var sf in staleList)
                //{
                //    //var backups = sf.Backups.ToArray();
                //    //db.Backups.RemoveRange(backups);
                //    //db.SourceFolders.Remove(sf);
                //    //log.LogInformation($"{sf.DisplayName} {sf.FullPath} (with {backups.Count()} backups) removed");
                //}
                await db.SaveChangesAsync();
                var count = db.SourceFolders.Count();
                log.LogInformation($"found {count} source folders in database");
                return null;// Task.FromResult<ITaskState>(null); 
            }
        }
        //private async Task AddFolderSource(DateTimeOffset configuredOn, FolderSource fs)
        //{
        //    var folder = fs.Folder.ToLower();
        //    var sf = await db.SourceFolders.SingleOrDefaultAsync(x => x.FullPath == folder);
        //    if (sf != null && sf.DisplayName != fs.DisplayName)
        //    {
        //        log.LogError($"{folder} is already in use with a different display name: {sf.DisplayName}");
        //        //log.LogError($"Source folder {folder} already exists in the database: display name {sf.DisplayName}");
        //    }
        //    else
        //    {
        //        sf = await db.SourceFolders.SingleOrDefaultAsync(x => string.Compare(x.DisplayName, fs.DisplayName, true) == 0);
        //        if (sf == null)
        //        {
        //            sf = new SourceFolder
        //            {
        //                BackupEnabled = false,
        //                DisplayName = fs.DisplayName,
        //                FullPath = folder,
        //                ScheduledTime = 3,
        //                Type = fs.Type
        //            };
        //            await db.SourceFolders.AddAsync(sf);
        //        }
        //        else
        //        {
        //            sf.FullPath = fs.Folder.ToLower();
        //            sf.Type = fs.Type;
        //        }
        //        sf.ConfiguredOn = configuredOn;
        //        await db.SaveChangesAsync();
        //    }
        //}
        //private async Task AddSiteFolder(DateTimeOffset configuredOn, Site site)
        //{
        //    try
        //    {
        //        foreach (var app in site.Applications.ToArray())
        //        {
        //            string appPath = app.Path.Substring(1);
        //            var vds = app.VirtualDirectories;
        //            foreach (VirtualDirectory vd in vds)
        //            {
        //                var fp = Environment.ExpandEnvironmentVariables(vd.PhysicalPath).ToLower();
        //                var myself = Path.Combine(fp, "fastnet.services.web.dll");
        //                if (!File.Exists(myself)) // dont't include myself!
        //                {
        //                    var sf = await db.SourceFolders.SingleOrDefaultAsync(x => x.FullPath == fp);
        //                    if (sf == null)
        //                    {
        //                        sf = new SourceFolder
        //                        {
        //                            SiteName = site.Name,
        //                            BackupEnabled = false,
        //                            DisplayName = site.Name,
        //                            Path = vd.Path == "/" ? appPath : $"{appPath}/{vd.Path}",
        //                            FullPath = fp,
        //                            ScheduledTime = 3,
        //                            Type = SourceType.Website

        //                        };
        //                        await db.SourceFolders.AddAsync(sf);
        //                    }
        //                    else
        //                    {
        //                        if (sf.Type == SourceType.Folder)
        //                        {
        //                            log.LogWarning($"Source folder {fp} is in the database as type {sf.Type.ToString()} - should be {SourceType.Website.ToString()}");
        //                        }
        //                    }
        //                    sf.ConfiguredOn = configuredOn;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception xe)
        //    {
        //        Debugger.Break();
        //        //throw;
        //    }
        //    await db.SaveChangesAsync();
        //}
        private void SetupPipeline()
        {
            CreatePipeline(this);
        }
    }
}
