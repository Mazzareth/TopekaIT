using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class AssetService
{
    public const int Ntag213UserBytes = 144;
    public const string RfidPayloadPrefix = "rfid:";

    private readonly IAssetRepository _repo;
    private readonly ActivityService _activity;

    // Only one primary location or workflow state should be set at a time.
    private static readonly StatusFlags[] PrimaryFlags =
    [
        StatusFlags.InLocker, StatusFlags.InCC, StatusFlags.WithHolder,
        StatusFlags.OnLoan, StatusFlags.InRepair, StatusFlags.InRMA,
        StatusFlags.Missing, StatusFlags.OnHold, StatusFlags.Spare,
    ];

    public AssetService(IAssetRepository repo, ActivityService activity)
    {
        _repo = repo;
        _activity = activity;
    }

    public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);
    public Task DeleteAsync(string id, CancellationToken ct = default) => _repo.RemoveAsync(id, ct);

    public async Task AddAsync(Asset asset, CancellationToken ct = default)
    {
        Normalize(asset);
        Validate(asset);
        await _repo.AddAsync(asset, ct);
    }

    public async Task<Asset?> FindByScanAsync(string scanValue, CancellationToken ct = default)
    {
        var normalized = NormalizeScanValue(scanValue);
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        var all = await _repo.GetAllAsync(ct);
        return all.FirstOrDefault(a =>
            ScanEquals(a.Tag, normalized) ||
            ScanEquals(a.Serial, normalized) ||
            ScanEquals(a.Imei, normalized) ||
            ScanEquals(a.RfidTagId, normalized) ||
            ScanEquals(a.Id, normalized));
    }

    public async Task<Asset?> GenerateRfidLinkAsync(string assetId, string actorName, CancellationToken ct = default)
    {
        var existing = await _repo.GetAllAsync(ct);
        var token = GenerateRfidToken(existing);
        return await LinkRfidAsync(assetId, token, actorName, ct);
    }

    public async Task<Asset?> LinkRfidAsync(string assetId, string rfidValue, string actorName, CancellationToken ct = default)
    {
        var token = NormalizeRfidToken(rfidValue);
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("RFID value is required.");

        var payload = BuildRfidPayload(token);
        if (GetPayloadByteCount(payload) > Ntag213UserBytes)
            throw new InvalidOperationException($"RFID payload is too large for NTAG213 ({Ntag213UserBytes} bytes max).");

        var all = await _repo.GetAllAsync(ct);
        var duplicate = all.FirstOrDefault(a => a.Id != assetId && ScanEquals(a.RfidTagId, token));
        if (duplicate != null)
            throw new InvalidOperationException($"RFID tag is already linked to {AssetLabel(duplicate)}.");

        var asset = await _repo.GetByIdAsync(assetId, ct);
        if (asset == null) return null;

        asset.RfidTagId = token;
        asset.RfidLinkedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(asset, ct);

        await _activity.PushAsync("rfid_link", $"{AssetLabel(asset)} linked to RFID tag by {actorName}", ct);
        return asset;
    }

    public async Task<Asset?> ClearRfidLinkAsync(string assetId, string actorName, CancellationToken ct = default)
    {
        var asset = await _repo.GetByIdAsync(assetId, ct);
        if (asset == null) return null;

        asset.RfidTagId = null;
        asset.RfidLinkedAt = null;
        await _repo.UpdateAsync(asset, ct);

        await _activity.PushAsync("rfid_unlink", $"{AssetLabel(asset)} RFID tag cleared by {actorName}", ct);
        return asset;
    }

    public async Task<Asset?> CheckOutAsync(string assetId, string holderId, int dueDays, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return null;
        if (a.Status != AssetStatus.Out) a.StatusChangedAt = DateTimeOffset.UtcNow;
        a.Status = AssetStatus.Out;
        a.HolderId = holderId;
        a.CheckedOutAt = DateTimeOffset.UtcNow;
        a.DueAt = DateTimeOffset.UtcNow.AddDays(dueDays);

        SetPrimaryFlag(a, StatusFlags.WithHolder);

        await _repo.UpdateAsync(a, ct);
        return a;
    }

    public async Task<Asset?> CheckInAsync(string assetId, string condition, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return null;

        var newStatus = condition == "repair" ? AssetStatus.Repair : AssetStatus.In;
        if (a.Status != newStatus)
        {
            a.StatusChangedAt = DateTimeOffset.UtcNow;
        }
        a.Status = newStatus;

        SetPrimaryFlag(a, condition == "repair" ? StatusFlags.InRepair : StatusFlags.InCC);

        a.HolderId = null;
        a.CheckedOutAt = null;
        a.DueAt = null;
        if (condition != "ok")
        {
            var line = $"{DateTime.UtcNow:yyyy-MM-dd}: {(condition == "repair" ? "Sent to IT for repair" : "Returned with issue")}";
            a.Notes = string.IsNullOrEmpty(a.Notes) ? line : a.Notes + "\n" + line;
        }
        await _repo.UpdateAsync(a, ct);
        return a;
    }

    public async Task SetStatusAsync(string assetId, AssetStatus newStatus, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return;

        var oldStatus = a.Status;
        if (oldStatus != newStatus)
        {
            a.Status = newStatus;
            a.StatusChangedAt = DateTimeOffset.UtcNow;
            await _repo.UpdateAsync(a, ct);

            await _activity.PushAsync("status_change", $"{a.Tag ?? a.Serial} status changed to {newStatus} by {actorName}", ct);
        }
    }

    public async Task SetFlagsAsync(string assetId, StatusFlags flagsToSet, StatusFlags flagsToClear, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return;

        // External callers may set modifiers and primary flags in the same call; primary flags still remain exclusive.
        foreach (var primary in PrimaryFlags)
        {
            if (flagsToSet.HasFlag(primary))
            {
                foreach (var other in PrimaryFlags)
                    if (other != primary) a.Flags &= ~other;
            }
        }

        a.Flags |= flagsToSet;
        a.Flags &= ~flagsToClear;
        a.StatusChangedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(a, ct);
        await _activity.PushAsync("flags_change", $"{a.Tag ?? a.Serial} flags updated by {actorName}", ct);
    }

    public async Task AssignHolderAsync(string assetId, string userId, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return;

        var isUnassign = string.IsNullOrWhiteSpace(userId);
        a.HolderId = isUnassign ? null : userId;
        if (!isUnassign)
        {
            a.CheckedOutAt = DateTimeOffset.UtcNow;
            SetPrimaryFlag(a, StatusFlags.WithHolder);
        }
        else
        {
            a.CheckedOutAt = null;
            SetPrimaryFlag(a, string.IsNullOrWhiteSpace(a.LockerId) ? StatusFlags.InCC : StatusFlags.InLocker);
        }

        await _repo.UpdateAsync(a, ct);

        var msg = isUnassign ? "unassigned" : $"assigned to {userId}";
        await _activity.PushAsync("assignment", $"{a.Tag ?? a.Serial} {msg} by {actorName}", ct);
    }

    public async Task AssignToLockerAsync(string assetId, string? lockerId, string? lockerNumber, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return;

        var assigning = !string.IsNullOrWhiteSpace(lockerId);
        a.LockerId = assigning ? lockerId : null;
        a.LastSeenAt = DateTimeOffset.UtcNow;
        a.LastSeenLocation = assigning ? lockerNumber?.Trim() : null;

        if (assigning)
        {
            SetPrimaryFlag(a, string.IsNullOrWhiteSpace(a.HolderId) ? StatusFlags.InLocker : StatusFlags.WithHolder);
            var nextStatus = string.IsNullOrWhiteSpace(a.HolderId) ? AssetStatus.InLocker : AssetStatus.InUse;
            if (a.Status != nextStatus)
            {
                a.Status = nextStatus;
                a.StatusChangedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            a.Flags &= ~StatusFlags.InLocker;
            if (!string.IsNullOrWhiteSpace(a.HolderId))
            {
                SetPrimaryFlag(a, StatusFlags.WithHolder);
            }
            else if (!FormatHasAttention(a.Flags))
            {
                SetPrimaryFlag(a, StatusFlags.Spare);
                if (a.Status != AssetStatus.Spare)
                {
                    a.Status = AssetStatus.Spare;
                    a.StatusChangedAt = DateTimeOffset.UtcNow;
                }
            }
        }

        await _repo.UpdateAsync(a, ct);

        var label = a.Tag ?? a.Serial;
        var msg = assigning
            ? $"{label} assigned to locker {lockerNumber} by {actorName}"
            : $"{label} removed from locker by {actorName}";
        await _activity.PushAsync("locker_asset", msg, ct);
    }

    public async Task PairScannerAsync(string scannerAssetId, string? saeAssetId, string actorName, CancellationToken ct = default)
    {
        var scanner = await _repo.GetByIdAsync(scannerAssetId, ct);
        if (scanner == null || scanner.Category != AssetCategory.Scanner) return;

        scanner.PairedAssetId = string.IsNullOrWhiteSpace(saeAssetId) ? null : saeAssetId;
        await _repo.UpdateAsync(scanner, ct);

        var pairedTo = saeAssetId ?? "none";
        await _activity.PushAsync("scanner_pair", $"Scanner {scanner.Serial} paired to SAE {pairedTo} by {actorName}", ct);
    }

    public async Task IssueSpareAsync(string spareAssetId, string borrowerId, string reason, LoanDuration duration, string comments, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(spareAssetId, ct);
        if (a == null) return;

        if (a.Status != AssetStatus.Loaned) a.StatusChangedAt = DateTimeOffset.UtcNow;
        a.Status = AssetStatus.Loaned;

        var newFlags = StatusFlags.OnLoan;
        if (duration == LoanDuration.DayLoan) newFlags |= StatusFlags.DayLoan;
        SetPrimaryFlag(a, newFlags);

        var loan = new LoanRecord
        {
            AssetId = a.Id,
            BorrowerId = borrowerId,
            Reason = reason,
            Duration = duration,
            IsDayLoan = duration == LoanDuration.DayLoan,
            Comments = comments,
            DateLoaned = DateTimeOffset.UtcNow
        };
        a.LoanRecords.Add(loan);

        await _repo.UpdateAsync(a, ct);
        await _activity.PushAsync("loan_out", $"Spare {a.Tag ?? a.Serial} loaned to {borrowerId} by {actorName}", ct);
    }

    public async Task ReturnSpareAsync(string loanRecordId, string actorName, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        var loan = all.SelectMany(a => a.LoanRecords).FirstOrDefault(r => r.Id == loanRecordId);
        if (loan == null) return;

        var a = await _repo.GetByIdAsync(loan.AssetId, ct);
        if (a == null) return;

        var recordToUpdate = a.LoanRecords.FirstOrDefault(r => r.Id == loanRecordId);
        if (recordToUpdate != null)
        {
            recordToUpdate.DateReturned = DateTimeOffset.UtcNow;
        }

        if (a.Status != AssetStatus.Spare) a.StatusChangedAt = DateTimeOffset.UtcNow;
        a.Status = AssetStatus.Spare;
        SetPrimaryFlag(a, StatusFlags.Spare);
        a.Flags &= ~StatusFlags.DayLoan;

        await _repo.UpdateAsync(a, ct);
        await _activity.PushAsync("loan_in", $"Spare {a.Tag ?? a.Serial} returned by {loan.BorrowerId} (via {actorName})", ct);
    }

    public Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default) => _repo.GetSparePoolAsync(ct);
    public Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default) => _repo.GetActiveLoansAsync(ct);

    public static string GetSimpleState(StatusFlags flags)
    {
        if (flags.HasFlag(StatusFlags.InRMA) ||
            flags.HasFlag(StatusFlags.InRepair) ||
            flags.HasFlag(StatusFlags.Missing) ||
            flags.HasFlag(StatusFlags.OnHold) ||
            flags.HasFlag(StatusFlags.UnderInvestigation))
            return "Attention";

        if (flags.HasFlag(StatusFlags.OnLoan))
            return "Loaned";

        if (flags.HasFlag(StatusFlags.WithHolder))
            return "In Use";

        return "Available";
    }

    private static void SetPrimaryFlag(Asset a, StatusFlags newPrimary)
    {
        foreach (var primary in PrimaryFlags)
            a.Flags &= ~primary;
        a.Flags |= newPrimary;
    }

    private static void Normalize(Asset asset)
    {
        asset.Tag = asset.Tag.Trim();
        asset.Serial = asset.Serial.Trim();
        asset.Model = asset.Model.Trim();
        asset.Imei = string.IsNullOrWhiteSpace(asset.Imei) ? null : asset.Imei.Trim();
        asset.RfidTagId = string.IsNullOrWhiteSpace(asset.RfidTagId) ? null : NormalizeRfidToken(asset.RfidTagId);
        asset.Notes = asset.Notes.Trim();
        asset.Quantity = asset.Category == AssetCategory.Battery ? Math.Max(asset.Quantity, 0) : 1;
        asset.IsSAE = asset.Category == AssetCategory.SaeDevice;
        asset.Type = asset.Category switch
        {
            AssetCategory.Battery => "battery",
            AssetCategory.PodTc77 => "pod-tc77",
            AssetCategory.Scanner  => "scanner",
            _                      => "sae",
        };
    }

    private static void Validate(Asset asset)
    {
        if (asset.Category == AssetCategory.PodTc77 && string.IsNullOrWhiteSpace(asset.Serial))
            throw new InvalidOperationException("TC77 POD devices require a serial number.");

        if (asset.Category == AssetCategory.Scanner && string.IsNullOrWhiteSpace(asset.Serial))
            throw new InvalidOperationException("Scanners require a serial number.");

        if (asset.Category == AssetCategory.Battery && asset.Quantity < 0)
            throw new InvalidOperationException("Battery quantity cannot be negative.");

        if (asset.Category != AssetCategory.Battery && string.IsNullOrWhiteSpace(asset.Tag) && string.IsNullOrWhiteSpace(asset.Serial))
            throw new InvalidOperationException("Tracked devices need an asset tag or serial number.");
    }

    private static bool ScanEquals(string? value, string normalizedScan)
        => string.Equals(NormalizeScanValue(value ?? ""), normalizedScan, StringComparison.OrdinalIgnoreCase);

    public static string BuildRfidPayload(string? rfidTagId)
    {
        var token = NormalizeRfidToken(rfidTagId ?? "");
        return string.IsNullOrWhiteSpace(token) ? "" : $"{RfidPayloadPrefix}{token}";
    }

    public static int GetPayloadByteCount(string value) => System.Text.Encoding.UTF8.GetByteCount(value);

    private static string GenerateRfidToken(IReadOnlyList<Asset> existing)
    {
        while (true)
        {
            var token = "NTAG-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            if (!existing.Any(a => ScanEquals(a.RfidTagId, token)))
                return token;
        }
    }

    private static string NormalizeRfidToken(string value) => NormalizeScanValue(value).Trim();

    private static string AssetLabel(Asset asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.Tag)) return asset.Tag;
        if (!string.IsNullOrWhiteSpace(asset.Serial)) return asset.Serial;
        return asset.Id;
    }

    private static bool FormatHasAttention(StatusFlags flags) =>
        flags.HasFlag(StatusFlags.InRMA) ||
        flags.HasFlag(StatusFlags.InRepair) ||
        flags.HasFlag(StatusFlags.Missing) ||
        flags.HasFlag(StatusFlags.OnHold) ||
        flags.HasFlag(StatusFlags.UnderInvestigation);

    private static string NormalizeScanValue(string value)
    {
        var cleaned = value.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return "";

        if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in new[] { "asset", "tag", "serial", "sn", "imei", "rfid", "nfc", "ntag", "uid", "id" })
            {
                var match = query.FirstOrDefault(part => part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
                if (match != null) return Uri.UnescapeDataString(match[(key.Length + 1)..]).Trim();
            }

            cleaned = uri.Segments.LastOrDefault()?.Trim('/') ?? cleaned;
        }

        foreach (var prefix in new[] { "asset:", "tag:", "serial:", "sn:", "imei:", "rfid:", "nfc:", "ntag:", "uid:", "id:" })
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
