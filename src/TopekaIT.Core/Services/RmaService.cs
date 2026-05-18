using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class RmaService
{
    private readonly IRmaRecordRepository _repo;

    public RmaService(IRmaRecordRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default) =>
        _repo.GetActiveAsync(ct);

    public async Task<IReadOnlyList<RmaRecord>> GetLongestOpenAsync(int count = 20, CancellationToken ct = default)
    {
        var active = await _repo.GetActiveAsync(ct);
        return active
            .OrderBy(r => r.DateSubmitted ?? r.ItHandOffDate ?? DateTimeOffset.MaxValue)
            .ThenBy(r => r.AssetTag)
            .Take(Math.Max(0, count))
            .ToList();
    }
}
