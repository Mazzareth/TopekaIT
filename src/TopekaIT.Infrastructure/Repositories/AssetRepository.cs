using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public AssetRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Assets
            .Include(a => a.RmaRecords)
            .Include(a => a.LoanRecords)
            .AsNoTracking()
            .OrderBy(a => a.Tag)
            .ToListAsync(ct);
    }

    public async Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Assets
            .Include(a => a.RmaRecords)
            .Include(a => a.LoanRecords)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(Asset asset, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Assets.Add(asset);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Asset asset, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Assets.Update(asset);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Assets
            .Include(a => a.LoanRecords)
            .Where(a => a.Status == TopekaIT.Core.Domain.Enums.AssetStatus.Spare)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.LoanRecords
            .Include(r => r.Asset)
            .Where(r => r.DateReturned == null)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
