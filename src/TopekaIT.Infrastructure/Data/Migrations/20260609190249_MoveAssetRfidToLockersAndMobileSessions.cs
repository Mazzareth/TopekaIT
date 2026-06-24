using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveAssetRfidToLockersAndMobileSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Assets_RfidTagId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RfidLinkedAt",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "RfidTagId",
                table: "Assets");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RfidLinkedAt",
                table: "Lockers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidTagId",
                table: "Lockers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNameSnapshot",
                table: "EquipmentTransactions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockerNumberSnapshot",
                table: "EquipmentTransactions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileSessionId",
                table: "EquipmentTransactions",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReaderDeviceSerial",
                table: "EquipmentTransactions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScannedLockerId",
                table: "EquipmentTransactions",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MobileEquipmentSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DivisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReaderDeviceSerial = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobileEquipmentSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lockers_RfidTagId",
                table: "Lockers",
                column: "RfidTagId",
                unique: true,
                filter: "[RfidTagId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MobileEquipmentSessions_ExpiresAt",
                table: "MobileEquipmentSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MobileEquipmentSessions_ReaderDeviceSerial",
                table: "MobileEquipmentSessions",
                column: "ReaderDeviceSerial");

            migrationBuilder.CreateIndex(
                name: "IX_MobileEquipmentSessions_TokenHash",
                table: "MobileEquipmentSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MobileEquipmentSessions_UserId",
                table: "MobileEquipmentSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MobileEquipmentSessions");

            migrationBuilder.DropIndex(
                name: "IX_Lockers_RfidTagId",
                table: "Lockers");

            migrationBuilder.DropColumn(
                name: "RfidLinkedAt",
                table: "Lockers");

            migrationBuilder.DropColumn(
                name: "RfidTagId",
                table: "Lockers");

            migrationBuilder.DropColumn(
                name: "EmployeeNameSnapshot",
                table: "EquipmentTransactions");

            migrationBuilder.DropColumn(
                name: "LockerNumberSnapshot",
                table: "EquipmentTransactions");

            migrationBuilder.DropColumn(
                name: "MobileSessionId",
                table: "EquipmentTransactions");

            migrationBuilder.DropColumn(
                name: "ReaderDeviceSerial",
                table: "EquipmentTransactions");

            migrationBuilder.DropColumn(
                name: "ScannedLockerId",
                table: "EquipmentTransactions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RfidLinkedAt",
                table: "Assets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RfidTagId",
                table: "Assets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_RfidTagId",
                table: "Assets",
                column: "RfidTagId",
                unique: true,
                filter: "[RfidTagId] IS NOT NULL");
        }
    }
}
