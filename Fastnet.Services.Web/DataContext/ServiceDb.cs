using Fastnet.Core;
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
    public class ServiceDbOptions : WebDbOptions
    {
        public ServiceDbOptions()
        {
            this.DisableLazyLoading = true;
        }
    }
    public static class ServiceDbInitialiser
    {
        public static void Initialise(ServiceDb db)
        {
            var logger = db.Database.GetService<ILogger<ServiceDb>>() as ILogger;
            var creator = db.Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            var dbExists = creator.Exists();

            if (dbExists)
            {
                logger.Information("ServiceDb exists");
            }
            else
            {
                logger.Warning("No ServiceDb found");
            }
            db.Database.Migrate();
            logger.Information("The following migrations have been applied:");
            var migrations = db.Database.GetAppliedMigrations();
            foreach (var migration in migrations)
            {
                logger.Information($"\t{migration}");
            }
            db.Seed();
        }
    }
    public class ServiceDbFactory : DesignTimeWebDbContextFactory<ServiceDb>// IDesignTimeDbContextFactory<ServiceDb>
    {
        public ServiceDbFactory()
        {
        }

        //public ServiceDb CreateDbContext(string[] args)
        //{

        //    var optionsBuilder = new DbContextOptionsBuilder<ServiceDb>();
        //    optionsBuilder.UseSqlServer(GetDesignTimeConnectionString());
        //    return new ServiceDb(optionsBuilder.Options, null, null);
        //}


        protected override string GetDesignTimeConnectionString()
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

    public class ServiceDbContextFactory : WebDbContextFactory
    {
        public ServiceDbContextFactory(IOptions<ServiceDbOptions> options, IServiceProvider sp) : base(options, sp)
        {
        }
    }
    public class ServiceDb : WebDbContext
    {
        public DbSet<SourceFolder> SourceFolders { get; set; }
        public DbSet<Backup> Backups { get; set; }
        public ServiceDb(DbContextOptions<ServiceDb> contextOptions, IOptions<ServiceDbOptions> dbOptions, IServiceProvider sp) : base(contextOptions, dbOptions, sp)
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
            //Debug.WriteLine("Seeding started");
        }
    }
}
