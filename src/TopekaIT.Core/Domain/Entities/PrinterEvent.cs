namespace TopekaIT.Core.Domain.Entities;

public class PrinterEvent
{
    public long Id { get; set; }
    public string PrinterId { get; set; } = "";
    public Printer Printer { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = "";   // JobStarted | Error | Warning | Info
    public string RawMessage { get; set; } = "";
    public string? Severity { get; set; }          // Info | Warning | Error
    public string? AlertKey { get; set; }
    public string? AlertTitle { get; set; }
    public string? AlertCategory { get; set; }
    public string? AlertDetail { get; set; }
    public string? FriendlyMessage { get; set; }
    public int? AlertTrainingLevel { get; set; }
}
