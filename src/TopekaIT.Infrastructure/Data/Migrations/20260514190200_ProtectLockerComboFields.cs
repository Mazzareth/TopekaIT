using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TopekaIT.Infrastructure.Data;

#nullable disable

namespace TopekaIT.Infrastructure.Data.Migrations;

[DbContext(typeof(TopekaDbContext))]
[Migration("20260514190200_ProtectLockerComboFields")]
public partial class ProtectLockerComboFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "LockCombo",
            table: "Lockers",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(64)",
            oldMaxLength: 64,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "LockCombo",
            table: "Lockers",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(512)",
            oldMaxLength: 512,
            oldNullable: true);
    }
}
