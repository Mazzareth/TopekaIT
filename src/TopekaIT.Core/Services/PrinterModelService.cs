using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class PrinterModelService
{
    private readonly IPrinterModelRepository _repo;

    public PrinterModelService(IPrinterModelRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<PrinterModel>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public Task<PrinterModel> AddAsync(string name, CancellationToken ct = default) =>
        _repo.AddAsync(name, ct);

    public Task EnsureDefaultAsync(CancellationToken ct = default) =>
        _repo.EnsureDefaultAsync(ct);
}
