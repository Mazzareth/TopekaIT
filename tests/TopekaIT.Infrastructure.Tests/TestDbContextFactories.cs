using Microsoft.EntityFrameworkCore;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Test tenant factory. It gives repositories a real DbContext without going through tenant selection.
/// </summary>
internal sealed class TestDivisionDbContextFactory : IDivisionDbContextFactory
{
    private readonly DbContextOptions<TopekaDbContext> _options;

    public TestDivisionDbContextFactory(DbContextOptions<TopekaDbContext> options)
    {
        _options = options;
    }

    public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
        Task.FromResult(new TopekaDbContext(_options, TestDataProtection.Provider));
}

/// <summary>
/// Test master factory for repositories that use the global database.
/// </summary>
internal sealed class TestMasterDbContextFactory : IDbContextFactory<MasterDbContext>
{
    private readonly DbContextOptions<MasterDbContext> _options;

    public TestMasterDbContextFactory(DbContextOptions<MasterDbContext> options)
    {
        _options = options;
    }

    public MasterDbContext CreateDbContext() => new(_options, TestDataProtection.Provider);

    public Task<MasterDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new MasterDbContext(_options, TestDataProtection.Provider));
}
