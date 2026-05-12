using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TopekaIT.Core.Access;
using TopekaIT.Web.Authorization;
using TopekaIT.Web.Components.Pages.Admin;
using TopekaIT.Web.Components.Pages.IT;
using TopekaIT.Web.Components.Pages.Manager;
using TopekaIT.Web.Components.Pages.Worker;
using Xunit;

namespace TopekaIT.Web.Tests;

public class AuthorizationPolicyTests
{
    [Theory]
    [InlineData(AccessPermissionKeys.TicketsViewOwn)]
    [InlineData(AccessPermissionKeys.AssetsViewSupervisorConsole)]
    [InlineData(AccessPermissionKeys.PrintersViewAdmin)]
    [InlineData(AccessPermissionKeys.PrintersAutoSetup)]
    [InlineData(AccessPermissionKeys.AdminCreateDivisions)]
    public void PermissionPolicies_AreRegistered(string permissionKey)
    {
        var options = new AuthorizationOptions();
        AccessAuthorizationPolicies.AddPolicies(options);

        var policy = options.GetPolicy(permissionKey);

        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement =>
            requirement is PermissionRequirement permissionRequirement
            && permissionRequirement.PermissionKey == permissionKey);
    }

    [Theory]
    [InlineData(typeof(MyTickets), AccessPermissionKeys.TicketsViewOwn)]
    [InlineData(typeof(ManagerAssets), AccessPermissionKeys.AssetsViewSupervisorConsole)]
    [InlineData(typeof(PrinterAdmin), AccessPermissionKeys.PrintersViewAdmin)]
    [InlineData(typeof(ControlRoom), AccessPermissionKeys.ItDashboard)]
    [InlineData(typeof(DivisionAdmin), AccessPermissionKeys.AdminCreateDivisions)]
    public void RepresentativePages_UsePermissionPolicies(Type componentType, string permissionKey)
    {
        var attributes = componentType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>();

        Assert.Contains(attributes, attribute => attribute.Policy == permissionKey);
    }
}
