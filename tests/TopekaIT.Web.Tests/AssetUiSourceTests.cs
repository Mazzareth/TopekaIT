using Xunit;

namespace TopekaIT.Web.Tests;

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
    public void AssetConsole_DoesNotDisplayUncomputedHealthScore()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Pages", "Shared", "AssetConsole.razor");

        Assert.DoesNotContain("Health Score", source);
        Assert.DoesNotContain("Health</div>", source);
        Assert.DoesNotContain("HealthScoreCss", source);
        Assert.DoesNotContain(".HealthScore", source);
    }

    [Theory]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Shared", "AssetConsole.razor")]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "Manager", "ManagerAssets.razor")]
    public void AssetTables_ConstrainModelColumnWidth(params string[] relativePath)
    {
        var source = ReadRepoFile(relativePath);

        Assert.Contains("minmax(110px, .55fr)", source);
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
