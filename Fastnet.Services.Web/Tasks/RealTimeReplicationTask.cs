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
    //static class _ex
    //{
    //    public static void OnChanged2(this FileSystemWatcher fileSystemWatcher,
    //        Action<IEnumerable<(WatcherChangeTypes type, string path, string oldPath)>> action, int delay = 2000,
    //        Action<Exception> onError = null)
    //    {
    //        var changes = new List<(WatcherChangeTypes type, string path, string oldPath)>();
    //        bool changeDelayStarted = false;
    //        void watcherEvent(object sender, EventArgs e)
    //        {
    //            Debug.Print($"{e.GetType().Name}");
    //            if (!changeDelayStarted)
    //            {
    //                changeDelayStarted = true;
    //                Task.Run(async () =>
    //                {
    //                    await Task.Delay(TimeSpan.FromMilliseconds(delay));
    //                    try
    //                    {
    //                        action(changes);
    //                        changes.Clear();
    //                    }
    //                    catch (Exception xe)
    //                    {
    //                        Debug.Print($"{xe.GetType().Name}");
    //                        //throw;
    //                    }
    //                    finally
    //                    {
    //                        changeDelayStarted = false;
    //                    }
    //                });
    //            }
    //            switch (e)
    //            {
    //                case RenamedEventArgs rea:
    //                    changes.Add((rea.ChangeType, rea.FullPath, rea.OldFullPath));
    //                    break;
    //                case FileSystemEventArgs fsea:
    //                    changes.Add((fsea.ChangeType, fsea.FullPath, null));
    //                    break;
    //                case ErrorEventArgs eea:
    //                    onError?.DynamicInvoke(eea.GetException());
    //                    break;
    //            }
    //        }
    //        fileSystemWatcher.IncludeSubdirectories = true;
    //        fileSystemWatcher.Changed -= watcherEvent;
    //        fileSystemWatcher.Created -= watcherEvent;
    //        fileSystemWatcher.Deleted -= watcherEvent;
    //        fileSystemWatcher.Renamed -= watcherEvent;
    //        fileSystemWatcher.Error -= watcherEvent;

    //        fileSystemWatcher.Changed += watcherEvent;
    //        fileSystemWatcher.Created += watcherEvent;
    //        fileSystemWatcher.Deleted += watcherEvent;
    //        fileSystemWatcher.Renamed += watcherEvent;

    //        fileSystemWatcher.Error += watcherEvent;

    //        fileSystemWatcher.EnableRaisingEvents = true;
    //    }
    //}
    public class RealTimeReplicationTask : RealtimeTask
    {
        //private readonly ILogger log;
        private SchedulerService schedulerService;
        private readonly IServiceProvider sp;
        private readonly ServiceDbContextFactory dbf;
        private IDictionary<SourceFolder, FileSystemWatcher> sources;
        //public RealTimeReplicationTask(ServiceDbContextFactory dbf, IServiceProvider sp, ILogger<RealTimeReplicationTask> logger, IApplicationLifetime applicationLifetime)
        //{
        //    this.sp = sp;
        //    this.log = logger;
        //    this.dbf = dbf;
        //    applicationLifetime.ApplicationStopping.Register(() => CleanUp());
        //}

        public RealTimeReplicationTask(ServiceDbContextFactory dbf, IHostedService hostedService, IServiceProvider sp, ILoggerFactory loggerFactory) : base(loggerFactory)
        {
            this.sp = sp;
            this.dbf = dbf;
            this.schedulerService = hostedService as SchedulerService;
            this.BeforeTaskStartsAsync = OnStartup;
            this.AfterTaskCompletesAsync = CleanUp;
        }

        ~RealTimeReplicationTask()
        {
            this.log.LogInformation($"~{nameof(RealTimeReplicationTask)}");
        }
        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.log.LogInformation($"{nameof(ExecuteAsync)}");
            //await Initialise();
            while(!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken);
                //log.LogInformation($"still alive ...");
            }
            if(cancellationToken.IsCancellationRequested)
            {
                await CleanUp();
            }
            log.LogInformation($"CancellationRequested");
        }
        private async Task CleanUp()
        {
            foreach(var kvp in sources)
            {
                //kvp.Value.
                kvp.Value.Dispose();
            }
            await Task.Delay(0);
            return;
        }
        private async Task Initialise()
        {
            using (var db = dbf.GetWebDbContext<ServiceDb>())
            {
                sources = new Dictionary<SourceFolder, FileSystemWatcher>();
                var folders = await db.SourceFolders.Where(x => x.BackupEnabled && x.Type == Web.SourceType.ReplicationSource).ToArrayAsync();        
                foreach (var folder in folders.Select(x => x))
                {
                    var fs = AddMonitor(folder);
                    sources.Add(folder, fs);
                }
            }
        }
        private FileSystemWatcher AddMonitor(SourceFolder sf)
        {
            var fs = new FileSystemWatcher(sf.FullPath);
            fs.IncludeSubdirectories = true;
            log.LogInformation($"File System Monitor added for {sf.FullPath}");
            fs.OnChanged((actions) => {
                OnChangeOccurred(sf, actions);
            }, 10000, (exception) => {
                log.LogError(exception);
            });
            return fs;
        }
        private void OnChangeOccurred(SourceFolder sf, IEnumerable<(WatcherChangeTypes type, string path, string oldPath)> actions)
        {
            if(schedulerService == null)
            {
                // contsructor injection for SchedulerService does not work - is it too soon?
                this.schedulerService = sp.GetService<IHostedService>() as SchedulerService;
            }
            foreach(var item in actions)
            {
                switch(item.type)
                {
                    case WatcherChangeTypes t when (t & WatcherChangeTypes.All) > 0 :
                        log.LogInformation($"{item.type.ToString()}, path: {item.path}, oldPath: {item.oldPath ?? "none"}");
                        break;
                }
            }
            this.schedulerService.ExecuteNow<ReplicationService>();
        }
        private async Task OnStartup()
        {
            await Initialise();
        }
    }
}
