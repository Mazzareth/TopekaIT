using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRmaAndLoanTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Audit",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LockSerialNumber",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockerCombo",
                table: "Users",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockerNumber",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assets",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<bool>(
                name: "IsSAE",
                table: "Assets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ScannerType",
                table: "Assets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoanRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BorrowerId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    IsDayLoan = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DateLoaned = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DateReturned = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanRecords_Users_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RmaRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AssetId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DateSubmitted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ItHandOffDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TentativeReturnDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsReceived = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Section = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AssetTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsTagged = table.Column<bool>(type: "bit", nullable: false),
                    IsLost = table.Column<bool>(type: "bit", nullable: false),
                    DateTagged = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RmaRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RmaRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanRecords_AssetId",
                table: "LoanRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanRecords_BorrowerId",
                table: "LoanRecords",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_RmaRecords_AssetId",
                table: "RmaRecords",
                column: "AssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanRecords");

            migrationBuilder.DropTable(
                name: "RmaRecords");

            migrationBuilder.DropColumn(
                name: "Audit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockSerialNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockerCombo",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockerNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSAE",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ScannerType",
                table: "Assets");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Assets",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);
        }
    }
}
