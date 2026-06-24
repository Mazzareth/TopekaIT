using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for lockers. Assignment keeps one active locker per user while preserving the old occupant rows as history.
/// </summary>
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

    public async Task<(Locker? Locker, bool AddedOccupant)> AssignOccupantAsync(
        string lockerId,
        string userId,
        bool isPrimary,
        string actorId,
        DateTimeOffset assignedAt,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var locker = await db.Lockers.FirstOrDefaultAsync(l => l.Id == lockerId, ct);
        if (locker == null) return (null, false);

        var activeAssignments = await db.LockerOccupants
            .Where(o => o.UserId == userId && o.UnassignedAt == null)
            .ToListAsync(ct);

        var targetAssignment = activeAssignments.FirstOrDefault(o => o.LockerId == lockerId);
        foreach (var otherAssignment in activeAssignments.Where(o => o.LockerId != lockerId))
        {
            otherAssignment.UnassignedAt = assignedAt;
            otherAssignment.UnassignedBy = actorId;
        }

        var addedOccupant = false;
        if (targetAssignment == null)
        {
            db.LockerOccupants.Add(new LockerOccupant
            {
                LockerId = lockerId,
                UserId = userId,
                IsPrimary = isPrimary,
                AssignedAt = assignedAt,
                AssignedBy = actorId,
            });
            addedOccupant = true;
        }
        else
        {
            targetAssignment.IsPrimary = isPrimary;
        }

        await db.SaveChangesAsync(ct);
        return (locker, addedOccupant);
    }

    public async Task<Locker?> UnassignOccupantAsync(
        string lockerId,
        string userId,
        string actorId,
        DateTimeOffset unassignedAt,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var locker = await db.Lockers.FirstOrDefaultAsync(l => l.Id == lockerId, ct);
        if (locker == null) return null;

        var occupant = await db.LockerOccupants
            .FirstOrDefaultAsync(o => o.LockerId == lockerId && o.UserId == userId && o.UnassignedAt == null, ct);
        if (occupant == null) return null;

        occupant.UnassignedAt = unassignedAt;
        occupant.UnassignedBy = actorId;

        await db.SaveChangesAsync(ct);
        return locker;
    }
}
