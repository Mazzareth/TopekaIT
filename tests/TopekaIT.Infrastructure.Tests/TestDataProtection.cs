using Microsoft.AspNetCore.DataProtection;

namespace TopekaIT.Infrastructure.Tests;

internal static class TestDataProtection
{
    public static readonly IDataProtectionProvider Provider = new EphemeralDataProtectionProvider();
}
