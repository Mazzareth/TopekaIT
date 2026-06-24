namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A grouped printer alert. Same root problem, multiple sightings, one row that humans can actually scan.
/// </summary>
public class PrinterAlertGroup
{
    public string AlertKey { get; set; } = "";
    public string AlertTitle { get; set; } = "";
    public string AlertCategory { get; set; } = "";
    public string? AlertDetail { get; set; }
    public string Severity { get; set; } = "Info";
    public int Count { get; set; }
    public DateTimeOffset LatestAt { get; set; }
    public List<PrinterAlertOccurrence> Occurrences { get; set; } = new();
}
