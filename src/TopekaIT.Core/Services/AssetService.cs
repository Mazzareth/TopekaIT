using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class AssetService
{
    private readonly IAssetRepository _repo;
    private readonly ActivityService _activity;

    public AssetService(IAssetRepository repo, ActivityService activity)
    {
        _repo = repo;
        _activity = activity;
    }

    public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

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
            ScanEquals(a.Id, normalized));
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

    public async Task AssignHolderAsync(string assetId, string userId, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(assetId, ct);
        if (a == null) return;

        var isUnassign = string.IsNullOrWhiteSpace(userId);
        a.HolderId = isUnassign ? null : userId;
        if (!isUnassign)
        {
            a.CheckedOutAt = DateTimeOffset.UtcNow;
        }
        else
        {
            a.CheckedOutAt = null;
        }
        
        await _repo.UpdateAsync(a, ct);
        
        var msg = isUnassign ? "unassigned" : $"assigned to {userId}";
        await _activity.PushAsync("assignment", $"{a.Tag ?? a.Serial} {msg} by {actorName}", ct);
    }

    public async Task IssueSpareAsync(string spareAssetId, string borrowerId, string reason, LoanDuration duration, string comments, string actorName, CancellationToken ct = default)
    {
        var a = await _repo.GetByIdAsync(spareAssetId, ct);
        if (a == null) return;

        if (a.Status != AssetStatus.Loaned) a.StatusChangedAt = DateTimeOffset.UtcNow;
        a.Status = AssetStatus.Loaned;
        
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
        
        await _repo.UpdateAsync(a, ct);
        await _activity.PushAsync("loan_in", $"Spare {a.Tag ?? a.Serial} returned by {loan.BorrowerId} (via {actorName})", ct);
    }

    public Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default) => _repo.GetSparePoolAsync(ct);
    public Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default) => _repo.GetActiveLoansAsync(ct);

    private static void Normalize(Asset asset)
    {
        asset.Tag = asset.Tag.Trim();
        asset.Serial = asset.Serial.Trim();
        asset.Model = asset.Model.Trim();
        asset.Imei = string.IsNullOrWhiteSpace(asset.Imei) ? null : asset.Imei.Trim();
        asset.Notes = asset.Notes.Trim();
        asset.Quantity = asset.Category == AssetCategory.Battery ? Math.Max(asset.Quantity, 0) : 1;
        asset.IsSAE = asset.Category == AssetCategory.SaeDevice;
        asset.Type = asset.Category switch
        {
            AssetCategory.Battery => "battery",
            AssetCategory.PodTc77 => "pod-tc77",
            _ => "sae",
        };
    }

    private static void Validate(Asset asset)
    {
        if (asset.Category == AssetCategory.PodTc77 && string.IsNullOrWhiteSpace(asset.Serial))
        {
            throw new InvalidOperationException("TC77 POD devices require a serial number.");
        }

        if (asset.Category == AssetCategory.Battery && asset.Quantity < 0)
        {
            throw new InvalidOperationException("Battery quantity cannot be negative.");
        }

        if (asset.Category != AssetCategory.Battery && string.IsNullOrWhiteSpace(asset.Tag) && string.IsNullOrWhiteSpace(asset.Serial))
        {
            throw new InvalidOperationException("Tracked devices need an asset tag or serial number.");
        }
    }

    private static bool ScanEquals(string? value, string normalizedScan)
        => string.Equals(NormalizeScanValue(value ?? ""), normalizedScan, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeScanValue(string value)
    {
        var cleaned = value.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return "";

        if (Uri.TryCreate(cleaned, UriKind.Absolute, out var uri))
        {
            var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var key in new[] { "asset", "tag", "serial", "sn", "imei", "id" })
            {
                var match = query.FirstOrDefault(part => part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
                if (match != null) return Uri.UnescapeDataString(match[(key.Length + 1)..]).Trim();
            }

            cleaned = uri.Segments.LastOrDefault()?.Trim('/') ?? cleaned;
        }

        foreach (var prefix in new[] { "asset:", "tag:", "serial:", "sn:", "imei:", "id:" })
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
