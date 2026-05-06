using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddDivisionPrinterPasswordSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrinterPasswordCode",
                table: "Divisions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrinterPasswordZipCode",
                table: "Divisions",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrinterPasswordCode",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "PrinterPasswordZipCode",
                table: "Divisions");
        }
    }
}
