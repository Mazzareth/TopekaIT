using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrinterAlertState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertCategory",
                table: "PrinterEvents",
                type: "nvarchar(96)",
                maxLength: 96,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertDetail",
                table: "PrinterEvents",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertKey",
                table: "PrinterEvents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlertTitle",
                table: "PrinterEvents",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlertTrainingLevel",
                table: "PrinterEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FriendlyMessage",
                table: "PrinterEvents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrinterAlertStates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrinterId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AlertKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AlertTitle = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AlertCategory = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false),
                    AlertDetail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FriendlyMessage = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TrainingLevel = table.Column<int>(type: "int", nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastEventId = table.Column<long>(type: "bigint", nullable: false),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    BlipSuppressed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterAlertStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrinterAlertStates_Printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrinterEvents_AlertKey_Timestamp",
                table: "PrinterEvents",
                columns: new[] { "AlertKey", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PrinterAlertStates_BlipSuppressed_LastSeenAt",
                table: "PrinterAlertStates",
                columns: new[] { "BlipSuppressed", "LastSeenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrinterAlertStates_PrinterId_AlertKey",
                table: "PrinterAlertStates",
                columns: new[] { "PrinterId", "AlertKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrinterAlertStates");

            migrationBuilder.DropIndex(
                name: "IX_PrinterEvents_AlertKey_Timestamp",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "AlertCategory",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "AlertDetail",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "AlertKey",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "AlertTitle",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "AlertTrainingLevel",
                table: "PrinterEvents");

            migrationBuilder.DropColumn(
                name: "FriendlyMessage",
                table: "PrinterEvents");
        }
    }
}
