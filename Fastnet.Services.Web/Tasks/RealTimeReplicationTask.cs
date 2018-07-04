using Fastnet.Core;
using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Fastnet.Services.Tasks
{
    public class RealTimeReplicationTask : RealtimeTask
    {
        private SchedulerService schedulerService;
        private readonly IServiceProvider sp;
        private readonly ServiceDbContextFactory dbf;
        private readonly FileSystemMonitorFactory mf;
        private IDictionary<SourceFolder, FileSystemMonitor> sources;
        private CancellationToken cancellationToken;
        public RealTimeReplicationTask(ServiceDbContextFactory dbf, FileSystemMonitorFactory mf, IServiceProvider sp, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.sp = sp;
            this.dbf = dbf;
            this.mf = mf;
            this.schedulerService = sp.GetSchedulerService();// hostedService as SchedulerService;
            this.BeforeTaskStartsAsync = OnStartup;
            //this.AfterTaskCompletesAsync = CleanUp;
        }

        ~RealTimeReplicationTask()
        {
            log.Information($"~{nameof(RealTimeReplicationTask)}");
        }
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            //log.Information($"{nameof(ExecuteAsync)}");
            this.cancellationToken = cancellationToken;
            //await Initialise();
            await StartAsync();
        }

        private async Task StartAsync()
        {
            while (!this.cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken);
                //log.LogInformation($"still alive ...");
            }
            if (cancellationToken.IsCancellationRequested)
            {
                CleanUp();
            }
            log.Information($"CancellationRequested");
        }

        public void Restart()
        {
            try
            {
                log.Information($"{nameof(Restart)}");
                CleanUp();
                Initialise();
            }
            catch (Exception xe)
            {
                log.Error(xe, "Restart failed");
                //Debugger.Break();
                //throw;
            }
            //Task.Run(async () => {

            //    //await StartAsync();
            //});
            return;
        }
        private void CleanUp()
        {
            if (sources != null)
            {
                foreach (var kvp in sources)
                {
                    kvp.Value.Stop();
                    kvp.Value.Dispose();
                }
            }
            //await Task.Delay(0);
            return;
        }
        private void Initialise()
        {
            using (var db = dbf.GetWebDbContext<ServiceDb>())
            {
                sources = new Dictionary<SourceFolder, FileSystemMonitor>();
                var folders = db.SourceFolders.Where(x => x.BackupEnabled && x.Type == Web.SourceType.ReplicationSource).ToArray();        
                foreach (var folder in folders.Select(x => x))
                {
                    if (Directory.Exists(folder.FullPath))
                    {
                        var fsm = AddMonitor(folder);
                        sources.Add(folder, fsm);
                    }
                    else
                    {
                        log.Warning($"{folder.DisplayName}: path {folder.FullPath} not found");
                    }
                }
            }
            foreach (var kvp in sources)
            {
                kvp.Value.Start();
            }
        }
        private FileSystemMonitor AddMonitor(SourceFolder sf)
        {
            var fsm = this.mf.CreateMonitor(sf.FullPath, (changes) => {
                OnChangeOccurred(sf, changes);
            });
            fsm.IncludeSubdirectories = true;
            //var fs = new FileSystemWatcher(sf.FullPath);
            //fs.IncludeSubdirectories = true;
            //log.LogInformation($"File System Monitor added for {sf.FullPath}");
            //fs.OnChanged((actions) => {
            //    OnChangeOccurred(sf, actions);
            //}, 10000, (exception) => {
            //    log.LogError(exception);
            //});
            return fsm;
        }
        private void OnChangeOccurred(SourceFolder sf, IEnumerable<FileSystemMonitorEvent> actions)
        {
            foreach(var item in actions)
            {
                switch(item.Type)
                {
                    case WatcherChangeTypes t when (t & WatcherChangeTypes.All) > 0 :
                        if (!item.Path.Contains("pr$obe.txt"))
                        {
                            log.Information($"{item.Type.ToString()}, path: {item.Path}, oldPath: {item.OldPath ?? "none"}");
                        }
                        break;
                }
            }
            // kick off the replication service so that these changes will be replicated 
            this.schedulerService.ExecuteNow<ReplicationService>();
        }
        private Task OnStartup()
        {
            Initialise();
            return Task.FromResult(0);
        }
    }
}
