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
    public AssetStatus Status { get; set; }
    public DateTimeOffset? StatusChangedAt { get; set; }
    public string? HolderId { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Notes { get; set; } = "";
    
    // New fields for SAE and Scanner types
    public string? ScannerType { get; set; } // e.g. 2D, BAR
    public bool IsSAE { get; set; }

    public ICollection<RmaRecord> RmaRecords { get; set; } = new List<RmaRecord>();
    public ICollection<LoanRecord> LoanRecords { get; set; } = new List<LoanRecord>();
}
