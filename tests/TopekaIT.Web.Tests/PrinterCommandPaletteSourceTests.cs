using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guards for the printer command palette, especially the telnet safety gates.
/// </summary>
public class PrinterCommandPaletteSourceTests
{
    [Fact]
    public void PrinterCommandPalette_UsesTelnetCatalogAndAdminTierGate()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Shared", "PrinterCommandPalette.razor");

        Assert.Contains("IPrinterSetupTelnetClient", source);
        Assert.Contains("PrintNetCommandCatalog", source);
        Assert.Contains("access?.Tier >= AccessTier.Admin", source);
        Assert.Contains("PrintNetCommandAccess.Excluded", source);
        Assert.Contains("Protected writes", source);
    }

    [Fact]
    public void PrinterCommandPalette_ExposesPrinterNameQuickAction()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Shared", "PrinterCommandPalette.razor");

        Assert.Contains("Quick actions", source);
        Assert.Contains("Get and Set Printer Name", source);
        Assert.Contains("PrinterSetupService.ParseSysInfoValue(output, \"Description\")", source);
        Assert.Contains("await Printers.UpdateAsync(printer)", source);
    }

    [Fact]
    public void MainLayout_RendersPrinterCommandPalette()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Layout", "MainLayout.razor");

        Assert.Contains("<PrinterCommandPalette />", source);
    }

    private static string ReadRepoFile(params string[] relativePath) =>
        RepositorySource.Read(relativePath);
}
