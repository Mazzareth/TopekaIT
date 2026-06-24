using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for the tenant activity feed.
/// </summary>
public class ActivityRepository : IActivityRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public ActivityRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Activity.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(count).ToListAsync(ct);
    }

    public async Task AddAsync(ActivityEvent ev, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Activity.Add(ev);
        await db.SaveChangesAsync(ct);
    }
}
