using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for device RMA trips.
/// </summary>
public interface IRmaRecordRepository
{
    Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default);
    Task<RmaRecord?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<RmaRecord>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(RmaRecord record, CancellationToken ct = default);
    Task UpdateAsync(RmaRecord record, CancellationToken ct = default);
    Task RemoveAsync(string id, CancellationToken ct = default);
}
