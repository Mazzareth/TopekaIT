namespace TopekaIT.Core.Domain.Entities;

public class LockerOccupant
{
    public string LockerId { get; set; } = "";
    public string UserId { get; set; } = "";        // cross-DB reference to MasterDbContext.Users
    public bool IsPrimary { get; set; } = true;
    public DateTimeOffset AssignedAt { get; set; }
    public string? AssignedBy { get; set; }         // UserId
    public DateTimeOffset? UnassignedAt { get; set; }  // null = still active
    public string? UnassignedBy { get; set; }

    public Locker Locker { get; set; } = null!;
}
