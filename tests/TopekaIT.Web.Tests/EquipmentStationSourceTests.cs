using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guards for the kiosk page. The big idea is employee PIN first, then assigned-device confirmation or RMA help.
/// </summary>
public class EquipmentStationSourceTests
{
    [Fact]
    public void StationPagePersistsDivisionAndKeepsKioskInputsFocused()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "Station", "EquipmentStation.razor");

        Assert.Contains("topekaStationDivision.get", source);
        Assert.Contains("topekaStationDivision.set", source);
        Assert.Contains("<h3>PIN</h3>", source);
        Assert.Contains("<h3>Device Scan</h3>", source);
        Assert.Contains("@page \"/station/equipment/{DivisionCode}\"", source);
        Assert.Contains("Station.ValidateStationPinAsync(_pin, _divisionId, allowCrossDivisionFallback: !_divisionLocked)", source);
        Assert.Contains("_divisionId = employeeDivisionId", source);
        Assert.Contains("Locked to this station URL", source);
        Assert.Contains("accountability cadence", source);
        Assert.Contains("Confirm I have this", source);
        Assert.Contains("This device is broken", source);
        Assert.Contains("Station.ConfirmAssignmentAsync", source);
        Assert.Contains("Station.SendToDstRmaAsync", source);
        Assert.DoesNotContain("Check out to @_employee.Name", source);
    }

    [Fact]
    public void StationPageShowsEmployeeDevicesAndOpenTickets()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "Station", "EquipmentStation.razor");

        Assert.Contains("Assigned devices", source);
        Assert.Contains("Open tickets", source);
        Assert.Contains("DueCheckInCount", source);
        Assert.Contains("CheckInStatusLine", source);
        Assert.Contains("AssetService.GetAllAsync()", source);
        Assert.Contains("TicketService.GetAllAsync()", source);
        Assert.Contains("TicketStatus.Resolved", source);
        Assert.Contains("ReportedById", source);
    }
}
