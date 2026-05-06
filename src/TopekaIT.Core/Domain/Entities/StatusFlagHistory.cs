using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class StatusFlagHistory
{
    public string Id { get; set; } = "";
    public string AssetId { get; set; } = "";
    public StatusFlags FlagChanged { get; set; }    // single flag that was toggled
    public bool WasSet { get; set; }                // true = flag added, false = flag removed
    public DateTimeOffset ChangedAt { get; set; }
    public string? ChangedBy { get; set; }          // UserId

    public Asset Asset { get; set; } = null!;
}
