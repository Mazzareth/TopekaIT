using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class UserPermissionOverride
{
    public string UserId { get; set; } = "";
    public string PermissionKey { get; set; } = "";
    public PermissionOverrideState State { get; set; }
    public string UpdatedById { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }

    public User? User { get; set; }
    public User? UpdatedBy { get; set; }
}
