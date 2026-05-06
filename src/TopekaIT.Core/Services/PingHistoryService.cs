using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class PingHistoryService
{
    private readonly IPingHistoryRepository _repo;

    public PingHistoryService(IPingHistoryRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<PingSample>> GetSamplesAsync(string printerId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        => _repo.GetByPrinterAsync(printerId, from, to, ct);

    /// <summary>
    /// Returns uptime windows for a printer over a given duration.
    /// Each tuple represents a contiguous period of up or down state.
    /// </summary>
    public async Task<List<UptimeWindow>> GetUptimeWindowAsync(string printerId, TimeSpan duration, CancellationToken ct = default)
    {
        var to = DateTimeOffset.UtcNow;
        var from = to - duration;
        var samples = await _repo.GetByPrinterAsync(printerId, from, to, ct);

        if (samples.Count == 0)
            return new List<UptimeWindow>();

        var windows = new List<UptimeWindow>();
        var currentUp = samples[0].Success;
        var windowStart = samples[0].Timestamp;

        for (int i = 1; i < samples.Count; i++)
        {
            if (samples[i].Success != currentUp)
            {
                windows.Add(new UptimeWindow(windowStart, samples[i].Timestamp, currentUp));
                windowStart = samples[i].Timestamp;
                currentUp = samples[i].Success;
            }
        }

        // Close the last window
        windows.Add(new UptimeWindow(windowStart, to, currentUp));

        return windows;
    }

    public Task PurgeOlderThanAsync(TimeSpan maxAge, CancellationToken ct = default)
        => _repo.PurgeOlderThanAsync(DateTimeOffset.UtcNow - maxAge, ct);
}

public record UptimeWindow(DateTimeOffset Start, DateTimeOffset End, bool IsUp);
