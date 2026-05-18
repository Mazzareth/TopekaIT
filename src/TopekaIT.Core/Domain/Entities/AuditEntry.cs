namespace TopekaIT.Core.Domain.Entities;

public class AuditEntry
{
    public string Id { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string AssetId { get; set; } = "";
    public string? LockerId { get; set; }
    public DateTimeOffset ScannedAt { get; set; }
    public bool IsDiscrepancy { get; set; }
    public string? DiscrepancyNote { get; set; }

    public AuditSession Session { get; set; } = null!;
}
