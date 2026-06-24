using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for printer model names the setup workflow can choose from.
/// </summary>
public interface IPrinterModelRepository
{
    Task<IReadOnlyList<PrinterModel>> GetAllAsync(CancellationToken ct = default);
    Task<PrinterModel> AddAsync(string name, CancellationToken ct = default);
    Task EnsureDefaultAsync(CancellationToken ct = default);
}
