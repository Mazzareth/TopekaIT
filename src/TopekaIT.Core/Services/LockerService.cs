using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Keeps locker numbers unique, records occupant moves, and nudges the activity feed when assignments change.
/// </summary>
public class LockerService
{
    public const int Ntag213UserBytes = 144;
    public const string RfidPayloadPrefix = "rfid:";

    private readonly ILockerRepository _repo;
    private readonly ActivityService _activity;

    public LockerService(ILockerRepository repo, ActivityService activity)
    {
        _repo = repo;
        _activity = activity;
    }

    public Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<Locker?> FindByRfidAsync(string scanValue, CancellationToken ct = default)
    {
        var token = NormalizeRfidToken(scanValue);
        if (string.IsNullOrWhiteSpace(token)) return null;

        var lockers = await _repo.GetAllAsync(ct);
        return lockers.FirstOrDefault(locker => ScanEquals(locker.RfidTagId, token));
    }

    public async Task<Locker?> GenerateRfidLinkAsync(string lockerId, string actorId, CancellationToken ct = default)
    {
        var existing = await _repo.GetAllAsync(ct);
        var token = GenerateRfidToken(existing);
        return await LinkRfidAsync(lockerId, token, actorId, ct);
    }

    public async Task<Locker?> LinkRfidAsync(string lockerId, string rfidValue, string actorId, CancellationToken ct = default)
    {
        var token = NormalizeRfidToken(rfidValue);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Locker RFID value is required.");
        }

        var payload = BuildRfidPayload(token);
        if (GetPayloadByteCount(payload) > Ntag213UserBytes)
        {
            throw new InvalidOperationException($"Locker RFID payload is too large for NTAG213 ({Ntag213UserBytes} bytes max).");
        }

        var lockers = await _repo.GetAllAsync(ct);
        var duplicate = lockers.FirstOrDefault(locker =>
            !string.Equals(locker.Id, lockerId, StringComparison.OrdinalIgnoreCase) &&
            ScanEquals(locker.RfidTagId, token));
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Locker RFID tag is already linked to locker {duplicate.Number}.");
        }

        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return null;

        locker.RfidTagId = token;
        locker.RfidLinkedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(locker, ct);
        await _activity.PushAsync("locker_rfid_link", $"Locker {locker.Number} linked to RFID tag by {actorId}", ct);
        return locker;
    }

    public async Task<Locker?> ClearRfidLinkAsync(string lockerId, string actorId, CancellationToken ct = default)
    {
        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return null;

        locker.RfidTagId = null;
        locker.RfidLinkedAt = null;
        await _repo.UpdateAsync(locker, ct);
        await _activity.PushAsync("locker_rfid_unlink", $"Locker {locker.Number} RFID tag cleared by {actorId}", ct);
        return locker;
    }

    public async Task AddAsync(Locker locker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locker.Number))
        {
            throw new InvalidOperationException("Locker number is required.");
        }

        var existing = await _repo.GetAllAsync(ct);
        if (existing.Any(l => l.IsActive &&
            string.Equals(l.Number.Trim(), locker.Number.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Locker {locker.Number.Trim()} already exists.");
        }

        locker.Id = Guid.NewGuid().ToString("N")[..16];
        locker.Number = locker.Number.Trim();
        locker.IsActive = true;
        await _repo.AddAsync(locker, ct);
        await _activity.PushAsync("locker_created", $"Locker {locker.Number} created", ct);
    }

    public async Task UpdateAsync(Locker locker, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locker.Number))
        {
            throw new InvalidOperationException("Locker number is required.");
        }

        var existing = await _repo.GetAllAsync(ct);
        if (existing.Any(l => l.Id != locker.Id && l.IsActive &&
            string.Equals(l.Number.Trim(), locker.Number.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Locker {locker.Number.Trim()} already exists.");
        }

        locker.Number = locker.Number.Trim();
        await _repo.UpdateAsync(locker, ct);
    }

    public async Task AssignOccupantAsync(string lockerId, string userId, bool isPrimary, string actorId, CancellationToken ct = default)
    {
        var (locker, addedOccupant) = await _repo.AssignOccupantAsync(
            lockerId,
            userId,
            isPrimary,
            actorId,
            DateTimeOffset.UtcNow,
            ct);
        if (locker == null || !addedOccupant) return;

        await _activity.PushAsync("locker_assign", $"User {userId} assigned to locker {locker.Number} by {actorId}", ct);
    }

    public async Task UnassignOccupantAsync(string lockerId, string userId, string actorId, CancellationToken ct = default)
    {
        var locker = await _repo.UnassignOccupantAsync(
            lockerId,
            userId,
            actorId,
            DateTimeOffset.UtcNow,
            ct);
        if (locker == null) return;

        await _activity.PushAsync("locker_unassign", $"User {userId} removed from locker {locker.Number} by {actorId}", ct);
    }

    public async Task RecordAuditAsync(string lockerId, string auditorId, CancellationToken ct = default)
    {
        var locker = await _repo.GetByIdAsync(lockerId, ct);
        if (locker == null) return;

        locker.LastAuditedAt = DateTimeOffset.UtcNow;
        locker.LastAuditedBy = auditorId;

        await _repo.UpdateAsync(locker, ct);
    }

    public async Task<IEnumerable<Locker>> GetAuditOverdueAsync(CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var now = DateTimeOffset.UtcNow;
        return all.Where(l =>
            l.IsActive &&
            l.AuditCadenceDays.HasValue &&
            (l.LastAuditedAt == null || (now - l.LastAuditedAt.Value).TotalDays >= l.AuditCadenceDays.Value));
    }

    public static string BuildRfidPayload(string? rfidTagId)
    {
        var token = NormalizeRfidToken(rfidTagId ?? "");
        return string.IsNullOrWhiteSpace(token) ? "" : $"{RfidPayloadPrefix}{token}";
    }

    public static int GetPayloadByteCount(string value) => System.Text.Encoding.UTF8.GetByteCount(value);

    private static string GenerateRfidToken(IReadOnlyList<Locker> existing)
    {
        while (true)
        {
            var token = "NTAG-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            if (!existing.Any(locker => ScanEquals(locker.RfidTagId, token)))
            {
                return token;
            }
        }
    }

    private static bool ScanEquals(string? value, string normalizedScan)
        => string.Equals(NormalizeRfidToken(value ?? ""), normalizedScan, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRfidToken(string value)
    {
        var cleaned = value.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return "";

        if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in new[] { "locker", "rfid", "nfc", "ntag", "uid", "id" })
            {
                var match = query.FirstOrDefault(part => part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
                if (match != null) return Uri.UnescapeDataString(match[(key.Length + 1)..]).Trim();
            }

            cleaned = uri.Segments.LastOrDefault()?.Trim('/') ?? cleaned;
        }

        foreach (var prefix in new[] { "locker:", "rfid:", "nfc:", "ntag:", "uid:", "id:" })
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned[prefix.Length..].Trim();
                break;
            }
        }

        return cleaned;
    }
}
