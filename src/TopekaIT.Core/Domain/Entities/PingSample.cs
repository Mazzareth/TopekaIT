namespace TopekaIT.Core.Domain.Entities;

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
