using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Fastnet.Services.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Services.Tasks
{
    public class ReplicationService: ScheduledTask
    {
        private ServiceOptions options;
        private readonly string schedule;
        private ServiceDb db;
        private readonly ServiceDbContextFactory dbf;
        public ReplicationService(ServiceDbContextFactory dbf,/* IOptions<SchedulerOptions> schedulerOptions,*/ IOptions<ServiceOptions> serviceOptions, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.options = serviceOptions.Value;
            this.dbf = dbf;
            //var serviceSchedule = schedulerOptions.Value.Schedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
            //schedule = serviceSchedule?.Schedule ?? "0 0 1 */12 *";// default is At 00:00 AM, on day 1 of the month, every 12 months!! not useful!
            BeforeTaskStartsAsync = async (m) => { await OnTaskStart(); };

        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        //public override string Schedule => schedule;
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
                    switch (sf.Type)
                    {
                        case SourceType.ReplicationSource:
                            list.Add(new ReplicationTask(options, sf.Id, dbf, CreatePipelineLogger<ReplicationTask>()));
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
