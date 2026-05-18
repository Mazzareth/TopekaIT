using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface ILockerRepository
{
    Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default);
    Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Locker locker, CancellationToken ct = default);
    Task UpdateAsync(Locker locker, CancellationToken ct = default);
    Task<(Locker? Locker, bool AddedOccupant)> AssignOccupantAsync(
        string lockerId,
        string userId,
        bool isPrimary,
        string actorId,
        DateTimeOffset assignedAt,
        CancellationToken ct = default);
    Task<Locker?> UnassignOccupantAsync(
        string lockerId,
        string userId,
        string actorId,
        DateTimeOffset unassignedAt,
        CancellationToken ct = default);
}
