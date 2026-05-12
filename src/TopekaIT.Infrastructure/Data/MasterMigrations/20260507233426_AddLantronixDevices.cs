using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddLantronixDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LantronixDevices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DivisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Hostname = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    PollCommand = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SerialSettings = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastPollAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPollSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastLatencyMs = table.Column<int>(type: "int", nullable: true),
                    LastFailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastFuelVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastTcVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastUllage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastHeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastWater = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LastTemperature = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LantronixDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LantronixDevices_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LantronixPollSamples",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReportName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TankNumber = table.Column<int>(type: "int", nullable: true),
                    Product = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TcVolume = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Ullage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Water = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RawResponse = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LantronixPollSamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LantronixPollSamples_LantronixDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "LantronixDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LantronixDevices_DivisionId",
                table: "LantronixDevices",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_LantronixDevices_Name",
                table: "LantronixDevices",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LantronixPollSamples_DeviceId_Timestamp",
                table: "LantronixPollSamples",
                columns: new[] { "DeviceId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LantronixPollSamples");

            migrationBuilder.DropTable(
                name: "LantronixDevices");
        }
    }
}
