using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IPrinterRepository
{
    Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default);
    Task<Printer?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Printer printer, CancellationToken ct = default);
    Task UpdateAsync(Printer printer, CancellationToken ct = default);
    Task RemoveAsync(string id, CancellationToken ct = default);
}
