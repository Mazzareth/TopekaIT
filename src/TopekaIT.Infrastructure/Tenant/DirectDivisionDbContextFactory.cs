using Microsoft.EntityFrameworkCore;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Tenant;

public sealed class DirectDivisionDbContextFactory : IDivisionDbContextFactory
{
    private readonly string _connectionString;

    public DirectDivisionDbContextFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseSqlServer(_connectionString, sql => sql.CommandTimeout(120))
            .Options;

        return Task.FromResult(new TopekaDbContext(options));
    }
}
