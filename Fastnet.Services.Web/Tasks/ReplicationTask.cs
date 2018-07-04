using Fastnet.Core;
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
    public static class fileInfoExtensions
    {
        public static async Task CopyToExactAsync(this FileInfo sourceFile, string targetName)
        {
            using (var ss = sourceFile.OpenRead())
            {
                using (var ds = File.Create(targetName))
                {
                    await ss.CopyToAsync(ds);
                }
            }
            var destination = new FileInfo(targetName);
            if (destination.IsReadOnly)
            {

                destination.IsReadOnly = false;
                destination.CreationTime = sourceFile.CreationTime;
                destination.LastWriteTime = sourceFile.LastWriteTime;
                destination.LastAccessTime = sourceFile.LastAccessTime;
                destination.IsReadOnly = true;
            }
            else
            {
                destination.CreationTime = sourceFile.CreationTime;
                destination.LastWriteTime = sourceFile.LastWriteTime;
                destination.LastAccessTime = sourceFile.LastAccessTime;
            }
        }
    }
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

        private const string replicaRootName = "replica";
        private const string deletionsRootName = "deletions";
        private readonly ILogger log;
        private readonly WebDbContextFactory dbf;
        //private readonly int sourceFolderId;
        private SourceFolder sourceFolder;
        private readonly ServiceOptions options;
        private ServiceDb db;
        public ReplicationTask(ServiceOptions options, int sourceFolderId, WebDbContextFactory dbf, ILogger log)
        {
            this.options = options;
            this.log = log;
            this.dbf = dbf;
            using (db = dbf.GetWebDbContext<ServiceDb>())
            {
                this.sourceFolder = db.SourceFolders
                    .Single(x => x.Id == sourceFolderId);
            }
            //this.sourceFolderId = sourceFolderId;
        }
        public string Name => $"ReplicationTask - {this.sourceFolder.DisplayName}";
        public TaskMethod ExecuteAsync => DoTask;
        private async Task<ITaskState> DoTask(ITaskState taskState, ScheduleMode mode, CancellationToken cancellationToken)
        {
            try
            {
                using (db = dbf.GetWebDbContext<ServiceDb>())
                {
                    var sf = await db.SourceFolders
                        .SingleAsync(x => x.Id == this.sourceFolder.Id);
                    if (sf.CanAccess())
                    {
                        (bool available, string destinationFolder) = sf.GetDestinationFolder(options);
                        if (available)
                        {
                            EnsurePresent(destinationFolder);
                            var replicaFolder = Path.Combine(destinationFolder, replicaRootName);
                            EnsurePresent(replicaFolder);
                            var deletionsFolder = Path.Combine(destinationFolder, deletionsRootName);
                            EnsurePresent(deletionsFolder);
                            // stages of replication
                            // 1. find all files that are no longer in the source
                            //    and move them into the deletions folder
                            // 2. find all files in the source that are not in the replica
                            //     and copy them
                            // 3. find all files in the source that have changed and copy them
                            // Note 2 and 3 may be in the same pass

                            await ProcessDeletionsAsync(sf, replicaFolder, deletionsFolder);
                            await UpdateReplicaAsync(sf, replicaFolder);
                            log.Information($"{sf.DisplayName}: {sf.FullPath} replication finished");
                        }
                        else
                        {
                            log.Warning($"{sf.DisplayName}: {sf.FullPath} not replicated as destination is not available");
                        }
                    }
                    else
                    {
                        log.Warning($"{sf.DisplayName}, cannot access {sf.FullPath}");
                    }
                }
            }
            catch (Exception xe)
            {
                log.Error(xe);
            }
            return null;
        }

        //private bool CanAccess(string fullPath)
        //{
        //    var result = false;
        //    try
        //    {
        //        if (Directory.EnumerateFileSystemEntries(fullPath) != null)
        //        {
        //            result = true;
        //        }
        //    }
        //    catch { }
        //    return result;
        //}

        private async Task UpdateReplicaAsync(SourceFolder sf, string replicaFolder)
        {
            log.Debug($"{sf.DisplayName}, searching for new and modified files...");
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
                        await sourceFile.CopyToExactAsync(replicaFile.FullName);
                        log.Debug($"{sourceFile.FullName} has changed - replaced in replica folder");
                    }
                }
                else
                {
                    var containingFolder = replicaFile.Directory;
                    if (!containingFolder.Exists)
                    {
                        containingFolder.Create();
                    }
                    await sourceFile.CopyToExactAsync(replicaFile.FullName);
                    log.Debug($"New file {sourceFile.FullName} found - copied to replica folder");
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
                log.Debug($"Empty folder {replicaDirectory} created");
            }
        }

        private async Task ProcessDeletionsAsync(SourceFolder sf, string replicaFolder, string deletionsFolder)
        {
            log.Debug($"{sf.DisplayName}, searching for deleted files and folders...");
            var sourceFilelist = Directory.EnumerateFiles(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            var replicaFilelist = Directory.EnumerateFiles(replicaFolder, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(replicaFolder.Length + 1) });
            var deletionFileList = replicaFilelist.Except(sourceFilelist, new fileComparer());
            if (deletionFileList.Count() > 0)
            {
                var timeFolder = DateTimeOffset.Now.ToString("yyyy.MM.dd.HHmmss");
                foreach(var fileName in deletionFileList)
                {
                    var fileInfo = new FileInfo(fileName.fullPath);
                    var name = fileName.fullPath.Substring(replicaFolder.Length + 1);
                    var targetName = Path.Combine(deletionsFolder, timeFolder, name);
                    var folder = Path.GetDirectoryName(targetName);
                    if(!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    await fileInfo.CopyToExactAsync(targetName);
                    log.Debug($"Deleted file {fileInfo.FullName} moved to {targetName}");
                    fileInfo.IsReadOnly = false;
                    fileInfo.Delete();
                }
            }
            var sourceDirectorylist = Directory.EnumerateDirectories(sf.FullPath, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(sf.FullPath.Length + 1) });
            var replicaDirectorylist = Directory.EnumerateDirectories(replicaFolder, "*.*", SearchOption.AllDirectories)
                .Select(x => new fileItem { fullPath = x, comparablePath = x.Substring(replicaFolder.Length + 1) });
            var deletionDirectoryList = replicaDirectorylist.Except(sourceDirectorylist, new fileComparer());
            var deletionQueue = new Queue<DirectoryInfo>(deletionDirectoryList.Select(x => new DirectoryInfo(x.fullPath)));
            while(deletionQueue.Count() > 0)
            {
                var item = deletionQueue.Dequeue();
                if (item.EnumerateFileSystemInfos().Count() > 0)
                {
                    deletionQueue.Enqueue(item);
                }
                else
                {
                    item.Delete();
                    log.Debug($"Deleted folder {item.FullName} removed from replica");
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
