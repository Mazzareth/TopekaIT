using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TopekaIT.Infrastructure.Data.MasterMigrations
{
    /// <inheritdoc />
    public partial class AddScopedAccessCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [Role] = N'Supervisor' WHERE [Role] = N'Manager'");

            migrationBuilder.CreateTable(
                name: "UserPermissionOverrides",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedById = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissionOverrides", x => new { x.UserId, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_Users_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPermissionOverrides_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissionOverrides_UpdatedById",
                table: "UserPermissionOverrides",
                column: "UpdatedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPermissionOverrides");

            migrationBuilder.Sql("UPDATE [Users] SET [Role] = N'Manager' WHERE [Role] = N'Supervisor'");
        }
    }
}
