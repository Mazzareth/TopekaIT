namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A user's active or historical relationship to a locker. We keep history instead of pretending assignments never changed.
/// </summary>
public class LockerOccupant
{
    public string LockerId { get; set; } = "";
    // Users live in the master database, so this remains an id reference rather than a navigation property.
    public string UserId { get; set; } = "";
    public bool IsPrimary { get; set; } = true;
    public DateTimeOffset AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public DateTimeOffset? UnassignedAt { get; set; }
    public string? UnassignedBy { get; set; }

    public Locker Locker { get; set; } = null!;
}
