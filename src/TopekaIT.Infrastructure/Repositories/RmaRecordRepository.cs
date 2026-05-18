using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class RmaRecordRepository : IRmaRecordRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public RmaRecordRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.RmaRecords
            .Include(r => r.Asset)
            .AsNoTracking()
            .Where(r => !r.IsReceived && r.ReceivedDate == null)
            .OrderBy(r => r.DateSubmitted ?? r.ItHandOffDate ?? DateTimeOffset.MaxValue)
            .ThenBy(r => r.AssetTag)
            .ToListAsync(ct);
    }
}
