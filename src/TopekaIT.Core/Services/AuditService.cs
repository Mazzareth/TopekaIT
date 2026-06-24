using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Runs equipment audits: start a session, record scans, then close it by marking anything expected but unseen.
/// </summary>
public class AuditService
{
    private readonly IAuditRepository _audits;
    private readonly AssetService _assets;

    public AuditService(IAuditRepository audits, AssetService assets)
    {
        _audits = audits;
        _assets = assets;
    }

    public async Task<AuditSession> StartSessionAsync(string divisionId, string conductedBy, string? notes = null, CancellationToken ct = default)
    {
        var session = new AuditSession
        {
            Id = Guid.NewGuid().ToString("N")[..16],
            DivisionId = divisionId,
            ConductedBy = conductedBy,
            StartedAt = DateTimeOffset.UtcNow,
            Notes = notes,
        };

        await _audits.AddSessionAsync(session, ct);
        return session;
    }

    public async Task<AuditEntry> RecordScanAsync(
        string sessionId,
        string scanValue,
        string? actualHolderId = null,
        string? actualLockerId = null,
        CancellationToken ct = default)
    {
        var asset = await _assets.FindByScanAsync(scanValue, ct);
        var entry = asset == null
            ? UnexpectedEntry(sessionId, scanValue)
            : BuildEntry(sessionId, scanValue, asset, actualHolderId, actualLockerId);

        await _audits.AddEntryAsync(entry, ct);
        return entry;
    }

    public async Task<AuditSession?> CompleteSessionAsync(string sessionId, string? notes = null, CancellationToken ct = default)
    {
        var session = await _audits.GetSessionAsync(sessionId, ct);
        if (session == null) return null;

        var entries = (await _audits.GetEntriesAsync(sessionId, ct)).ToList();
        var scannedAssetIds = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.AssetId) && e.Result != AuditResult.Missing)
            .Select(e => e.AssetId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var assets = await _assets.GetAllAsync(ct);
        var missing = assets
            .Where(a => a.Category != AssetCategory.Battery)
            .Where(a => !scannedAssetIds.Contains(a.Id))
            .Select(a => new AuditEntry
            {
                Id = Guid.NewGuid().ToString("N")[..16],
                SessionId = sessionId,
                AssetId = a.Id,
                LockerId = a.LockerId,
                ExpectedHolderId = a.HolderId,
                ExpectedLockerId = a.LockerId,
                ScannedAt = DateTimeOffset.UtcNow,
                Result = AuditResult.Missing,
                IsDiscrepancy = true,
                DiscrepancyReason = "Expected asset was not scanned.",
                DiscrepancyNote = "Expected asset was not scanned.",
            })
            .ToList();

        if (missing.Count > 0)
        {
            await _audits.AddEntriesAsync(missing, ct);
            entries.AddRange(missing);
        }

        session.CompletedAt = DateTimeOffset.UtcNow;
        session.Notes = MergeNotes(session.Notes, notes);
        session.TotalScanned = entries.Count(e => e.Result != AuditResult.Missing);
        session.Discrepancies = entries.Count(e => e.IsDiscrepancy);
        session.MissingCount = entries.Count(e => e.Result == AuditResult.Missing);
        session.UnexpectedCount = entries.Count(e => e.Result == AuditResult.Unexpected);
        await _audits.UpdateSessionAsync(session, ct);
        return session;
    }

    private static AuditEntry BuildEntry(string sessionId, string scanValue, Asset asset, string? actualHolderId, string? actualLockerId)
    {
        var holderMismatch = !MatchesNullable(asset.HolderId, actualHolderId);
        var lockerMismatch = !MatchesNullable(asset.LockerId, actualLockerId);
        var hasActualContext = !string.IsNullOrWhiteSpace(actualHolderId) || !string.IsNullOrWhiteSpace(actualLockerId);
        var discrepancy = hasActualContext && (holderMismatch || lockerMismatch);
        var reason = discrepancy
            ? BuildDiscrepancyReason(holderMismatch, lockerMismatch)
            : null;

        return new AuditEntry
        {
            Id = Guid.NewGuid().ToString("N")[..16],
            SessionId = sessionId,
            AssetId = asset.Id,
            LockerId = asset.LockerId,
            ExpectedHolderId = asset.HolderId,
            ExpectedLockerId = asset.LockerId,
            ActualHolderId = string.IsNullOrWhiteSpace(actualHolderId) ? null : actualHolderId,
            ActualLockerId = string.IsNullOrWhiteSpace(actualLockerId) ? null : actualLockerId,
            ScanValue = scanValue,
            ScannedAt = DateTimeOffset.UtcNow,
            Result = discrepancy ? AuditResult.Discrepancy : AuditResult.Expected,
            IsDiscrepancy = discrepancy,
            DiscrepancyReason = reason,
            DiscrepancyNote = reason,
        };
    }

    private static AuditEntry UnexpectedEntry(string sessionId, string scanValue) => new()
    {
        Id = Guid.NewGuid().ToString("N")[..16],
        SessionId = sessionId,
        AssetId = "",
        ScanValue = scanValue,
        ScannedAt = DateTimeOffset.UtcNow,
        Result = AuditResult.Unexpected,
        IsDiscrepancy = true,
        DiscrepancyReason = "Scanned value does not match a known asset.",
        DiscrepancyNote = "Scanned value does not match a known asset.",
    };

    private static bool MatchesNullable(string? expected, string? actual)
    {
        if (string.IsNullOrWhiteSpace(expected) && string.IsNullOrWhiteSpace(actual)) return true;
        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDiscrepancyReason(bool holderMismatch, bool lockerMismatch) => (holderMismatch, lockerMismatch) switch
    {
        (true, true) => "Holder and locker do not match expected assignment.",
        (true, false) => "Holder does not match expected assignment.",
        _ => "Locker does not match expected assignment.",
    };

    private static string? MergeNotes(string? existing, string? addition)
    {
        if (string.IsNullOrWhiteSpace(addition)) return existing;
        return string.IsNullOrWhiteSpace(existing) ? addition.Trim() : existing + "\n" + addition.Trim();
    }
}
