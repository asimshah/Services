using Fastnet.Core.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Services.Data
{
    public class ServiceDb : WebDbContext
    {
        public DbSet<SourceFolder> SourceFolders { get; set; }
        public DbSet<Backup> Backups { get; set; }
        public ServiceDb(DbContextOptions<ServiceDb> contextOptions, IServiceProvider sp) : base(contextOptions, sp)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("srv");
            modelBuilder.Entity<SourceFolder>()
                .HasIndex(x => x.DisplayName)
                .IsUnique();

            modelBuilder.Entity<SourceFolder>()
                .HasIndex(x => x.FullPath)
                .IsUnique();

            modelBuilder.Entity<Backup>()
                .HasOne(x => x.SourceFolder)
                .WithMany(x => x.Backups)
                .HasForeignKey(x => x.SourceFolderId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
