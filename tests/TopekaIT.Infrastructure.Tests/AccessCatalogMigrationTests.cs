using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Data.MasterMigrations;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Checks that access-catalog migrations keep the expected user role data intact.
/// </summary>
public class AccessCatalogMigrationTests
{
    [Fact]
    public void MasterContext_ContainsPermissionOverrides()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        using var db = new MasterDbContext(options, TestDataProtection.Provider);

        Assert.NotNull(db.Model.FindEntityType(typeof(UserPermissionOverride)));
    }

    [Fact]
    public void MasterContext_DivisionIncludesEquipmentCheckInCadence()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        using var db = new MasterDbContext(options, TestDataProtection.Provider);
        var division = db.Model.FindEntityType(typeof(Division));

        Assert.NotNull(division?.FindProperty(nameof(Division.EquipmentCheckInIntervalDays)));
    }

    [Fact]
    public void TenantContext_DoesNotContainPermissionOverrides()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        using var db = new TopekaDbContext(options, TestDataProtection.Provider);

        Assert.Null(db.Model.FindEntityType(typeof(UserPermissionOverride)));
    }

    [Fact]
    public void ScopedAccessMigration_CreatesOverrideTableAndMigratesManagerRoles()
    {
        var operations = BuildUpOperations(new AddScopedAccessCatalog());

        Assert.Contains(operations.OfType<SqlOperation>(), op => op.Sql.Contains("SET [Role] = N'Supervisor'", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(operations.OfType<CreateTableOperation>(), op => op.Name == "UserPermissionOverrides");
    }

    [Fact]
    public void NormalizeItRolesMigration_MigratesItRolesToAdmin()
    {
        var operations = BuildUpOperations(new NormalizeItRolesToAdmin());

        Assert.Contains(operations.OfType<SqlOperation>(), op => op.Sql.Contains("SET [Role] = N'Admin'", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(operations.OfType<SqlOperation>(), op => op.Sql.Contains("[Role] = N'IT'", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DivisionCadenceMigration_AddsEquipmentCheckInInterval()
    {
        var operations = BuildUpOperations(new AddDivisionEquipmentCheckInCadence());

        Assert.Contains(operations.OfType<AddColumnOperation>(), op =>
            op.Table == "Divisions" &&
            op.Name == "EquipmentCheckInIntervalDays" &&
            op.DefaultValue is 30);
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
