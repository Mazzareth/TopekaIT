namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A signed-in mobile reader session. The token is stored hashed; the Android app only receives the raw token once.
/// </summary>
public class MobileEquipmentSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string TokenHash { get; set; } = "";
    public string UserId { get; set; } = "";
    public string DivisionId { get; set; } = "";
    public string ReaderDeviceSerial { get; set; } = "";
    public string? Platform { get; set; }
    public string? AppVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(12);
    public DateTimeOffset? RevokedAt { get; set; }
}
