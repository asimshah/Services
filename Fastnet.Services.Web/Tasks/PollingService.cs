using Fastnet.Core;
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
    public class PollingService : RealtimeTask // ScheduledTask, IPipelineTask
    {
        private class pollWrapper
        {
            public PollingSchedule PollingSchedule { get; set; }
            public CrontabSchedule Schedule { get; set; }
            public DateTime LastRunTime { get; set; }
            public DateTime NextRunTime { get; set; }
        }
        private CancellationToken cancellationToken;
        private Dictionary<string, pollWrapper> pollingControl;
        private ServiceOptions options;
        //private readonly string schedule;
        public PollingService(/*IOptions<SchedulerOptions> schedulerOptions,*/ IOptions<ServiceOptions> serviceOptions, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            try
            {
                this.options = serviceOptions.Value;
                pollingControl = new Dictionary<string, pollWrapper>();
                //BeforeTaskStartsAsync = async (m) => { await BeforeStart(m); };
                //CreatePipeline(this);
                BeforeTaskStartsAsync = OnStartup;
            }
            catch (Exception)
            {
                Debugger.Break();
                throw;
            }
        }


        //public override TimeSpan StartAfter => TimeSpan.Zero;
        public string Name => "PollingService";
        //public TaskMethod ExecuteAsync => DoTask;
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;
            await StartAsync();
        }

        private async Task StartAsync()
        {
            while (!this.cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                //CleanUp();
            }
            log.Information($"CancellationRequested");
        }

        private Task OnStartup()
        {
            if (options.PollingSchedules?.Count() > 0)
            {
                log.Information($"Polling resolution is {options.PollingResolution} second(s)");
                var now = DateTime.Now;
                foreach (var item in options.PollingSchedules)
                {
                    
                    if (!pollingControl.ContainsKey(item.Url))
                    {
                        var pw = new pollWrapper
                        {
                            PollingSchedule = item,
                            Schedule = CrontabSchedule.Parse(item.Schedule),
                            LastRunTime = now
                        };
                        //pw.NextRunTime = pw.Schedule.GetNextOccurrence(DateTime.Now);
                        pollingControl.Add(item.Url, pw);
                        log.Information($"{item.Url} schedule is {CrontabSchedule.GetDescription(item.Schedule)}");
                    }
                }
                StartPolling();
            }
            else
            {
                log.Warning($"No polling schedules provided - check ServiceOptions");
            }
            return Task.CompletedTask;
        }
        private void StartPolling()
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var pw in pollingControl.Values)
                    {
                        pw.NextRunTime = pw.Schedule.GetNextOccurrence(pw.LastRunTime);
                        if(pw.NextRunTime < DateTime.Now)
                        {
                            await Poll(pw.PollingSchedule);
                            pw.LastRunTime = pw.NextRunTime;
                        }
                        else
                        {
                            log.Trace($"{pw.PollingSchedule.Url} next poll at {pw.NextRunTime.ToDefaultWithTime()}");
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(options.PollingResolution));
                }
            }, cancellationToken);
        }
        //private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        //{
        //    var currentTime = DateTime.Now;
        //    if (options.PollingSchedules != null)
        //    {
        //        foreach (var item in options.PollingSchedules)
        //        {
        //            var pw = EnsureWrapped(item);
        //            if (IsDue(currentTime, pw))
        //            {
        //                pw.LastRunTime = pw.NextRunTime;
        //                pw.NextRunTime = pw.Schedule.GetNextOccurrence(pw.NextRunTime);
        //                var r = await Poll(item);
        //                if (r)
        //                {
        //                    log.Information($"Poll {item.Url} succeeded");
        //                }
        //            }
        //        } 
        //    }
        //    //else
        //    //{
        //    //    log.Information("no urls provided");
        //    //}
        //    return null;
        //}
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
                        log.Error($"Poll to {item.Url} failed with response {response.StatusCode}");
                    } 
                }
            }
            catch(System.Net.Http.HttpRequestException hre)
            {
                log.Error($"Poll to {item.Url} failed: {hre.AllToString()}");
            }
            catch (Exception xe)
            {
                log.LogError(xe, $"Poll to {item.Url} failed");
            }
            return result;
        }
        //private string[] CollectExceptionMessages(Exception xe)
        //{
        //    var msgs = new List<string>();
        //    do
        //    {
        //        msgs.Add(xe.Message);
        //        xe = xe.InnerException;
        //    } while (xe != null);
        //    msgs.Reverse();
        //    return msgs.ToArray();
        //}
        //private bool IsDue(DateTime ct, pollWrapper pw)
        //{
        //    return pw.NextRunTime < ct && pw.LastRunTime != pw.NextRunTime;
        //}
        //private pollWrapper EnsureWrapped(PollingSchedule item)
        //{
        //    if (!pollingControl.ContainsKey(item.Url))
        //    {
        //        var pw = new pollWrapper
        //        {
        //            Schedule = CrontabSchedule.Parse(item.Schedule)//,
        //            //LastRunTime = DateTime.MinValue,
        //            //NextRunTime = DateTime.MinValue
        //        };
        //        pw.NextRunTime = pw.Schedule.GetNextOccurrence(DateTime.Now);
        //        pollingControl.Add(item.Url, pw);
        //    }
        //    return pollingControl[item.Url];
        //}
        //private Task BeforeStart(ScheduleMode m)
        //{
        //    if (options.PollingSchedules?.Count() > 0)
        //    {
        //        foreach (var item in options.PollingSchedules)
        //        {
        //            log.Information($"{item.Url}");
        //        }
        //    }
        //    else
        //    {
        //        log.Warning($"No polling schedules provided - check ServiceOptions");
        //    }
        //    return Task.CompletedTask;
        //}
        //private async Task AfterFinish(ScheduleMode m)
        //{
        //    log.Information("after finish");
        //    await Task.Delay(0);
        //}
    }
}
