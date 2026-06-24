using Microsoft.AspNetCore.DataProtection;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Shared ephemeral data protection for tests that need encrypted fields but not real key storage.
/// </summary>
internal static class TestDataProtection
{
    public static readonly IDataProtectionProvider Provider = new EphemeralDataProtectionProvider();
}
