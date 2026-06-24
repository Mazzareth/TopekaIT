using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// History for a single asset flag change. Useful when the current state is less interesting than how it got there.
/// </summary>
public class StatusFlagHistory
{
    public string Id { get; set; } = "";
    public string AssetId { get; set; } = "";
    public StatusFlags FlagChanged { get; set; }
    public bool WasSet { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? ChangedBy { get; set; }

    public Asset Asset { get; set; } = null!;
}
