using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Manages a device's RMA trip and avoids opening two active RMA records for the same asset.
/// </summary>
public class RmaService
{
    private readonly IRmaRecordRepository _repo;

    public RmaService(IRmaRecordRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default) =>
        _repo.GetActiveAsync(ct);

    public Task<RmaRecord?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _repo.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<RmaRecord>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public async Task<RmaRecord?> GetActiveForAssetAsync(string assetId, CancellationToken ct = default)
    {
        var active = await _repo.GetActiveAsync(ct);
        return active.FirstOrDefault(r => string.Equals(r.AssetId, assetId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<RmaRecord> CreateRmaAsync(string assetId, string assetTag, string comments, string section, DateTimeOffset? tentativeReturnDate = null, CancellationToken ct = default)
    {
        var existing = await GetActiveForAssetAsync(assetId, ct);
        if (existing != null) return existing;

        var record = new RmaRecord
        {
            AssetId = assetId,
            AssetTag = assetTag,
            DateSubmitted = DateTimeOffset.UtcNow,
            Comments = comments,
            Section = section,
            IsReceived = false,
            TentativeReturnDate = tentativeReturnDate
        };
        await _repo.AddAsync(record, ct);
        return record;
    }

    public async Task UpdateRmaAsync(RmaRecord record, CancellationToken ct = default)
    {
        await _repo.UpdateAsync(record, ct);
    }

    public async Task CompleteRmaAsync(string id, string comments, CancellationToken ct = default)
    {
        var record = await _repo.GetByIdAsync(id, ct);
        if (record != null)
        {
            record.IsReceived = true;
            record.ReceivedDate = DateTimeOffset.UtcNow;
            if (!string.IsNullOrWhiteSpace(comments))
            {
                record.Comments = string.IsNullOrEmpty(record.Comments)
                    ? comments
                    : record.Comments + "\n" + comments;
            }
            await _repo.UpdateAsync(record, ct);
        }
    }
}
