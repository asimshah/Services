using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Fastnet.Services.Web.Migrations
{
    public partial class AddSourceSpecificBackupConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackupDriveLabel",
                schema: "srv",
                table: "SourceFolders",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackupFolder",
                schema: "srv",
                table: "SourceFolders",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackupDriveLabel",
                schema: "srv",
                table: "SourceFolders");

            migrationBuilder.DropColumn(
                name: "BackupFolder",
                schema: "srv",
                table: "SourceFolders");
        }
    }
}
