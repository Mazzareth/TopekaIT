using Microsoft.AspNetCore.Authorization;

namespace TopekaIT.Web.Authorization;

/// <summary>
/// A named permission requirement. The handler does the real lookup so policies stay tiny.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permissionKey)
    {
        PermissionKey = permissionKey;
    }

    public string PermissionKey { get; }
}
