namespace TopekaIT.Core.Domain.Entities;

public class AuditEntry
{
    public string Id { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string AssetId { get; set; } = "";
    public string? LockerId { get; set; }           // locker where asset was found (if applicable)
    public DateTimeOffset ScannedAt { get; set; }
    public bool IsDiscrepancy { get; set; }
    public string? DiscrepancyNote { get; set; }    // e.g. "Expected InLocker, found WithHolder"

    public AuditSession Session { get; set; } = null!;
}
