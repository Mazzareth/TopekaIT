using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Authorization;

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
