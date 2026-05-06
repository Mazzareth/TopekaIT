using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IPingHistoryRepository
{
    Task AddAsync(PingSample sample, CancellationToken ct = default);
    Task<IReadOnlyList<PingSample>> GetByPrinterAsync(string printerId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
