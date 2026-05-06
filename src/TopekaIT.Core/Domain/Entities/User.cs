using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class User
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public UserRole Role { get; set; }
    public string Avatar { get; set; } = "";
    
    // New fields for Locker Management
    public string? Position { get; set; }
    public string? LockerNumber { get; set; }
    public string? LockerCombo { get; set; }
    public string? LockSerialNumber { get; set; }
    public bool Audit { get; set; }
    public string? DivisionId { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }

    // Leave of Absence
    public bool IsOnLOA { get; set; }
    public DateTimeOffset? OnLOASince { get; set; }
    public string? OnLOAReason { get; set; }
}
