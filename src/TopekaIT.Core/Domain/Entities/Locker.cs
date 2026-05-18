namespace TopekaIT.Core.Domain.Entities;

public class Locker
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";
    public string? Section { get; set; }
    public string? LockCombo { get; set; }
    public string? LockSerial { get; set; }
    public string? Notes { get; set; }
    public bool IsShared { get; set; }
    public bool IsActive { get; set; } = true;
    public int? AuditCadenceDays { get; set; }
    public DateTimeOffset? LastAuditedAt { get; set; }
    // Users live in the master database, so this remains an id reference rather than a navigation property.
    public string? LastAuditedBy { get; set; }

    public ICollection<LockerOccupant> Occupants { get; set; } = new List<LockerOccupant>();
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
}
