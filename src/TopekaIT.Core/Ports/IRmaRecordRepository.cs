using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IRmaRecordRepository
{
    Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default);
}
