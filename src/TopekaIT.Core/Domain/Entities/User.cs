using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A portal user. This carries normal login data, station PIN data, and the tier that starts their access.
/// </summary>
public class User
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";
    public int PasswordIterations { get; set; } = 100_000;
    public bool MustChangePassword { get; set; }
    public string? StationPinHash { get; set; }
    public string? StationPinSalt { get; set; }
    public int StationPinIterations { get; set; }
    public AccessTier Role { get; set; }
    public string Avatar { get; set; } = "";

    public string? Position { get; set; }
    public string? LockerNumber { get; set; }
    public string? LockerCombo { get; set; }
    public string? LockSerialNumber { get; set; }
    public bool Audit { get; set; }
    public string? DivisionId { get; set; }
    public DateTimeOffset? LastActiveAt { get; set; }

    public bool IsOnLOA { get; set; }
    public DateTimeOffset? OnLOASince { get; set; }
    public string? OnLOAReason { get; set; }
}
