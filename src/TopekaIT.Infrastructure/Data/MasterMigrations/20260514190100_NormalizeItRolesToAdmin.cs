using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TopekaIT.Infrastructure.Data;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations;

[DbContext(typeof(MasterDbContext))]
[Migration("20260514190100_NormalizeItRolesToAdmin")]
public partial class NormalizeItRolesToAdmin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE [Users] SET [Role] = N'Admin' WHERE [Role] = N'IT'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
