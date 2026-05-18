using System.Runtime.CompilerServices;
using Xunit;

namespace TopekaIT.Web.Tests;

public class PrinterLogFilterSourceTests
{
    [Theory]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "IT", "PrinterAdmin.razor")]
    [InlineData("src", "TopekaIT.Web", "Components", "Pages", "IT", "PrinterDetail.razor")]
    public void PrinterLogDateFilters_UseSeparateDateAndTimeInputs(params string[] relativePath)
    {
        var source = ReadRepoFile(relativePath);

        Assert.DoesNotContain("datetime-local", source);
        Assert.Contains("type=\"date\"", source);
        Assert.Contains("type=\"time\"", source);
    }

    [Fact]
    public void PrinterDetail_PausesRefreshWhileLogFilterIsFocused()
    {
        var source = ReadRepoFile("src", "TopekaIT.Web", "Components", "Pages", "IT", "PrinterDetail.razor");

        Assert.Contains("@onfocusin=\"BeginLogFilterEdit\"", source);
        Assert.Contains("if (_logFilterEditing) return;", source);
    }

    private static string ReadRepoFile(params string[] relativePath) =>
        File.ReadAllText(RepoPath(relativePath, SourceFile()));

    private static string SourceFile([CallerFilePath] string sourceFile = "") => sourceFile;

    private static string RepoPath(string[] relativePath, string sourceFile)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceFile) ?? "";
        foreach (var start in new[] { sourceDirectory, Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "6IA-IT-Portal.slnx")))
            {
                dir = dir.Parent;
            }

            if (dir != null)
            {
                return Path.Combine(new[] { dir.FullName }.Concat(relativePath).ToArray());
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
