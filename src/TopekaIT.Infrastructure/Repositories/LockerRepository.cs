using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class LockerRepository : ILockerRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public LockerRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Lockers
            .Include(l => l.Occupants)
            .Include(l => l.Assets)
            .AsNoTracking()
            .OrderBy(l => l.Number)
            .ToListAsync(ct);
    }

    public async Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Lockers
            .Include(l => l.Occupants)
            .Include(l => l.Assets)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    public async Task AddAsync(Locker locker, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Lockers.Add(locker);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Locker locker, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Lockers.Update(locker);
        await db.SaveChangesAsync(ct);
    }
}
