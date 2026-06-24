using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guards for the scan-first RMA handoff flow and its sidebar entry.
/// </summary>
public class RmaConsoleSourceTests
{
    [Fact]
    public void RmaConsole_OffersScanFirstDstHandoffFlow()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "IT", "RmaConsole.razor");

        Assert.Contains("@page \"/manager/rma\"", source);
        Assert.Contains("AssetsViewSupervisorConsole", source);
        Assert.Contains("RMA Device", source);
        Assert.Contains("AssetService.FindByScanAsync", source);
        Assert.Contains("Station.SendToDstRmaAsync", source);
        Assert.Contains("Give the device to local DST", source);
        Assert.DoesNotContain("RfidTagId", source);
        Assert.DoesNotContain("NFC/RFID", source);
    }

    [Fact]
    public void Sidebar_AddsRmaNavigationForManagers()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Layout", "Sidebar.razor");

        Assert.Contains("\"RMA Flow\"", source);
        Assert.Contains("\"/manager/rma\"", source);
        Assert.Contains("StationRoute", source);
        Assert.Contains("Uri.EscapeDataString(TenantContext.DivisionId)", source);
        Assert.Contains("\"/station/equipment/{Uri.EscapeDataString(TenantContext.DivisionId)}\"", source);
    }
}
