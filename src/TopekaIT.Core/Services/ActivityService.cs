using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Writes short feed items for humans to see what just happened. This is not the hard audit log.
/// </summary>
public class ActivityService
{
    private readonly IActivityRepository _repo;

    public ActivityService(IActivityRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
        => _repo.GetRecentAsync(count, ct);

    public Task PushAsync(string kind, string text, CancellationToken ct = default)
    {
        var ev = new ActivityEvent
        {
            Id = $"ev-{Guid.NewGuid():N}".Substring(0, 12),
            Timestamp = DateTimeOffset.UtcNow,
            Kind = kind,
            Text = text,
        };
        return _repo.AddAsync(ev, ct);
    }
}
