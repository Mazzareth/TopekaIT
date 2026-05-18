namespace TopekaIT.Core.Domain.Entities;

public class PrinterLogEntry
{
    public long Id { get; set; }
    public string PrinterId { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public string Department { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = "";
    public string RawMessage { get; set; } = "";
    public string? Severity { get; set; }
    public string? AlertKey { get; set; }
    public string? AlertTitle { get; set; }
    public string? AlertCategory { get; set; }
    public string? AlertDetail { get; set; }
    public string? FriendlyMessage { get; set; }
    public int? AlertTrainingLevel { get; set; }
}
