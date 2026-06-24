namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// The current open-alert memory for a printer. New logs update this instead of creating a fresh incident every time.
/// </summary>
public class PrinterAlertState
{
    public long Id { get; set; }
    public string PrinterId { get; set; } = "";
    public Printer Printer { get; set; } = null!;
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
    public bool BlipSuppressed { get; set; }
}
