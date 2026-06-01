namespace TopekaIT.Core.Domain.Entities;

public class AuditSession
{
    public string Id { get; set; } = "";
    public string DivisionId { get; set; } = "";
    public string ConductedBy { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public int TotalScanned { get; set; }
    public int Discrepancies { get; set; }
    public int MissingCount { get; set; }
    public int UnexpectedCount { get; set; }

    public ICollection<AuditEntry> Entries { get; set; } = new List<AuditEntry>();
}
