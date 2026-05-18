using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations
{
    /// <inheritdoc />
    [DbContext(typeof(MasterDbContext))]
    [Migration("20260514190000_AddPasswordSecurityFields")]
    public partial class AddPasswordSecurityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PasswordIterations",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 100000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordIterations",
                table: "Users");
        }
    }
}
