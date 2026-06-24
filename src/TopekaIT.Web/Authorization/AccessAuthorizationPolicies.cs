using Microsoft.AspNetCore.Authorization;
using TopekaIT.Core.Access;

namespace TopekaIT.Web.Authorization;

/// <summary>
/// Bridges the Core permission catalog into ASP.NET policies. One catalog entry, one policy name.
/// </summary>
public static class AccessAuthorizationPolicies
{
    public static void AddPolicies(AuthorizationOptions options)
    {
        foreach (var permission in AccessCatalog.Permissions)
        {
            options.AddPolicy(
                permission.Key,
                policy => policy
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission.Key)));
        }
    }
}
