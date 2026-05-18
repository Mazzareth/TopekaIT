using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

public sealed class PrinterRouteResolver
{
    private static readonly TimeSpan PrinterMapRefreshInterval = TimeSpan.FromMinutes(5);

    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly SemaphoreSlim _mapLock = new(1, 1);
    private volatile IReadOnlyDictionary<string, PrinterRoute> _printerMap =
        new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _printerMapRefreshedAt;

    public PrinterRouteResolver(
        IDbContextFactory<MasterDbContext> masterFactory,
        IDataProtectionProvider dataProtectionProvider)
    {
        _masterFactory = masterFactory;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task<PrinterRoute?> ResolveAsync(string ip, CancellationToken ct = default)
    {
        await RefreshIfNeededAsync(ct);
        var map = _printerMap;
        var normalizedIp = PrinterIpNormalizer.Normalize(ip);
        return map.TryGetValue(normalizedIp, out var route) ? route : null;
    }

    private async Task RefreshIfNeededAsync(CancellationToken ct)
    {
        var map = _printerMap;
        if (map.Count > 0 && DateTimeOffset.UtcNow - _printerMapRefreshedAt < PrinterMapRefreshInterval)
        {
            return;
        }

        await _mapLock.WaitAsync(ct);
        try
        {
            var mapInner = _printerMap;
            if (mapInner.Count > 0 && DateTimeOffset.UtcNow - _printerMapRefreshedAt < PrinterMapRefreshInterval)
            {
                return;
            }

            var next = new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
            await using var masterDb = await _masterFactory.CreateDbContextAsync(ct);
            var divisions = await masterDb.Divisions.AsNoTracking().ToListAsync(ct);

            foreach (var division in divisions)
            {
                var factory = new DirectDivisionDbContextFactory(division.ConnectionString, _dataProtectionProvider);
                var printers = await new PrinterRepository(factory).GetAllAsync(ct);
                foreach (var printer in printers.Where(p => PrinterModels.SupportsLogging(p.Model) && !string.IsNullOrWhiteSpace(p.IpAddress)))
                {
                    next[PrinterIpNormalizer.Normalize(printer.IpAddress)] =
                        new PrinterRoute(printer.Id, division.ConnectionString);
                }
            }

            _printerMapRefreshedAt = DateTimeOffset.UtcNow;
            _printerMap = next;
        }
        finally
        {
            _mapLock.Release();
        }
    }
}

public sealed record PrinterRoute(string PrinterId, string ConnectionString);
