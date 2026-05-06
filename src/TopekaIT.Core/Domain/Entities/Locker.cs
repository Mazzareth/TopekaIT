namespace TopekaIT.Core.Domain.Entities;

public class Locker
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";       // e.g. "A-07", "12"
    public string? Section { get; set; }            // e.g. "Floor A", "Charging Bay"
    public string? LockCombo { get; set; }
    public string? LockSerial { get; set; }
    public string? Notes { get; set; }
    public bool IsShared { get; set; }
    public bool IsActive { get; set; } = true;
    public int? AuditCadenceDays { get; set; }      // null = never auto-flag
    public DateTimeOffset? LastAuditedAt { get; set; }
    public string? LastAuditedBy { get; set; }      // UserId (cross-DB reference)

    public ICollection<LockerOccupant> Occupants { get; set; } = new List<LockerOccupant>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
