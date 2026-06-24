using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for RMA records attached to assets.
/// </summary>
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

    public async Task<RmaRecord?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.RmaRecords
            .Include(r => r.Asset)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<IReadOnlyList<RmaRecord>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.RmaRecords
            .Include(r => r.Asset)
            .OrderByDescending(r => r.DateSubmitted)
            .ToListAsync(ct);
    }

    public async Task AddAsync(RmaRecord record, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.RmaRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RmaRecord record, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Entry(record).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var r = await db.RmaRecords.FindAsync(new object[] { id }, ct);
        if (r != null)
        {
            db.RmaRecords.Remove(r);
            await db.SaveChangesAsync(ct);
        }
    }
}
