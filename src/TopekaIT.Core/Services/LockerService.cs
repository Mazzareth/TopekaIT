using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class LockerService
{
    private readonly ILockerRepository _repo;
    private readonly ActivityService _activity;

    public LockerService(ILockerRepository repo, ActivityService activity)
    {
        _repo = repo;
        _activity = activity;
    }

    public Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task AddAsync(Locker locker, CancellationToken ct = default)
    {
        locker.Id = Guid.NewGuid().ToString("N")[..16];
        locker.Number = locker.Number.Trim();
        locker.IsActive = true;
        await _repo.AddAsync(locker, ct);
        await _activity.PushAsync("locker_created", $"Locker {locker.Number} created", ct);
    }

    public async Task UpdateAsync(Locker locker, CancellationToken ct = default)
    {
        locker.Number = locker.Number.Trim();
        await _repo.UpdateAsync(locker, ct);
    }

    public async Task AssignOccupantAsync(string lockerId, string userId, bool isPrimary, string actorId, CancellationToken ct = default)
    {
        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return;

        // Soft-close any active assignment for this user on this locker
        var existing = locker.Occupants
            .FirstOrDefault(o => o.UserId == userId && o.UnassignedAt == null);
        if (existing != null)
        {
            existing.UnassignedAt = DateTimeOffset.UtcNow;
            existing.UnassignedBy = actorId;
        }

        locker.Occupants.Add(new LockerOccupant
        {
            LockerId   = lockerId,
            UserId     = userId,
            IsPrimary  = isPrimary,
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = actorId,
        });

        await _repo.UpdateAsync(locker, ct);
        await _activity.PushAsync("locker_assign", $"User {userId} assigned to locker {locker.Number} by {actorId}", ct);
    }

    public async Task UnassignOccupantAsync(string lockerId, string userId, string actorId, CancellationToken ct = default)
    {
        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return;

        var occupant = locker.Occupants
            .FirstOrDefault(o => o.UserId == userId && o.UnassignedAt == null);
        if (occupant == null) return;

        occupant.UnassignedAt = DateTimeOffset.UtcNow;
        occupant.UnassignedBy = actorId;

        await _repo.UpdateAsync(locker, ct);
        await _activity.PushAsync("locker_unassign", $"User {userId} removed from locker {locker.Number} by {actorId}", ct);
    }

    public async Task RecordAuditAsync(string lockerId, string auditorId, CancellationToken ct = default)
    {
        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return;

        locker.LastAuditedAt = DateTimeOffset.UtcNow;
        locker.LastAuditedBy = auditorId;

        await _repo.UpdateAsync(locker, ct);
    }

    /// <summary>Returns lockers whose audit cadence has lapsed and are overdue for inspection.</summary>
    public async Task<IEnumerable<Locker>> GetAuditOverdueAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return all.Where(l =>
            l.IsActive &&
            l.AuditCadenceDays.HasValue &&
            (l.LastAuditedAt == null || (now - l.LastAuditedAt.Value).TotalDays >= l.AuditCadenceDays.Value));
    }
}
