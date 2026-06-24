using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for printer model names and the default model the portal expects.
/// </summary>
public class PrinterModelRepository : IPrinterModelRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public PrinterModelRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<PrinterModel>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.PrinterModels.AsNoTracking().OrderBy(m => m.Name).ToListAsync(ct);
    }

    public async Task<PrinterModel> AddAsync(string name, CancellationToken ct = default)
    {
        var normalized = NormalizeName(name);

        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.PrinterModels.FirstOrDefaultAsync(
            m => m.Name.ToLower() == normalized.ToLower(),
            ct);
        if (existing != null)
        {
            return existing;
        }

        var model = new PrinterModel
        {
            Name = normalized,
            SupportsLogging = PrinterModels.SupportsLogging(normalized),
        };
        db.PrinterModels.Add(model);
        await db.SaveChangesAsync(ct);
        return model;
    }

    public async Task EnsureDefaultAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (await db.PrinterModels.AnyAsync(m => m.Name == PrinterModels.T8000, ct))
        {
            return;
        }

        db.PrinterModels.Add(new PrinterModel
        {
            Name = PrinterModels.T8000,
            SupportsLogging = true,
        });
        await db.SaveChangesAsync(ct);
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Printer model name is required.", nameof(name));
        }

        return normalized;
    }
}
