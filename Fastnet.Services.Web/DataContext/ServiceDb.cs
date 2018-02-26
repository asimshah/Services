using Fastnet.Core.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fastnet.Services.Data
{
    public static class ServiceDbInitialiser
    {
        public static void Initialise(ServiceDb db)
        {
            var logger = db.Database.GetService<ILogger<ServiceDb>>() as ILogger;
            var creator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            var dbExists = creator.Exists();

            if(dbExists)
            {
                logger.LogInformation("ServiceDb exists");
            }
            else
            {
                logger.LogWarning("No ServiceDb found - migrate() is probably not going to work");
            }
            db.Database.Migrate();
            logger.LogInformation("The following migrations have been applied:");
            var migrations = db.Database.GetAppliedMigrations();
            foreach(var migration in migrations)
            {
                logger.LogInformation($"\t{migration}");
            }
            db.Seed();
        }
    }
    public class ServiceDbFactory : IDesignTimeDbContextFactory<ServiceDb>
    {
        public ServiceDbFactory()
        {
        }

        public ServiceDb CreateDbContext(string[] args)
        {
           
            var optionsBuilder = new DbContextOptionsBuilder<ServiceDb>();
            optionsBuilder.UseSqlServer(GetDesignTimeConnectionString());
            return new ServiceDb(optionsBuilder.Options, null);
        }
        private string GetDesignTimeConnectionString()
        {
            var path = @"C:\devroot\Standard2\Services\Fastnet.Services.Web";
            var databaseFilename = @"ServiceDb.mdf";
            var catalog = @"ServiceDb";
            //string path = environment.ContentRootPath;
            string dataFolder = Path.Combine(path, "Data");
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }
            string databaseFile = Path.Combine(dataFolder, databaseFilename);
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
            csb.AttachDBFilename = databaseFile;
            csb.DataSource = ".\\SQLEXPRESS";
            csb.InitialCatalog = catalog;
            csb.IntegratedSecurity = true;
            csb.MultipleActiveResultSets = true;
            return csb.ToString();
        }
    }
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
        internal void Seed()
        {
            Debug.WriteLine("Seeding started");
        }
    }
}
