using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// Stores raw printer events, keeps current alert state warm, and builds the flattened reports the printer screens can scan quickly.
/// </summary>
public class PrinterEventRepository : IPrinterEventRepository
{
    private static readonly TimeSpan ActiveIncidentWindow = TimeSpan.FromDays(2);
    private readonly IDivisionDbContextFactory _factory;
    private readonly IDivisionRepository? _divisionRepository;
    private readonly ITenantContext? _tenantContext;
    private readonly IDataProtectionProvider? _dataProtectionProvider;

    public PrinterEventRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public PrinterEventRepository(
        IDivisionDbContextFactory factory,
        IDivisionRepository divisionRepository,
        ITenantContext tenantContext,
        IDataProtectionProvider dataProtectionProvider)
    {
        _factory = factory;
        _divisionRepository = divisionRepository;
        _tenantContext = tenantContext;
        _dataProtectionProvider = dataProtectionProvider;
    }

    public async Task AddAsync(PrinterEvent ev, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.PrinterEvents.Add(ev);
        await db.SaveChangesAsync(ct);
        await UpsertAlertStateAsync(db, ev, ct);
    }

    public async Task<IReadOnlyList<PrinterEvent>> GetByPrinterAsync(string printerId, int count, CancellationToken ct = default)
        => await GetByPrinterAsync(printerId, count, null, null, ct);

    public async Task<IReadOnlyList<PrinterEvent>> GetByPrinterAsync(
        string printerId,
        int count,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.PrinterEvents
            .AsNoTracking()
            .Where(e => e.PrinterId == printerId);

        query = ApplyTimestampRange(query, from, to);
        query = query.OrderByDescending(e => e.Timestamp);
        if (count > 0)
        {
            query = query.Take(count);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterLogEntry>> GetLogsAsync(
        IReadOnlyCollection<string> printerIds,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int count = 0,
        CancellationToken ct = default)
    {
        var ids = printerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (ids.Count == 0)
        {
            return Array.Empty<PrinterLogEntry>();
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var query = db.PrinterEvents
            .AsNoTracking()
            .Include(e => e.Printer)
            .Where(e => ids.Contains(e.PrinterId));

        query = ApplyTimestampRange(query, from, to);

        var logs = query
            .OrderByDescending(e => e.Timestamp)
            .Select(e => new PrinterLogEntry
            {
                Id = e.Id,
                PrinterId = e.PrinterId,
                PrinterName = e.Printer.Name,
                Department = e.Printer.Department,
                IpAddress = e.Printer.IpAddress,
                Timestamp = e.Timestamp,
                EventType = e.EventType,
                RawMessage = e.RawMessage,
                Severity = e.Severity,
                AlertKey = e.AlertKey,
                AlertTitle = e.AlertTitle,
                AlertCategory = e.AlertCategory,
                AlertDetail = e.AlertDetail,
                FriendlyMessage = e.FriendlyMessage,
                AlertTrainingLevel = e.AlertTrainingLevel,
            });

        if (count > 0)
        {
            logs = logs.Take(count);
        }

        return await logs.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.PrinterAlertStates
            .AsNoTracking()
            .Where(a => !a.BlipSuppressed)
            .OrderByDescending(a => a.LastSeenAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterAlertState>> GetActiveAlertsByPrinterAsync(string printerId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.PrinterAlertStates
            .AsNoTracking()
            .Where(a => a.PrinterId == printerId)
            .Where(a => !a.BlipSuppressed)
            .OrderByDescending(a => a.LastSeenAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetActiveIncidentsAsync(CancellationToken ct = default)
    {
        if (_divisionRepository == null || _tenantContext == null)
        {
            throw new InvalidOperationException("Division context is required to query active printer incidents.");
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var divisionId = _tenantContext.DivisionId ?? "";
        var divisionName = divisionId;
        if (!string.IsNullOrWhiteSpace(divisionId))
        {
            var division = await _divisionRepository.GetByIdAsync(divisionId, ct);
            divisionName = division?.Name ?? divisionId;
        }

        return await BuildActiveIncidentQuery(db, divisionId, divisionName)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterActiveIncidentReportRow>> GetAllDivisionActiveIncidentsAsync(CancellationToken ct = default)
    {
        if (_divisionRepository == null || _dataProtectionProvider == null)
        {
            throw new InvalidOperationException("Division repository is required to query active printer incidents.");
        }

        var divisions = await _divisionRepository.GetAllAsync(ct);
        var incidents = new List<PrinterActiveIncidentReportRow>();

        foreach (var division in divisions)
        {
            var factory = new DirectDivisionDbContextFactory(division.ConnectionString, _dataProtectionProvider);
            await using var db = await factory.CreateDbContextAsync(ct);
            var divisionIncidents = await BuildActiveIncidentQuery(db, division.Id, division.Name)
                .ToListAsync(ct);
            incidents.AddRange(divisionIncidents);
        }

        return incidents
            .OrderByDescending(i => SeverityRank(i.Severity))
            .ThenByDescending(i => i.LastSeenAt)
            .ToList();
    }

    public async Task SetAlertBlipSuppressedAsync(string printerId, string alertKey, bool suppressed, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var alert = await db.PrinterAlertStates
            .FirstOrDefaultAsync(a => a.PrinterId == printerId && a.AlertKey == alertKey, ct);
        if (alert == null) return;

        alert.BlipSuppressed = suppressed;
        await db.SaveChangesAsync(ct);
    }

    public async Task ClearAlertAsync(string printerId, string alertKey, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var alert = await db.PrinterAlertStates
            .FirstOrDefaultAsync(a => a.PrinterId == printerId && a.AlertKey == alertKey, ct);
        if (alert == null) return;

        db.PrinterAlertStates.Remove(alert);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> PurgeEventsOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var oldEvents = await db.PrinterEvents
            .Where(e => e.Timestamp < cutoff)
            .ToListAsync(ct);
        db.PrinterEvents.RemoveRange(oldEvents);
        await db.SaveChangesAsync(ct);
        return oldEvents.Count;
    }

    public async Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(int count, CancellationToken ct = default)
        => await GetErrorsAsync(count, null, null, ct);

    public async Task<IReadOnlyList<PrinterErrorLogEntry>> GetErrorsAsync(
        int count,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        if (_divisionRepository == null || _tenantContext == null)
        {
            throw new InvalidOperationException("Division context is required to query printer errors.");
        }

        await using var db = await _factory.CreateDbContextAsync(ct);
        var divisionId = _tenantContext.DivisionId ?? "";
        var divisionName = divisionId;
        if (!string.IsNullOrWhiteSpace(divisionId))
        {
            var division = await _divisionRepository.GetByIdAsync(divisionId, ct);
            divisionName = division?.Name ?? divisionId;
        }

        return await ApplyLimit(BuildErrorQuery(db, divisionId, divisionName, from, to), count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(int count, CancellationToken ct = default)
        => await GetAllDivisionErrorsAsync(count, null, null, ct);

    public async Task<IReadOnlyList<PrinterErrorLogEntry>> GetAllDivisionErrorsAsync(
        int count,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        if (_divisionRepository == null || _dataProtectionProvider == null)
        {
            throw new InvalidOperationException("Division repository is required to query printer errors.");
        }

        var divisions = await _divisionRepository.GetAllAsync(ct);
        var entries = new List<PrinterErrorLogEntry>();

        foreach (var division in divisions)
        {
            var factory = new DirectDivisionDbContextFactory(division.ConnectionString, _dataProtectionProvider);
            await using var db = await factory.CreateDbContextAsync(ct);
            var divisionEntries = await ApplyLimit(BuildErrorQuery(db, division.Id, division.Name, from, to), count)
                .ToListAsync(ct);
            entries.AddRange(divisionEntries);
        }

        var ordered = entries.OrderByDescending(e => e.Timestamp);
        return count > 0
            ? ordered.Take(count).ToList()
            : ordered.ToList();
    }

    public async Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(int count, CancellationToken ct = default)
        => await GetGroupedErrorsAsync(count, null, null, ct);

    public async Task<IReadOnlyList<PrinterAlertGroup>> GetGroupedErrorsAsync(
        int count,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var entries = await GetErrorsAsync(count, from, to, ct);
        return BuildGroups(entries.Select(ToOccurrence));
    }

    public async Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(int count, CancellationToken ct = default)
        => await GetAllDivisionGroupedErrorsAsync(count, null, null, ct);

    public async Task<IReadOnlyList<PrinterAlertGroup>> GetAllDivisionGroupedErrorsAsync(
        int count,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var entries = await GetAllDivisionErrorsAsync(count, from, to, ct);
        return BuildGroups(entries.Select(ToOccurrence));
    }

    private static IQueryable<PrinterErrorLogEntry> BuildErrorQuery(
        TopekaDbContext db,
        string divisionId,
        string divisionName,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        var query = db.PrinterEvents
            .AsNoTracking()
            .Include(e => e.Printer)
            .Where(e =>
                e.Severity == "Error" ||
                e.Severity == "Critical" ||
                e.Severity == "Warning" ||
                e.EventType == "Error" ||
                e.EventType == "Warning");

        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.Timestamp < to.Value);
        }

        return query
            .OrderByDescending(e => e.Timestamp)
            .Select(e => new PrinterErrorLogEntry
            {
                Id = e.Id,
                DivisionId = divisionId,
                DivisionName = divisionName,
                PrinterId = e.PrinterId,
                PrinterName = e.Printer.Name,
                Department = e.Printer.Department,
                IpAddress = e.Printer.IpAddress,
                Timestamp = e.Timestamp,
                EventType = e.EventType,
                RawMessage = e.RawMessage,
                Severity = e.Severity,
                AlertKey = e.AlertKey,
                AlertTitle = e.AlertTitle,
                AlertCategory = e.AlertCategory,
                AlertDetail = e.AlertDetail,
                FriendlyMessage = e.FriendlyMessage,
                AlertTrainingLevel = e.AlertTrainingLevel,
            });
    }

    private static IQueryable<PrinterEvent> ApplyTimestampRange(
        IQueryable<PrinterEvent> query,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (from.HasValue)
        {
            query = query.Where(e => e.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.Timestamp < to.Value);
        }

        return query;
    }

    private static IQueryable<PrinterActiveIncidentReportRow> BuildActiveIncidentQuery(
        TopekaDbContext db,
        string divisionId,
        string divisionName)
    {
        var activeSince = DateTimeOffset.UtcNow.Subtract(ActiveIncidentWindow);

        return db.PrinterAlertStates
            .AsNoTracking()
            .Include(a => a.Printer)
            .Where(a => !a.BlipSuppressed)
            .Where(a => a.LastSeenAt >= activeSince)
            .OrderByDescending(a => a.LastSeenAt)
            .Select(a => new PrinterActiveIncidentReportRow
            {
                DivisionId = divisionId,
                DivisionName = divisionName,
                PrinterId = a.PrinterId,
                PrinterName = a.Printer.Name,
                Department = a.Printer.Department,
                IpAddress = a.Printer.IpAddress,
                AlertKey = a.AlertKey,
                AlertTitle = a.AlertTitle,
                AlertCategory = a.AlertCategory,
                AlertDetail = a.AlertDetail,
                FriendlyMessage = a.FriendlyMessage,
                Severity = a.Severity,
                TrainingLevel = a.TrainingLevel,
                FirstSeenAt = a.FirstSeenAt,
                LastSeenAt = a.LastSeenAt,
                LastEventId = a.LastEventId,
                OccurrenceCount = a.OccurrenceCount,
            });
    }

    private static IQueryable<PrinterErrorLogEntry> ApplyLimit(
        IQueryable<PrinterErrorLogEntry> query,
        int count)
    {
        return count > 0 ? query.Take(count) : query;
    }

    // A noisy printer should update one open alert, not create a brand-new incident every time it repeats itself.
    private static async Task UpsertAlertStateAsync(TopekaDbContext db, PrinterEvent ev, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ev.AlertKey) || IsInfoSeverity(ev.Severity))
        {
            return;
        }

        var existing = await db.PrinterAlertStates
            .FirstOrDefaultAsync(a => a.PrinterId == ev.PrinterId && a.AlertKey == ev.AlertKey, ct);

        if (existing == null)
        {
            db.PrinterAlertStates.Add(new PrinterAlertState
            {
                PrinterId = ev.PrinterId,
                AlertKey = ev.AlertKey,
                AlertTitle = ev.AlertTitle ?? ev.AlertKey,
                AlertCategory = ev.AlertCategory ?? "Printer Alert",
                AlertDetail = ev.AlertDetail,
                FriendlyMessage = ev.FriendlyMessage,
                Severity = ev.Severity ?? "Info",
                TrainingLevel = ev.AlertTrainingLevel,
                FirstSeenAt = ev.Timestamp,
                LastSeenAt = ev.Timestamp,
                LastEventId = ev.Id,
                OccurrenceCount = 1,
            });
        }
        else
        {
            existing.AlertTitle = ev.AlertTitle ?? existing.AlertTitle;
            existing.AlertCategory = ev.AlertCategory ?? existing.AlertCategory;
            existing.AlertDetail = ev.AlertDetail;
            existing.FriendlyMessage = ev.FriendlyMessage;
            existing.Severity = ev.Severity ?? existing.Severity;
            existing.TrainingLevel = ev.AlertTrainingLevel;
            existing.LastSeenAt = ev.Timestamp;
            existing.LastEventId = ev.Id;
            existing.OccurrenceCount += 1;
            existing.BlipSuppressed = false;
        }

        await db.SaveChangesAsync(ct);
    }

    // Stored events may predate newer normalization rules, so rebuild the friendly alert shape while preserving the raw message.
    private static PrinterAlertOccurrence ToOccurrence(PrinterErrorLogEntry entry)
    {
        var normalized = PrinterAlertNormalizer.Normalize(entry.RawMessage, entry.EventType, entry.Severity);
        var useNormalized = PrinterAlertNormalizer.IsAuthenticationFailure(
            rawMessage: entry.RawMessage,
            eventType: entry.EventType,
            alertKey: entry.AlertKey,
            alertTitle: entry.AlertTitle,
            alertDetail: entry.AlertDetail,
            friendlyMessage: entry.FriendlyMessage);

        return new PrinterAlertOccurrence
        {
            EventId = entry.Id,
            DivisionId = entry.DivisionId,
            DivisionName = entry.DivisionName,
            PrinterId = entry.PrinterId,
            PrinterName = entry.PrinterName,
            Department = entry.Department,
            IpAddress = entry.IpAddress,
            Timestamp = entry.Timestamp,
            AlertKey = useNormalized ? normalized.AlertKey : entry.AlertKey ?? normalized.AlertKey,
            AlertTitle = useNormalized ? normalized.AlertTitle : entry.AlertTitle ?? normalized.AlertTitle,
            AlertCategory = useNormalized ? normalized.AlertCategory : entry.AlertCategory ?? normalized.AlertCategory,
            AlertDetail = useNormalized ? normalized.AlertDetail : entry.AlertDetail ?? normalized.AlertDetail,
            FriendlyMessage = useNormalized ? normalized.FriendlyMessage : entry.FriendlyMessage ?? normalized.FriendlyMessage,
            Severity = useNormalized ? normalized.Severity : entry.Severity ?? normalized.Severity,
            TrainingLevel = useNormalized ? normalized.TrainingLevel : entry.AlertTrainingLevel ?? normalized.TrainingLevel,
            RawMessage = entry.RawMessage,
        };
    }

    // Group by alert key first, then sort by "how bad and how recent" because that is how people triage the list.
    private static IReadOnlyList<PrinterAlertGroup> BuildGroups(IEnumerable<PrinterAlertOccurrence> entries)
    {
        return entries
            .GroupBy(e => e.AlertKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(e => e.Timestamp).ToList();
                var latest = ordered[0];

                return new PrinterAlertGroup
                {
                    AlertKey = latest.AlertKey,
                    AlertTitle = latest.AlertTitle,
                    AlertCategory = latest.AlertCategory,
                    AlertDetail = latest.AlertDetail,
                    Severity = HighestSeverity(ordered.Select(e => e.Severity)),
                    Count = ordered.Count,
                    LatestAt = latest.Timestamp,
                    Occurrences = ordered,
                };
            })
            .OrderByDescending(g => SeverityRank(g.Severity))
            .ThenByDescending(g => g.LatestAt)
            .ToList();
    }

    private static string HighestSeverity(IEnumerable<string> severities)
    {
        return severities
            .OrderByDescending(SeverityRank)
            .FirstOrDefault() ?? "Info";
    }

    private static int SeverityRank(string? severity) => severity?.ToUpperInvariant() switch
    {
        "CRITICAL" => 3,
        "ERROR" => 3,
        "WARNING" => 2,
        _ => 1,
    };

    private static bool IsInfoSeverity(string? severity) =>
        SeverityRank(severity) <= 1;
}
