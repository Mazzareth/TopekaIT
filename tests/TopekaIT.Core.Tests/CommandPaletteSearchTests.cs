using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class CommandPaletteSearchTests
{
    [Fact]
    public void Score_MatchesPluralWorkersAgainstWorkerRoleAndDivision()
    {
        var worker = CommandPaletteSearch.Score(
            "6I workers",
            "Maya Rivera",
            "PEOPLE",
            "maya.rivera - Worker - 6I-A",
            "Usr");

        var supervisor = CommandPaletteSearch.Score(
            "6I workers",
            "Avery Chen",
            "PEOPLE",
            "avery.chen - Supervisor - 6I-A",
            "Usr");

        Assert.True(worker > 0);
        Assert.Equal(0, supervisor);
    }

    [Fact]
    public void Score_PrefersPrinterResultOverDivisionWhenQueryMentionsPrinters()
    {
        var printer = CommandPaletteSearch.Score(
            "6I printers",
            "Freezer Printer 1",
            "PRINTERS",
            "10.36.155.24 - 6I-A - Healthy",
            "Prn");

        var division = CommandPaletteSearch.Score(
            "6I printers",
            "Topeka",
            "DIVISIONS",
            "6I-A - printer code 1234",
            "Div");

        Assert.True(printer > division);
    }

    [Fact]
    public void Score_RequiresEveryQueryToken()
    {
        var score = CommandPaletteSearch.Score(
            "6I workers",
            "Freezer Printer 1",
            "PRINTERS",
            "10.36.155.24 - 6I-A - Healthy",
            "Prn");

        Assert.Equal(0, score);
    }
}
