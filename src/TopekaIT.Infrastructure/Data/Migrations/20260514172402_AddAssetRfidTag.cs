using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRfidTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
