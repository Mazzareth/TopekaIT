using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class Asset
{
    public string Id { get; set; } = "";
    public AssetCategory Category { get; set; } = AssetCategory.SaeDevice;
    public string Type { get; set; } = "";
    public string Tag { get; set; } = "";
    public string Serial { get; set; } = "";
    public string? Imei { get; set; }
    public string Model { get; set; } = "";
    public int Quantity { get; set; } = 1;

    // Legacy status — kept for read compatibility during transition; use Flags going forward
    public AssetStatus Status { get; set; }
    public DateTimeOffset? StatusChangedAt { get; set; }

    // New multi-bit status
    public StatusFlags Flags { get; set; }

    public string? HolderId { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Notes { get; set; } = "";

    // SAE / Scanner fields
    public bool IsSAE { get; set; }
    public string? ScannerType { get; set; }        // legacy — migrated to Scanner asset records
    public ScannerKind? ScannerKind { get; set; }   // for Category == Scanner
    public string? PairedAssetId { get; set; }      // Scanner ↔ SAE pairing (FK to Asset.Id)

    // Location
    public string? LockerId { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string? LastSeenLocation { get; set; }   // free-text or locker number

    // Computed health (0-100); recalculated by service on significant events
    public int HealthScore { get; set; } = 100;

    public ICollection<RmaRecord> RmaRecords { get; set; } = new List<RmaRecord>();
    public ICollection<LoanRecord> LoanRecords { get; set; } = new List<LoanRecord>();
    public ICollection<AssetIssueTag> IssueTags { get; set; } = new List<AssetIssueTag>();
    public ICollection<StatusFlagHistory> FlagHistory { get; set; } = new List<StatusFlagHistory>();

    public Locker? Locker { get; set; }
    public Asset? PairedAsset { get; set; }
}
