using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IActivityRepository
{
    Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default);
    Task AddAsync(ActivityEvent ev, CancellationToken ct = default);
}
