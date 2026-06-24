namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// One Lantronix poll result, successful or not. The raw response stays around because field devices enjoy being weird.
/// </summary>
public class LantronixPollSample
{
    public long Id { get; set; }
    public string DeviceId { get; set; } = "";
    public LantronixDevice Device { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public bool Success { get; set; }
    public int? LatencyMs { get; set; }
    public string? FailureReason { get; set; }
    public string? ReportName { get; set; }
    public int? TankNumber { get; set; }
    public string? Product { get; set; }
    public decimal? Volume { get; set; }
    public decimal? TcVolume { get; set; }
    public decimal? Ullage { get; set; }
    public decimal? Height { get; set; }
    public decimal? Water { get; set; }
    public decimal? Temperature { get; set; }
    public string? RawResponse { get; set; }
}
