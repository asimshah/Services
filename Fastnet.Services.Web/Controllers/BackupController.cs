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
        private readonly SchedulerService schedulerService;
        private ServiceOptions serviceOptions;
        public BackupController(IOptionsMonitor<ServiceOptions> options, IHostedService hostedService, ILogger<BackupController> logger, ServiceDb serviceDb, IHostingEnvironment env) : base(env)
        {
            this.serviceOptions = options.CurrentValue;
            options.OnChangeWithDelay((opt) => this.serviceOptions = opt);
            this.log = logger;
            this.serviceDb = serviceDb;
            this.schedulerService = hostedService as SchedulerService;
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
                log.LogInformation($"{available}, {folder}");
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
        //[HttpGet("get/backup/destinationStatus")]
        //public Task<IActionResult> GetBackupDestinationStatus()
        //{
        //    var r = serviceOptions.IsBackupDestinationAvailable();
        //    return Task.FromResult(SuccessDataResult(r));
        //}
        //[HttpGet("get/backup/destination")]
        //public Task<IActionResult> GetBackupDestination()
        //{
        //    string volumeLabel = this.serviceOptions.BackupDriveLabel;
        //    string destinationFolder = string.Empty;
        //    var r = serviceOptions.IsBackupDestinationAvailable();
        //    if(r)
        //    {
        //        destinationFolder = serviceOptions.GetDefaultBackupDestination();
        //    }
        //    return Task.FromResult(SuccessDataResult(new { volumeLabel = volumeLabel, available = r, destination = destinationFolder }));
        //}
        //[HttpGet("log/driveinfo")]
        //public Task<IActionResult> DriveInformation()
        //{
        //    foreach (var drive in DriveInfo.GetDrives())
        //    {
        //        log.LogInformation($"Drive {drive.Name}, Label: {drive.VolumeLabel}, ready: {drive.IsReady}, type: {drive.DriveType}");
        //    }
        //    var fi = new FileInfo(this.serviceOptions.GetBackupDestination());
        //    log.LogInformation($"backup root is {fi.Directory.Root.FullName}");
        //    return Task.FromResult(SuccessDataResult(null));
        //}
    }
}