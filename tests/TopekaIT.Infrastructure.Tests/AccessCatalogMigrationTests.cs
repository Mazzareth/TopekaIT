using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Data.MasterMigrations;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class AccessCatalogMigrationTests
{
    [Fact]
    public void MasterContext_ContainsPermissionOverrides()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        using var db = new MasterDbContext(options);

        Assert.NotNull(db.Model.FindEntityType(typeof(UserPermissionOverride)));
    }

    [Fact]
    public void TenantContext_DoesNotContainPermissionOverrides()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        using var db = new TopekaDbContext(options);

        Assert.Null(db.Model.FindEntityType(typeof(UserPermissionOverride)));
    }

    [Fact]
    public void ScopedAccessMigration_CreatesOverrideTableAndMigratesManagerRoles()
    {
        var operations = BuildUpOperations(new AddScopedAccessCatalog());

        Assert.Contains(operations.OfType<SqlOperation>(), op => op.Sql.Contains("SET [Role] = N'Supervisor'", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(operations.OfType<CreateTableOperation>(), op => op.Name == "UserPermissionOverrides");
    }

    private static IReadOnlyList<MigrationOperation> BuildUpOperations(Migration migration)
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var up = migration.GetType().GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(up);

        up.Invoke(migration, new object[] { builder });
        return builder.Operations;
    }
}
