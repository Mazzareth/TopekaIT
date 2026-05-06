using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPingSamplesAndPrinterEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveFailures",
                table: "Printers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Contact",
                table: "Printers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "Printers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastLatencyMs",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPingAt",
                table: "Printers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Printers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Printers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialNumber",
                table: "Printers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PingSamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PingSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PingSamples_Printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrinterEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RawMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrinterEvents_Printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PingSamples_PrinterId_Timestamp",
                table: "PingSamples",
                columns: new[] { "PrinterId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PrinterEvents_PrinterId_Timestamp",
                table: "PrinterEvents",
                columns: new[] { "PrinterId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PingSamples");

            migrationBuilder.DropTable(
                name: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "ConsecutiveFailures",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Contact",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "LastLatencyMs",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "LastPingAt",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "SerialNumber",
                table: "Printers");
        }
    }
}
