﻿// <auto-generated />
using Fastnet.Services.Data;
using Fastnet.Services.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Fastnet.Services.Web.Migrations
{
    [DbContext(typeof(ServiceDb))]
    [Migration("20180226133156_AddSourceSpecificBackupConfiguration")]
    partial class AddSourceSpecificBackupConfiguration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("srv")
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Fastnet.Services.Data.Backup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset>("BackedUpOn");

                    b.Property<string>("FullPath");

                    b.Property<DateTimeOffset>("ScheduledOn");

                    b.Property<int>("SourceFolderId");

                    b.Property<int>("State");

                    b.Property<byte[]>("TimeStamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.HasKey("Id");

                    b.HasIndex("SourceFolderId");

                    b.ToTable("Backups");
                });

            modelBuilder.Entity("Fastnet.Services.Data.SourceFolder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BackupDriveLabel");

                    b.Property<bool>("BackupEnabled");

                    b.Property<string>("BackupFolder");

                    b.Property<DateTimeOffset>("ConfiguredOn");

                    b.Property<string>("DisplayName");

                    b.Property<string>("FullPath");

                    b.Property<int>("ScheduledTime");

                    b.Property<byte[]>("TimeStamp")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<int>("Type");

                    b.HasKey("Id");

                    b.HasIndex("DisplayName")
                        .IsUnique()
                        .HasFilter("[DisplayName] IS NOT NULL");

                    b.HasIndex("FullPath")
                        .IsUnique()
                        .HasFilter("[FullPath] IS NOT NULL");

                    b.ToTable("SourceFolders");
                });

            modelBuilder.Entity("Fastnet.Services.Data.Backup", b =>
                {
                    b.HasOne("Fastnet.Services.Data.SourceFolder", "SourceFolder")
                        .WithMany("Backups")
                        .HasForeignKey("SourceFolderId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
