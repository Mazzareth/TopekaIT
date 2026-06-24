using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guards for locker manager details that are easy to regress in Razor.
/// </summary>
public class LockerConsoleSourceTests
{
    [Fact]
    public void LockerConsole_RendersLockerRfidControls()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "Manager", "LockerConsole.razor");

        Assert.Contains("Locker RFID", source);
        Assert.Contains("LockerService.GenerateRfidLinkAsync", source);
        Assert.Contains("LockerService.LinkRfidAsync", source);
        Assert.Contains("LockerService.ClearRfidLinkAsync", source);
        Assert.Contains("BuildRfidPayload", source);
        Assert.Contains("RfidTagId", source);
        Assert.Contains("RfidLinkedAt", source);
        Assert.Contains("_canEditLockers", source);
        Assert.Contains("Link Locker Tag", source);
        Assert.Contains("Clear Tag", source);
    }

    [Fact]
    public void ManagerAssets_RendersLockerDerivedAssignment()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "Manager", "ManagerAssets.razor");

        Assert.Contains("Assigned To", source);
        Assert.Contains("Via Locker #", source);
        Assert.Contains("LockerForAsset", source);
        Assert.Contains("ActiveOccupantUser(locker)", source);
        Assert.Contains("Search tag, serial, holder, locker", source);
    }
}
