using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for the lightweight activity feed.
/// </summary>
public interface IActivityRepository
{
    Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default);
    Task AddAsync(ActivityEvent ev, CancellationToken ct = default);
}
