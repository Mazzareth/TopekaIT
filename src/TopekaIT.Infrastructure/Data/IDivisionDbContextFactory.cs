namespace TopekaIT.Infrastructure.Data;

public interface IDivisionDbContextFactory
{
    Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default);
}
