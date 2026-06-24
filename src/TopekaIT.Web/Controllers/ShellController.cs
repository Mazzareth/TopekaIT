using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopekaIT.Core.Access;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Controllers;

[ApiController]
[Route("api/shell")]
public sealed class ShellController : ControllerBase
{
    private readonly UserService _users;
    private readonly AccessControlService _access;

    public ShellController(UserService users, AccessControlService access)
    {
        _users = users;
        _access = access;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ShellLoginResponse>> Login(
        [FromBody] ShellLoginRequest request,
        CancellationToken ct)
    {
        var user = await _users.ValidateCredentialsAsync(request.Username, request.Password, ct);
        if (user == null)
        {
            return Unauthorized(new ShellLoginError("Invalid username or password."));
        }

        await _users.MarkActiveAsync(user.Id, DateTimeOffset.UtcNow, ct);

        var resolvedAccess = await _access.GetUserAccessAsync(user.Id, ct);
        var permissions = resolvedAccess?.Permissions.OrderBy(permission => permission, StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();
        var permissionSet = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var permissionGroups = ShellPermissionGroup.FromCatalog(permissionSet);

        return Ok(new ShellLoginResponse(
            User: new ShellUser(
                user.Id,
                user.Username,
                user.Name,
                user.Avatar,
                user.Role.ToString(),
                user.Role.DisplayName(),
                user.DivisionId),
            RequiresPasswordChange: user.MustChangePassword,
            Permissions: permissions,
            PermissionGroups: permissionGroups));
    }
}

public sealed record ShellLoginRequest(string Username, string Password);

public sealed record ShellLoginError(string Message);

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
    IReadOnlyList<ShellPermission> Permissions)
{
    public static IReadOnlyList<ShellPermissionGroup> FromCatalog(IReadOnlySet<string> effectivePermissions)
    {
        return AccessCatalog.Permissions
            .GroupBy(permission => permission.Group)
            .OrderBy(group => group.Key)
            .Select(group => new ShellPermissionGroup(
                group.Key,
                group
                    .OrderBy(permission => permission.DisplayName)
                    .Select(permission => new ShellPermission(
                        permission.Key,
                        permission.DisplayName,
                        permission.Group,
                        permission.DefaultTier.ToString(),
                        permission.DefaultTier.DisplayName(),
                        permission.GrantableTier.ToString(),
                        permission.GrantableTier.DisplayName(),
                        effectivePermissions.Contains(permission.Key)))
                    .ToList()))
            .ToList();
    }
}

public sealed record ShellPermission(
    string Key,
    string Label,
    string Group,
    string DefaultTier,
    string DefaultTierLabel,
    string GrantableTier,
    string GrantableTierLabel,
    bool IsAllowed);
