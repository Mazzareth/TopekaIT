namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// One printer ping sample. Small rows, many rows, useful when the printer says "I was fine" and the chart says otherwise.
/// </summary>
public class PingSample
{
    public long Id { get; set; }
    public string PrinterId { get; set; } = "";
    public Printer Printer { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public bool Success { get; set; }
    public int? LatencyMs { get; set; }
    public string? FailureReason { get; set; }
}
