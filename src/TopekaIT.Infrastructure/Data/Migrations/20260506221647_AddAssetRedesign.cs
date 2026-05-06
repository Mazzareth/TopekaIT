using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Flags",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HealthScore",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSeenLocation",
                table: "Assets",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockerId",
                table: "Assets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PairedAssetId",
                table: "Assets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannerKind",
                table: "Assets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ConductedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalScanned = table.Column<int>(type: "int", nullable: false),
                    Discrepancies = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BatteryContainers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CurrentCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatteryContainers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IssueTagDefinitions",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ApplicableCategories = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueTagDefinitions", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Lockers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LockCombo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LockSerial = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    AuditCadenceDays = table.Column<int>(type: "int", nullable: true),
                    LastAuditedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAuditedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lockers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusFlagHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FlagChanged = table.Column<int>(type: "int", nullable: false),
                    WasSet = table.Column<bool>(type: "bit", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusFlagHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StatusFlagHistory_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    LockerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDiscrepancy = table.Column<bool>(type: "bit", nullable: false),
                    DiscrepancyNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntries_AuditSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AuditSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetIssueTags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DefinitionCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TaggedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaggedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetIssueTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetIssueTags_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetIssueTags_IssueTagDefinitions_DefinitionCode",
                        column: x => x.DefinitionCode,
                        principalTable: "IssueTagDefinitions",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LockerOccupants",
                columns: table => new
                {
                    LockerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    UnassignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnassignedBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockerOccupants", x => new { x.LockerId, x.UserId, x.AssignedAt });
                    table.ForeignKey(
                        name: "FK_LockerOccupants_Lockers_LockerId",
                        column: x => x.LockerId,
                        principalTable: "Lockers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_HolderId",
                table: "Assets",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_LockerId",
                table: "Assets",
                column: "LockerId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_PairedAssetId",
                table: "Assets",
                column: "PairedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Serial",
                table: "Assets",
                column: "Serial");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Tag",
                table: "Assets",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_AssetIssueTags_AssetId_ResolvedAt",
                table: "AssetIssueTags",
                columns: new[] { "AssetId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetIssueTags_DefinitionCode",
                table: "AssetIssueTags",
                column: "DefinitionCode");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_SessionId",
                table: "AuditEntries",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LockerOccupants_UserId_UnassignedAt",
                table: "LockerOccupants",
                columns: new[] { "UserId", "UnassignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerId",
                table: "SavedViews",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusFlagHistory_AssetId_ChangedAt",
                table: "StatusFlagHistory",
                columns: new[] { "AssetId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Assets_PairedAssetId",
                table: "Assets",
                column: "PairedAssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Lockers_LockerId",
                table: "Assets",
                column: "LockerId",
                principalTable: "Lockers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Assets_PairedAssetId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Lockers_LockerId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "AssetIssueTags");

            migrationBuilder.DropTable(
                name: "AuditEntries");

            migrationBuilder.DropTable(
                name: "BatteryContainers");

            migrationBuilder.DropTable(
                name: "LockerOccupants");

            migrationBuilder.DropTable(
                name: "SavedViews");

            migrationBuilder.DropTable(
                name: "StatusFlagHistory");

            migrationBuilder.DropTable(
                name: "IssueTagDefinitions");

            migrationBuilder.DropTable(
                name: "AuditSessions");

            migrationBuilder.DropTable(
                name: "Lockers");

            migrationBuilder.DropIndex(
                name: "IX_Assets_HolderId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_LockerId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_PairedAssetId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_Serial",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_Tag",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "HealthScore",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LastSeenLocation",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "LockerId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PairedAssetId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ScannerKind",
                table: "Assets");
        }
    }
}
