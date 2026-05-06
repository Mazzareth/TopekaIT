using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class PingHistoryRepository : IPingHistoryRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public PingHistoryRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task AddAsync(PingSample sample, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.PingSamples.Add(sample);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PingSample>> GetByPrinterAsync(string printerId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.PingSamples
            .AsNoTracking()
            .Where(s => s.PrinterId == printerId && s.Timestamp >= from && s.Timestamp <= to)
            .OrderBy(s => s.Timestamp)
            .ToListAsync(ct);
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.PingSamples
            .Where(s => s.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
