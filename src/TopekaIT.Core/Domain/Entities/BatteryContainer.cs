namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A named place that holds loose batteries. This is inventory math, not serialized-device tracking.
/// </summary>
public class BatteryContainer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Location { get; set; }
    public int Capacity { get; set; }
    public int CurrentCount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
