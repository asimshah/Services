using Fastnet.Core.Cron;
using Fastnet.Core.Web;
using Fastnet.Services.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Services.Tasks
{
    public class PollingService : ScheduledTask, IPipelineTask
    {
        private class pollWrapper
        {
            public CrontabSchedule Schedule { get; set; }
            public DateTime LastRunTime { get; set; }
            public DateTime NextRunTime { get; set; }
        }
        private Dictionary<string, pollWrapper> pollingControl;
        private ServiceOptions options;
        private readonly string schedule;
        public PollingService(IOptions<ServiceOptions> option, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            try
            {
                this.options = option.Value;
                var serviceSchedule = this.options.ServiceSchedules?.FirstOrDefault(sc => string.Compare(sc.Name, this.GetType().Name) == 0);
                schedule = serviceSchedule.Schedule;
                pollingControl = new Dictionary<string, pollWrapper>();
                //BeforeTaskStartsAsync = async (m) => { await BeforeStart(m); };
                //AfterTaskCompletesAsync = async (m) => { await AfterFinish(m); };
                CreatePipeline(this);
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }
        public override TimeSpan StartAfter => TimeSpan.Zero;
        public override string Schedule => schedule;
        public string Name => "PollingService";
        public TaskMethod ExecuteAsync => DoTask;
        private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            var currentTime = DateTime.Now;
            if (options.PollingSchedules != null)
            {
                foreach (var item in options.PollingSchedules)
                {
                    var pw = EnsureWrapped(item);
                    if (IsDue(currentTime, pw))
                    {
                        pw.LastRunTime = pw.NextRunTime;
                        pw.NextRunTime = pw.Schedule.GetNextOccurrence(pw.NextRunTime);
                        var r = await Poll(item);
                        if (r)
                        {
                            log.LogInformation($"Poll {item.Url} succeeded");
                        }
                    }
                } 
            }
            else
            {
                log.LogInformation("no urls provided");
            }
            return null;
        }
        private async Task<bool> Poll(PollingSchedule item)
        {
            bool result = false;
            try
            {
                using (HttpClient client = new HttpClient())
                {                    
                    HttpResponseMessage response = await client.GetAsync(item.Url);
                    if (response.IsSuccessStatusCode)
                    {
                        result = true;
                    }
                    else
                    {
                        log.LogError($"Poll to {item.Url} failed with response {response.StatusCode}");
                    } 
                }
            }
            catch (Exception xe)
            {
                log.LogError(xe, $"Poll to {item.Url} failed");
            }
            return result;
        }
        private bool IsDue(DateTime ct, pollWrapper pw)
        {
            return pw.NextRunTime < ct && pw.LastRunTime != pw.NextRunTime;
        }
        private pollWrapper EnsureWrapped(PollingSchedule item)
        {
            if (!pollingControl.ContainsKey(item.Url))
            {
                var pw = new pollWrapper
                {
                    Schedule = CrontabSchedule.Parse(item.Schedule)//,
                    //LastRunTime = DateTime.MinValue,
                    //NextRunTime = DateTime.MinValue
                };
                pw.NextRunTime = pw.Schedule.GetNextOccurrence(DateTime.Now);
                pollingControl.Add(item.Url, pw);
            }
            return pollingControl[item.Url];
        }
        private async Task BeforeStart(ScheduleMode m)
        {
            log.LogInformation("before start");
            await Task.Delay(0);
        }
        private async Task AfterFinish(ScheduleMode m)
        {
            log.LogInformation("after finish");
            await Task.Delay(0);
        }
    }
}
