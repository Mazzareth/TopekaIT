namespace TopekaIT.Infrastructure.Data;

/// <summary>
/// Creates the tenant database context for the current division, or for an explicitly chosen division in background work.
/// </summary>
public interface IDivisionDbContextFactory
{
    Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default);
}
