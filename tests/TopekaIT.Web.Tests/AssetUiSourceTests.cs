using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guards for asset UI choices that are easy to regress in Razor without a browser test.
/// </summary>
public class AssetUiSourceTests
{
    [Fact]
    public void AssetDetail_DoesNotRenderPlaceholderQrCode()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Shared", "AssetDetail.razor");
        var qrComponent = RepoPath("src", "TopekaIT.Web", "Components", "Shared", "QRCode.razor");

        Assert.DoesNotContain("QRCode", source);
        Assert.DoesNotContain("ScanCode", source);
        Assert.False(File.Exists(qrComponent));
    }

    [Fact]
    public void AssetDetail_RendersDeviceAttachedRmaHistory()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Shared", "AssetDetail.razor");

        Assert.Contains("RMA History", source);
        Assert.Contains("No RMA trips recorded for this device.", source);
        Assert.Contains("Sent to RMA", source);
        Assert.Contains("Returned from RMA", source);
        Assert.Contains("RmaStatusLabel", source);
        Assert.Contains("RmaTrackUrl", source);
    }

    [Fact]
    public void AssetDetail_IssuesAssignedDevicesThroughStationLedger()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Shared", "AssetDetail.razor");

        Assert.Contains("Station.AssignByManagerAsync", source);
        Assert.Contains("new EquipmentStationRequest", source);
        Assert.Contains("_canAssignAssets", source);
        Assert.Contains("Issue device to employee", source);
    }

    [Theory]
    [InlineData("src", "TopekaIT.Web", "Components", "Shared", "AssetDetail.razor")]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Shared", "AssetConsole.razor")]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Manager", "ManagerAssets.razor")]
    public void AssetSurfaces_DoNotRenderDeviceRfidControls(params string[] relativePath)
    {
        var source = ReadRepoFile(relativePath);

        Assert.DoesNotContain("RfidTagId", source);
        Assert.DoesNotContain("RfidScanModal", source);
        Assert.DoesNotContain("RfidTagModal", source);
        Assert.DoesNotContain("RFID NTAG213", source);
    }

    [Fact]
    public void AssetConsole_DoesNotDisplayUncomputedHealthScore()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Pages", "Shared", "AssetConsole.razor");

        Assert.DoesNotContain("Health Score", source);
        Assert.DoesNotContain("Health</div>", source);
        Assert.DoesNotContain(".HealthScore", source);
    }

    [Theory]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Shared", "AssetConsole.razor.css")]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Manager", "ManagerAssets.razor.css")]
    public void AssetTables_ConstrainModelColumnWidth(params string[] relativePath)
    {
        var source = ReadRepoFile(relativePath);

        Assert.Contains("minmax(90px, 0.4fr)", source);
    }

    private static string ReadRepoFile(params string[] relativePath) =>
        RepositorySource.Read(relativePath);

    private static string RepoPath(params string[] relativePath)
        => RepositorySource.Path(relativePath);
}
