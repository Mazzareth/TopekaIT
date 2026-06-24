namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A small breadcrumb for the live activity feed. It is useful context, not the official audit trail.
/// </summary>
public class ActivityEvent
{
    public string Id { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string Kind { get; set; } = "";
    public string Text { get; set; } = "";
}
