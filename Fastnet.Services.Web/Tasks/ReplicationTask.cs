using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Fastnet.Services.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fastnet.Services.Tasks
{
    public class ReplicationTask : IPipelineTask
    {
        private class fileItem
        {
            public string fullPath { get; set; }
            public string comparablePath { get; set; }
        }
        private class fileComparer : IEqualityComparer<fileItem>
        {
            public bool Equals(fileItem x, fileItem y)
            {
                return x.comparablePath.ToLower().Equals(y.comparablePath.ToLower());
            }

            public int GetHashCode(fileItem obj)
            {
                return obj.comparablePath.GetHashCode();
            }
        }
        private readonly ILogger log;
        private readonly WebDbContextFactory dbf;
        private readonly int sourceFolderId;
        private readonly ServiceOptions options;
        private ServiceDb db;
        public ReplicationTask(ServiceOptions options, int sourceFolderId, WebDbContextFactory dbf, ILogger log)
        {
            this.options = options;
            this.log = log;
            this.dbf = dbf;
            this.sourceFolderId = sourceFolderId;
        }
        public string Name => $"ReplicationTask - source folder id {sourceFolderId}";
        public TaskMethod ExecuteAsync => DoTask;
        private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                var sf = await db.SourceFolders
                    .SingleAsync(x => x.Id == sourceFolderId);
                (bool available, string destinationFolder) = sf.GetDestinationFolder(options);
                if (available)
                {
                    EnsurePresent(destinationFolder);
                    var replicaFolder = Path.Combine(destinationFolder, "replica");
                    EnsurePresent(replicaFolder);
                    var deletionsFolder = Path.Combine(destinationFolder, "deletions");
                    EnsurePresent(deletionsFolder);
                    // stages of replication
                    // 1. find all files that are no longer in the source
                    //    and move them into the deletions folder as a zip file
                    // 2. find all files in the source that are not in the replica
                    //     and copy them
                    // 3. find all files in the source that have changed and copy them
                    // Note 2 and 3 may be in the same pass

                    ProcessDeletions(sf, replicaFolder, deletionsFolder);
                    UpdateReplica(sf, replicaFolder);
                }
                else
                {
                    log.LogWarning($"{sf.DisplayName}: {sf.FullPath} not replicated as destination is not available");
                }
            }
            return null;
        }

        private void UpdateReplica(SourceFolder sf, string replicaFolder)
        {
            var sourceFilelist = Directory.EnumerateFiles(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            foreach (var fileItem in sourceFilelist)
            {
                var sourceFile = new FileInfo(fileItem.fullPath);
                var replicaFile = new FileInfo(Path.Combine(replicaFolder, fileItem.comparablePath));
                if (File.Exists(replicaFile.FullName))
                {
                    if (sourceFile.Length != replicaFile.Length || sourceFile.LastWriteTimeUtc != replicaFile.LastWriteTimeUtc)
                    {
                        sourceFile.CopyTo(replicaFile.FullName, true);
                        log.LogInformation($"{sf.FullPath} has changed - replaced in replica folder");
                    }
                }
                else
                {
                    var containingFolder = replicaFile.Directory;
                    if (!containingFolder.Exists)
                    {
                        containingFolder.Create();
                    }
                    sourceFile.CopyTo(replicaFile.FullName);
                    log.LogInformation($"New file {sourceFile.FullName} found - copied to replica folder");
                }
            }
            var sourceDirectorylist = Directory.EnumerateDirectories(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            var replicaDirectorylist = Directory.EnumerateDirectories(replicaFolder, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(replicaFolder.Length + 1) });
            var additionalDirectories = sourceDirectorylist.Except(replicaDirectorylist, new fileComparer());
            foreach(var dir in additionalDirectories)
            {
                var replicaDirectory = Path.Combine(replicaFolder, dir.comparablePath);
                Directory.CreateDirectory(replicaDirectory);
                log.LogInformation($"Empty folder {replicaDirectory} created");
            }
        }

        private void ProcessDeletions(SourceFolder sf, string replicaFolder, string deletionsFolder)
        {
            var sourceFilelist = Directory.EnumerateFiles(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            var replicaFilelist = Directory.EnumerateFiles(replicaFolder, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(replicaFolder.Length + 1) });
            var deletionFileList = replicaFilelist.Except(sourceFilelist, new fileComparer());
            if (deletionFileList.Count() > 0)
            {
                // create zip file name
                // zip these files
                // remove them from replica
                var now = DateTimeOffset.Now;
                var datePart = $"{(now.ToString("yyyy.MM.dd.HHmmss"))}";
                var backupFileName = $"{datePart}.zip";
                using (var archive = new FileStream(Path.Combine(deletionsFolder, backupFileName), FileMode.CreateNew))
                {
                    using (var za = new ZipArchive(archive, ZipArchiveMode.Update))
                    {
                        foreach (var fi in deletionFileList)
                        {
                            var entry = za.CreateEntryFromFile(fi.fullPath, fi.comparablePath);
                            log.LogInformation($"Deleted file {entry.FullName} moved to {backupFileName}");
                            File.Delete(fi.fullPath);
                        }
                    }
                }
            }
            var sourceDirectorylist = Directory.EnumerateDirectories(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            var replicaDirectorylist = Directory.EnumerateDirectories(replicaFolder, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(replicaFolder.Length + 1) });
            var deletionDirectoryList = replicaDirectorylist.Except(sourceDirectorylist, new fileComparer());
            foreach (var fi in deletionDirectoryList)
            {
                if (Directory.Exists(fi.fullPath))
                {
                    Directory.Delete(fi.fullPath);
                    log.LogInformation($"Deleted folder {fi.fullPath} removed from replica");
                }
            }
        }

        private void EnsurePresent(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                log.LogInformation($"{folder} created");
            }
        }
    }
}
