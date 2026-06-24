namespace TopekaIT.AvaloniaShell;

public sealed record ShellLoginResponse(
    ShellUser User,
    bool RequiresPasswordChange,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<ShellPermissionGroup> PermissionGroups);

public sealed record ShellUser(
    string Id,
    string Username,
    string Name,
    string Avatar,
    string Tier,
    string TierLabel,
    string? DivisionId);

public sealed record ShellPermissionGroup(
    string Name,
    IReadOnlyList<ShellPermission> Permissions);

public sealed record ShellPermission(
    string Key,
    string Label,
    string Group,
    string DefaultTier,
    string DefaultTierLabel,
    string GrantableTier,
    string GrantableTierLabel,
    bool IsAllowed);

public sealed record ShellSession(
    ShellUser User,
    IReadOnlySet<string> Permissions,
    IReadOnlyList<ShellPermissionGroup> PermissionGroups)
{
    public static ShellSession FromLoginResponse(ShellLoginResponse response)
    {
        return new ShellSession(
            response.User,
            response.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase),
            response.PermissionGroups);
    }

    public bool Has(string permissionKey) => Permissions.Contains(permissionKey);
}
