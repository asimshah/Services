using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Services.Web;
using System.IO;

namespace Fastnet.Services.Tasks
{
    public class BackupService : ScheduledTask
    {
        //private string lastBackupDrive;
        private ServiceOptions options;
        //private readonly string schedule;
        private ServiceDb db;
        private readonly ServiceDbContextFactory dbf;
        public BackupService(ServiceDbContextFactory dbf, /*IOptions<SchedulerOptions> schedulerOptions,*/ IOptions<ServiceOptions> option, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.options = option.Value;
            this.dbf = dbf;
            //var serviceSchedule = schedulerOptions.Value.Schedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
            //schedule = serviceSchedule?.Schedule ?? "0 0 1 */12 *";// default is At 00:00 AM, on day 1 of the month, every 12 months!! not useful!
            BeforeTaskStartsAsync = async (m) => { await OnTaskStart(); };
            
        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        //public override string Schedule => schedule;
        private async Task OnTaskStart()
        {
            //if(options.EnsureBackupDriveReady)
            //{
            //    SpinUpBackupDrive();
            //}
            await SetupPipeline();
        }

        //private void SpinUpBackupDrive()
        //{
        //    try
        //    {
        //        var available = options.IsBackupDestinationAvailable();
        //        if(available)
        //        {
        //            var fi = new FileInfo(options.GetDefaultBackupDestination());
        //            lastBackupDrive = fi.Directory.Root.Name;
        //        }
        //        else if(lastBackupDrive != null)
        //        {
        //            var folder = Path.Combine(lastBackupDrive, options.BackupFolder);
        //            if(!Directory.Exists(folder))
        //            {
        //                Directory.CreateDirectory(folder);
        //                log.LogInformation($"{folder} created");
        //            }
        //            var filename = Path.Combine(folder, "probe.txt");
        //            if(File.Exists(filename))
        //            {
        //                File.Delete(filename);
        //            }
        //            File.CreateText(filename);
        //            File.Delete(filename);
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        private async Task SetupPipeline()
        {
            List<IPipelineTask> list = new List<IPipelineTask>();
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                var sources = await db.SourceFolders.Where(x => x.BackupEnabled).ToArrayAsync();
                foreach (var sf in sources)
                {
                    switch (sf.Type)
                    {
                        case SourceType.StandardSource:
                        case SourceType.Website:
                            list.Add(new BackupTask(options, sf.Id, dbf, CreatePipelineLogger<BackupTask>()));
                            break;
                        default:
                            break;
                    }
                    
                }
            }
            CreatePipeline(list);
        }

    }
}
