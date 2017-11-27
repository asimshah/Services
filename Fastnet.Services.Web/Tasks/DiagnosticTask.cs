using Fastnet.Core.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Extensions.Options;
//using Microsoft.Web.Administration;
using Fastnet.Services.Web;

namespace Fastnet.Services.Tasks
{
    public class DiagnosticTask : ScheduledTask, IPipelineTask
    {
        private ServiceOptions options;
        private readonly string schedule;
        public DiagnosticTask(ILoggerFactory loggerFactory, IOptionsMonitor<ServiceOptions> options) : base(loggerFactory)
        {
            this.options = options.CurrentValue;
            var jobSchedule = this.options.Schedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
            schedule = jobSchedule?.Schedule ?? "0 0 1 */12 *";// default is At 00:00 AM, on day 1 of the month, every 12 months!! not useful!
            SetupPipeline();
        }

        private void SetupPipeline()
        {
            CreatePipeline(this);
        }

        public override TimeSpan StartAfter => TimeSpan.Zero;

        public override string Schedule => schedule;

        public string Name => "DiagnosticTask";

        public TaskMethod ExecuteAsync => DoTask;

        private  Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            log.LogInformation($"DoTask() called");
            //GetSites();
            return Task.FromResult<ITaskState>(null);
        }
        //private void GetSites()
        //{
        //    var sm = new ServerManager();
        //    foreach (var site in sm.Sites)
        //    {                
        //        ShowSiteInformation(site);
        //    }
        //}

        //private void ShowSiteInformation(Site site)
        //{
        //    log.LogInformation($"Site {site.Name}");
        //    foreach(var b in site.Bindings)
        //    {
        //        log.LogInformation($"Binding {b.BindingInformation}, {b.EndPoint.ToString()}, {b.Host}");
        //    }
        //    var vds = site.Applications[0].VirtualDirectories;
        //    foreach(var  vd in vds)
        //    {
        //        log.LogInformation($"\tPath {vd.Path}, physical path {Environment.ExpandEnvironmentVariables(vd.PhysicalPath)} ");
        //    }
        //}
        //public TaskMethod ExecuteAsync(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        //{

        //}
    }
}
