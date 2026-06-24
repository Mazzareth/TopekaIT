using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for printers in the active division.
/// </summary>
public class PrinterRepository : IPrinterRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public PrinterRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task<IReadOnlyList<Printer>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Printers.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
    }

    public async Task<Printer?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Printers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task AddAsync(Printer printer, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Printers.Add(printer);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Printer printer, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Printers.Update(printer);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var p = await db.Printers.FindAsync(new object?[] { id }, ct);
        if (p == null) return;
        db.Printers.Remove(p);
        await db.SaveChangesAsync(ct);
    }
}
