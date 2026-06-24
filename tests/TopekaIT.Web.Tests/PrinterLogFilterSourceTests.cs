using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// Source guard for printer log filters and export query wiring.
/// </summary>
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
        RepositorySource.Read(relativePath);
}
