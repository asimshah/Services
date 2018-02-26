using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace Fastnet.Services.Web.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.EnsureSchema(
            //    name: "srv");

            //migrationBuilder.CreateTable(
            //    name: "SourceFolders",
            //    schema: "srv",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        BackupEnabled = table.Column<bool>(nullable: false),
            //        ConfiguredOn = table.Column<DateTimeOffset>(nullable: false),
            //        DisplayName = table.Column<string>(nullable: true),
            //        FullPath = table.Column<string>(nullable: true),
            //        ScheduledTime = table.Column<int>(nullable: false),
            //        TimeStamp = table.Column<byte[]>(rowVersion: true, nullable: true),
            //        Type = table.Column<int>(nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_SourceFolders", x => x.Id);
            //    });

            //migrationBuilder.CreateTable(
            //    name: "Backups",
            //    schema: "srv",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(nullable: false)
            //            .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
            //        BackedUpOn = table.Column<DateTimeOffset>(nullable: false),
            //        FullPath = table.Column<string>(nullable: true),
            //        ScheduledOn = table.Column<DateTimeOffset>(nullable: false),
            //        SourceFolderId = table.Column<int>(nullable: false),
            //        State = table.Column<int>(nullable: false),
            //        TimeStamp = table.Column<byte[]>(rowVersion: true, nullable: true)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Backups", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Backups_SourceFolders_SourceFolderId",
            //            column: x => x.SourceFolderId,
            //            principalSchema: "srv",
            //            principalTable: "SourceFolders",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Cascade);
            //    });

            //migrationBuilder.CreateIndex(
            //    name: "IX_Backups_SourceFolderId",
            //    schema: "srv",
            //    table: "Backups",
            //    column: "SourceFolderId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_SourceFolders_DisplayName",
            //    schema: "srv",
            //    table: "SourceFolders",
            //    column: "DisplayName",
            //    unique: true,
            //    filter: "[DisplayName] IS NOT NULL");

            //migrationBuilder.CreateIndex(
            //    name: "IX_SourceFolders_FullPath",
            //    schema: "srv",
            //    table: "SourceFolders",
            //    column: "FullPath",
            //    unique: true,
            //    filter: "[FullPath] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Backups",
                schema: "srv");

            migrationBuilder.DropTable(
                name: "SourceFolders",
                schema: "srv");
        }
    }
}
