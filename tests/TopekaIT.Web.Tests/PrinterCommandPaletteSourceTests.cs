using Xunit;

namespace TopekaIT.Web.Tests;

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
        File.ReadAllText(RepoPath(relativePath));

    private static string RepoPath(params string[] relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "6IA-IT-Portal.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not locate repository root.");
        }

        return Path.Combine(new[] { dir.FullName }.Concat(relativePath).ToArray());
    }
}
