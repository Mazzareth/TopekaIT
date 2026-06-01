using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TopekaIT.Infrastructure.Data;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations;

[DbContext(typeof(TopekaDbContext))]
[Migration("20260529165500_AddEquipmentStationLedger")]
public partial class AddEquipmentStationLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActualHolderId",
            table: "AuditEntries",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ActualLockerId",
            table: "AuditEntries",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DiscrepancyReason",
            table: "AuditEntries",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExpectedHolderId",
            table: "AuditEntries",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExpectedLockerId",
            table: "AuditEntries",
            type: "nvarchar(16)",
            maxLength: 16,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Result",
            table: "AuditEntries",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Expected");

        migrationBuilder.AddColumn<string>(
            name: "ScanValue",
            table: "AuditEntries",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DivisionId",
            table: "AuditSessions",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "MissingCount",
            table: "AuditSessions",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "UnexpectedCount",
            table: "AuditSessions",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "EquipmentTransactions",
            columns: table => new
            {
                Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                DivisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                LinkedAssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                EmployeeId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                CurrentHolderId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                ActorId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                TicketId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                TicketLink = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                RmaRecordId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                RmaLink = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                ScanSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                BeforeStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                AfterStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                BeforeHolderId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                AfterHolderId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                BeforeLockerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                AfterLockerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                BeforeFlags = table.Column<int>(type: "int", nullable: false),
                AfterFlags = table.Column<int>(type: "int", nullable: false),
                BeforeState = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                AfterState = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EquipmentTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_EquipmentTransactions_Assets_AssetId",
                    column: x => x.AssetId,
                    principalTable: "Assets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentTransactions_AssetId",
            table: "EquipmentTransactions",
            column: "AssetId");

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentTransactions_DivisionId",
            table: "EquipmentTransactions",
            column: "DivisionId");

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentTransactions_EmployeeId",
            table: "EquipmentTransactions",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_EquipmentTransactions_Timestamp",
            table: "EquipmentTransactions",
            column: "Timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EquipmentTransactions");

        migrationBuilder.DropColumn(name: "ActualHolderId", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "ActualLockerId", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "DiscrepancyReason", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "ExpectedHolderId", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "ExpectedLockerId", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "Result", table: "AuditEntries");
        migrationBuilder.DropColumn(name: "ScanValue", table: "AuditEntries");

        migrationBuilder.DropColumn(name: "DivisionId", table: "AuditSessions");
        migrationBuilder.DropColumn(name: "MissingCount", table: "AuditSessions");
        migrationBuilder.DropColumn(name: "UnexpectedCount", table: "AuditSessions");
    }
}
