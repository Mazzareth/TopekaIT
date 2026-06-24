using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Infrastructure.Data;

/// <summary>
/// Protects stored locker combinations while still reading old plaintext rows. New writes are locked down; old data does not crash the page.
/// </summary>
public static class ComboProtection
{
    public const int ProtectedComboMaxLength = 512;

    private const string LockerComboPurpose = "TopekaIT.Lockers.LockCombo.v1";
    private const string UserLockerComboPurpose = "TopekaIT.Users.LegacyLockerCombo.v1";

    public static void ApplyLockerProtection(ModelBuilder mb, IDataProtectionProvider dataProtectionProvider)
    {
        var converter = CreateConverter(dataProtectionProvider, LockerComboPurpose);
        mb.Entity<Locker>()
            .Property(x => x.LockCombo)
            .HasConversion(converter)
            .HasMaxLength(ProtectedComboMaxLength);
    }

    public static void ApplyLegacyUserLockerProtection(ModelBuilder mb, IDataProtectionProvider dataProtectionProvider)
    {
        var converter = CreateConverter(dataProtectionProvider, UserLockerComboPurpose);
        mb.Entity<User>()
            .Property(x => x.LockerCombo)
            .HasConversion(converter)
            .HasMaxLength(ProtectedComboMaxLength);
    }

    private static ValueConverter<string?, string?> CreateConverter(
        IDataProtectionProvider dataProtectionProvider,
        string purpose)
    {
        var protector = dataProtectionProvider.CreateProtector(purpose);
        return new ValueConverter<string?, string?>(
            value => Protect(protector, value),
            value => UnprotectLegacySafe(protector, value),
            new ConverterMappingHints(size: ProtectedComboMaxLength));
    }

    private static string? Protect(IDataProtector protector, string? value)
    {
        return string.IsNullOrEmpty(value) ? value : protector.Protect(value);
    }

    private static string? UnprotectLegacySafe(IDataProtector protector, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            return protector.Unprotect(value);
        }
        catch (CryptographicException)
        {
            return value;
        }
    }
}
