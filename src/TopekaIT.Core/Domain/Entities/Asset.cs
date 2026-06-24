using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// One tracked piece of equipment, from SAE devices to scanners and batteries. Most station and locker flows orbit this object.
/// </summary>
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

    // Kept for read compatibility during the transition to Flags.
    public AssetStatus Status { get; set; }
    public DateTimeOffset? StatusChangedAt { get; set; }

    public StatusFlags Flags { get; set; }

    public string? HolderId { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Notes { get; set; } = "";

    public bool IsSAE { get; set; }
    // Legacy value retained for older scanner records migrated before category-specific assets.
    public string? ScannerType { get; set; }
    public ScannerKind? ScannerKind { get; set; }
    // Scanner-to-SAE pairing uses Asset.Id because both sides live in the same aggregate.
    public string? PairedAssetId { get; set; }

    public string? LockerId { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public string? LastSeenLocation { get; set; }

    public int HealthScore { get; set; } = 100;

    public ICollection<RmaRecord> RmaRecords { get; set; } = new List<RmaRecord>();
    public ICollection<LoanRecord> LoanRecords { get; set; } = new List<LoanRecord>();
    public ICollection<AssetIssueTag> IssueTags { get; set; } = new List<AssetIssueTag>();
    public ICollection<StatusFlagHistory> FlagHistory { get; set; } = new List<StatusFlagHistory>();

    public Locker? Locker { get; set; }
    public Asset? PairedAsset { get; set; }
}
