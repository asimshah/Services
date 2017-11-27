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

namespace Fastnet.Services.Tasks
{
    public class BackupService : ScheduledTask
    {
        private ServiceOptions options;
        private readonly string schedule;
        private ServiceDb db;
        private readonly WebDbContextFactory dbf;
        public BackupService(WebDbContextFactory dbf, IOptions<ServiceOptions> option, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.options = option.Value;
            this.dbf = dbf;
            var jobSchedule = this.options.Schedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
            schedule = jobSchedule?.Schedule ?? "0 0 1 */12 *";// default is At 00:00 AM, on day 1 of the month, every 12 months!! not useful!
            BeforeTaskStartsAsync = async (m) => { await OnTaskStart(); };
            
        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        public override string Schedule => schedule;
        private async Task OnTaskStart()
        {
            await SetupPipeline();
        }
        private async Task SetupPipeline()
        {
            List<IPipelineTask> list = new List<IPipelineTask>();
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                var sources = await db.SourceFolders.Where(x => x.BackupEnabled).ToArrayAsync();
                foreach (var sf in sources)
                {
                    list.Add(new BackupTask(options, sf.Id, dbf, CreatePipelineLogger<BackupTask>()));
                }
            }
            CreatePipeline(list);
        }

    }
}
