using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Authorization;

/// <summary>
/// Last mile of permission checks: pull the signed-in user id, ask Core for effective access, and approve only that permission.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly AccessControlService _access;

    public PermissionAuthorizationHandler(AccessControlService access)
    {
        _access = access;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;

        if (await _access.HasPermissionAsync(userId, requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
    }
}
