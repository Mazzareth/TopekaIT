using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TopekaIT.Infrastructure.Data;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations;

[DbContext(typeof(MasterDbContext))]
[Migration("20260529165000_AddUserStationPins")]
public partial class AddUserStationPins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StationPinHash",
            table: "Users",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "StationPinIterations",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "StationPinSalt",
            table: "Users",
            type: "nvarchar(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_DivisionId",
            table: "Users",
            column: "DivisionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_DivisionId",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "StationPinHash",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "StationPinIterations",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "StationPinSalt",
            table: "Users");
    }
}
