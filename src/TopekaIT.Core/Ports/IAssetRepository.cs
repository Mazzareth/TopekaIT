using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default);
    Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Asset asset, CancellationToken ct = default);
    Task UpdateAsync(Asset asset, CancellationToken ct = default);
    Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default);
    Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default);
}
