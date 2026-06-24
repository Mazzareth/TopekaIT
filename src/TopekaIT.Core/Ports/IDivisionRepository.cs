using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for divisions, including the connection strings that point at tenant databases.
/// </summary>
public interface IDivisionRepository
{
    Task<IReadOnlyList<Division>> GetAllAsync(CancellationToken ct = default);
    Task<Division?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Division division, CancellationToken ct = default);
    Task UpdateAsync(Division division, CancellationToken ct = default);
    Task RemoveAsync(string id, CancellationToken ct = default);
}
