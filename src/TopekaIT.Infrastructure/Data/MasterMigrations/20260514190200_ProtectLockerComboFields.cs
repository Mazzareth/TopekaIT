using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TopekaIT.Infrastructure.Data;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations;

[DbContext(typeof(MasterDbContext))]
[Migration("20260514190200_ProtectLockerComboFields")]
public partial class ProtectLockerComboFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "LockerCombo",
            table: "Users",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(32)",
            oldMaxLength: 32,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "LockerCombo",
            table: "Users",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(512)",
            oldMaxLength: 512,
            oldNullable: true);
    }
}
