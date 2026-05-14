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
        if (string.IsNullOrWhiteSpace(locker.Number))
        {
            throw new InvalidOperationException("Locker number is required.");
        }

        var existing = await _repo.GetAllAsync(ct);
        if (existing.Any(l => l.IsActive &&
            string.Equals(l.Number.Trim(), locker.Number.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Locker {locker.Number.Trim()} already exists.");
        }

        locker.Id = Guid.NewGuid().ToString("N")[..16];
        locker.Number = locker.Number.Trim();
        locker.IsActive = true;
        await _repo.AddAsync(locker, ct);
        await _activity.PushAsync("locker_created", $"Locker {locker.Number} created", ct);
    }

    public async Task UpdateAsync(Locker locker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locker.Number))
        {
            throw new InvalidOperationException("Locker number is required.");
        }

        var existing = await _repo.GetAllAsync(ct);
        if (existing.Any(l => l.Id != locker.Id && l.IsActive &&
            string.Equals(l.Number.Trim(), locker.Number.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Locker {locker.Number.Trim()} already exists.");
        }

        locker.Number = locker.Number.Trim();
        await _repo.UpdateAsync(locker, ct);
    }

    public async Task AssignOccupantAsync(string lockerId, string userId, bool isPrimary, string actorId, CancellationToken ct = default)
    {
        var allLockers = await _repo.GetAllAsync(ct);
        foreach (var otherLocker in allLockers.Where(l => l.Id != lockerId))
        {
            var otherAssignment = otherLocker.Occupants
                .FirstOrDefault(o => o.UserId == userId && o.UnassignedAt == null);
            if (otherAssignment == null) continue;

            otherAssignment.UnassignedAt = DateTimeOffset.UtcNow;
            otherAssignment.UnassignedBy = actorId;
            await _repo.UpdateAsync(otherLocker, ct);
        }

        var locker = allLockers.FirstOrDefault(l => l.Id == lockerId)
            ?? await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return;

        var existing = locker.Occupants
            .FirstOrDefault(o => o.UserId == userId && o.UnassignedAt == null);
        if (existing != null)
        {
            existing.IsPrimary = isPrimary;
            await _repo.UpdateAsync(locker, ct);
            return;
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
