namespace TopekaIT.Core.Domain.Entities;

public class PrinterActiveIncidentReportRow
{
    public string DivisionId { get; set; } = "";
    public string DivisionName { get; set; } = "";
    public string PrinterId { get; set; } = "";
    public string PrinterName { get; set; } = "";
    public string Department { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string AlertKey { get; set; } = "";
    public string AlertTitle { get; set; } = "";
    public string AlertCategory { get; set; } = "";
    public string? AlertDetail { get; set; }
    public string? FriendlyMessage { get; set; }
    public string Severity { get; set; } = "Info";
    public int? TrainingLevel { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public long LastEventId { get; set; }
    public int OccurrenceCount { get; set; }
}
