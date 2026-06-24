using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Locker combo protection tests: encrypted new values, readable old plaintext, no drama.
/// </summary>
public class ComboProtectionTests
{
    [Fact]
    public void LockerComboConverter_ProtectsValuesAndReadsLegacyPlainText()
    {
        using var db = CreateTenantContext();
        var converter = db.Model
            .FindEntityType(typeof(Locker))!
            .FindProperty(nameof(Locker.LockCombo))!
            .GetValueConverter()!;

        var protectedValue = Assert.IsType<string>(converter.ConvertToProvider("12-34-56"));

        Assert.NotEqual("12-34-56", protectedValue);
        Assert.True(protectedValue.Length <= ComboProtection.ProtectedComboMaxLength);
        Assert.Equal("12-34-56", converter.ConvertFromProvider(protectedValue));
        Assert.Equal("legacy-plain", converter.ConvertFromProvider("legacy-plain"));
    }

    [Fact]
    public void LegacyUserLockerComboConverter_ProtectsValuesAndReadsLegacyPlainText()
    {
        using var db = CreateMasterContext();
        var converter = db.Model
            .FindEntityType(typeof(User))!
            .FindProperty(nameof(User.LockerCombo))!
            .GetValueConverter()!;

        var protectedValue = Assert.IsType<string>(converter.ConvertToProvider("01-02-03"));

        Assert.NotEqual("01-02-03", protectedValue);
        Assert.True(protectedValue.Length <= ComboProtection.ProtectedComboMaxLength);
        Assert.Equal("01-02-03", converter.ConvertFromProvider(protectedValue));
        Assert.Equal("legacy-plain", converter.ConvertFromProvider("legacy-plain"));
    }

    private static TopekaDbContext CreateTenantContext()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        return new TopekaDbContext(options, TestDataProtection.Provider);
    }

    private static MasterDbContext CreateMasterContext()
    {
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ModelOnly;Trusted_Connection=True;")
            .Options;

        return new MasterDbContext(options, TestDataProtection.Provider);
    }
}
