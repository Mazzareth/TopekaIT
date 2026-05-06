using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class PrinterService
{
    private readonly IPrinterRepository _repo;

    public PrinterService(IPrinterRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Printer?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<Printer> AddAsync(Printer input, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var id = NextPrinterId(all);
        var printer = new Printer
        {
            Id = id,
            Name = input.Name,
            Department = input.Department,
            Model = string.IsNullOrWhiteSpace(input.Model) ? PrinterModels.T8000 : input.Model,
            IpAddress = input.IpAddress,
            Status = PrinterStatus.Up,
        };
        await _repo.AddAsync(printer, ct);
        return printer;
    }

    public Task UpdateAsync(Printer printer, CancellationToken ct = default) => _repo.UpdateAsync(printer, ct);
    public Task DeleteAsync(string id, CancellationToken ct = default) => _repo.RemoveAsync(id, ct);

    public async Task<Printer?> ToggleStatusAsync(string id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct);
        if (p == null) return null;
        p.Status = p.Status == PrinterStatus.Up ? PrinterStatus.Down : PrinterStatus.Up;
        await _repo.UpdateAsync(p, ct);
        return p;
    }

    private static string NextPrinterId(IReadOnlyList<Printer> printers)
    {
        var used = printers.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = printers.Count + 1; i < 1000; i++)
        {
            var id = $"p-{i:D2}";
            if (!used.Contains(id))
            {
                return id;
            }
        }

        return $"p-{Guid.NewGuid():N}"[..16];
    }
}
