namespace TopekaIT.Core.Domain.Entities;

public class ActivityEvent
{
    public string Id { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public string Kind { get; set; } = "";
    public string Text { get; set; } = "";
}
