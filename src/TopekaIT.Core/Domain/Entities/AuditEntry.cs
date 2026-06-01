using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class AuditEntry
{
    public string Id { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string AssetId { get; set; } = "";
    public string? LockerId { get; set; }
    public string? ExpectedHolderId { get; set; }
    public string? ExpectedLockerId { get; set; }
    public string? ActualHolderId { get; set; }
    public string? ActualLockerId { get; set; }
    public string? ScanValue { get; set; }
    public AuditResult Result { get; set; } = AuditResult.Expected;
    public string? DiscrepancyReason { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public bool IsDiscrepancy { get; set; }
    public string? DiscrepancyNote { get; set; }

    public AuditSession Session { get; set; } = null!;
}
