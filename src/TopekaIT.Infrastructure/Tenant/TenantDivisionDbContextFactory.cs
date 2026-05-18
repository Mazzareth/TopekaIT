using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Tenant;

public class TenantDivisionDbContextFactory : IDivisionDbContextFactory
{
    private readonly ITenantContext _tenantContext;
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public TenantDivisionDbContextFactory(
        ITenantContext tenantContext,
        IDataProtectionProvider dataProtectionProvider)
    {
        _tenantContext = tenantContext;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_tenantContext.ConnectionString))
        {
            throw new InvalidOperationException("No active division has been selected.");
        }

        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer(_tenantContext.ConnectionString, sql => sql.CommandTimeout(120))
            .Options;

        return Task.FromResult(new TopekaDbContext(options, _dataProtectionProvider));
    }
}
