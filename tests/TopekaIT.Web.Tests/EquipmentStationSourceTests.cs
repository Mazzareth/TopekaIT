using Xunit;

namespace TopekaIT.Web.Tests;

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
        Assert.Contains("Station.ValidateStationPinAsync(_pin, _divisionId)", source);
        Assert.Contains("_divisionId = employeeDivisionId", source);
        Assert.DoesNotContain("Swap / Replacement", source);
        Assert.DoesNotContain("Monthly Audit", source);
        Assert.DoesNotContain("Manager Assignment", source);
    }

    [Fact]
    public void StationPageShowsEmployeeDevicesAndOpenTickets()
    {
        var source = RepositorySource.Read("src", "TopekaIT.Web", "Components", "Pages", "Station", "EquipmentStation.razor");

        Assert.Contains("Devices currently out", source);
        Assert.Contains("Open tickets", source);
        Assert.Contains("AssetService.GetAllAsync()", source);
        Assert.Contains("TicketService.GetAllAsync()", source);
        Assert.Contains("TicketStatus.Resolved", source);
        Assert.Contains("ReportedById", source);
    }
}
