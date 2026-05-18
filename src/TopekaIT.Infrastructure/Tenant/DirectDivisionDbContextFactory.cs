using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Tenant;

public sealed class DirectDivisionDbContextFactory : IDivisionDbContextFactory
{
    private readonly string _connectionString;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public DirectDivisionDbContextFactory(
        string connectionString,
        IDataProtectionProvider dataProtectionProvider)
    {
        _connectionString = connectionString;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer(_connectionString, sql => sql.CommandTimeout(120))
            .Options;

        return Task.FromResult(new TopekaDbContext(options, _dataProtectionProvider));
    }
}
