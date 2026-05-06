using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class DivisionRepository : IDivisionRepository
{
    private readonly IDbContextFactory<MasterDbContext> _factory;

    public DivisionRepository(IDbContextFactory<MasterDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Division>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Divisions.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);
    }

    public async Task<Division?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Divisions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AddAsync(Division division, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Divisions.Add(division);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Division division, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Divisions.Update(division);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var division = await db.Divisions.FindAsync(new object?[] { id }, ct);
        if (division == null) return;
        db.Divisions.Remove(division);
        await db.SaveChangesAsync(ct);
    }
}
