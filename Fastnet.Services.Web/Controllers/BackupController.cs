using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Fastnet.Services.Data;
using Fastnet.Core.Web.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Fastnet.Core.Web;
using Fastnet.Services.Tasks;
using Microsoft.Extensions.Options;
using System.IO;
using Fastnet.Core;

namespace Fastnet.Services.Web.Controllers
{
    [Produces("application/json")]
    [Route("backup")]
    public class BackupController : BaseController
    {
        private readonly ILogger log;
        private readonly ServiceDb serviceDb;
        //private readonly IServiceProvider sp;
        private readonly SchedulerService schedulerService;
        private ServiceOptions serviceOptions;
        public BackupController(IOptionsMonitor<ServiceOptions> options, IServiceProvider sp, ILogger<BackupController> logger, ServiceDb serviceDb, IHostingEnvironment env) : base(env)
        {
            this.serviceOptions = options.CurrentValue;
            options.OnChangeWithDelay((opt) => this.serviceOptions = opt);
            this.log = logger;
            this.serviceDb = serviceDb;
            this.schedulerService = sp.GetSchedulerService();// hostedService as SchedulerService;
            //this.serviceDb.Database.EnsureCreated();
        }
        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var sourceFolders = await serviceDb.SourceFolders
                //.Include(x => x.Backups)
                .ToArrayAsync();
            foreach(var sf in sourceFolders)
            {
                (bool available, string folder) = sf.GetDestinationFolder(serviceOptions);
                log.Information($"{available}, {folder}");
            }

            return SuccessDataResult(sourceFolders);
        }
        [HttpGet("get/enabled/sources")]
        public async Task<IActionResult> GetEnabledSourceFolders()
        {
            serviceDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var sourceFolders = await serviceDb.SourceFolders
                .Include(x => x.Backups)
                .Where(x => x.BackupEnabled)
                .ToArrayAsync();
            return SuccessDataResult(sourceFolders);
        }
        [HttpGet("get/sources")]
        public async Task<IActionResult> GetSourceFolders()
        {
            serviceDb.ChangeTracker.AutoDetectChangesEnabled = false;
            var sourceFolders = await serviceDb.SourceFolders
                //.Include(x => x.Backups)
                .ToArrayAsync();
            return SuccessDataResult(sourceFolders);
        }
        [HttpGet("set/source/{id}/{enabled}")]
        public async Task<IActionResult> SetSourceBackup(int id, bool enabled)
        {
            var sf = await serviceDb.SourceFolders.FindAsync(id);
            sf.BackupEnabled = enabled;
            await serviceDb.SaveChangesAsync();
            return SuccessDataResult(null);
        }
        [HttpGet("reconfigure")]
        public Task<IActionResult> Reconfigure()
        {
            this.schedulerService.ExecuteNow<ConfigureBackups>();
            return Task.FromResult(SuccessDataResult(null));
        }
        [HttpGet("start/backupservice")]
        public Task<IActionResult> StartBackupService()
        {
            this.schedulerService.ExecuteNow<BackupService>();
            return Task.FromResult(SuccessDataResult(null));
        }
        [HttpGet("start/replicationservice")]
        public Task<IActionResult> StartReplicationService()
        {
            this.schedulerService.ExecuteNow<ReplicationService>();
            return Task.FromResult(SuccessDataResult(null));
        }
    }
}