using TopekaIT.Core.Domain.Entities;
using TopekaIT.Web.Services;
using Xunit;

namespace TopekaIT.Web.Tests;

/// <summary>
/// CSV export tests. Spreadsheet-safe output is boring until one quote breaks it.
/// </summary>
public class PrinterLogCsvTests
{
    [Fact]
    public void Build_EscapesQuotesAndPreservesRawMessage()
    {
        var logs = new[]
        {
            new PrinterLogEntry
            {
                PrinterName = "Dock \"A\"",
                Department = "Dry",
                IpAddress = "10.0.0.8",
                Timestamp = new DateTimeOffset(2026, 5, 14, 18, 30, 0, TimeSpan.Zero),
                EventType = "Error",
                Severity = "Critical",
                AlertTitle = "Gap",
                AlertDetail = "Not detected",
                FriendlyMessage = "Check label gap",
                RawMessage = "Raw \"device\" payload",
            },
        };

        var csv = PrinterLogCsv.Build(logs);

        Assert.Contains("\"Date/Time Local\",\"Date/Time UTC\",\"Printer\"", csv);
        Assert.Contains("\"Dock \"\"A\"\"\"", csv);
        Assert.Contains("\"Gap: Not detected\"", csv);
        Assert.Contains("\"Check label gap\"", csv);
        Assert.Contains("\"Raw \"\"device\"\" payload\"", csv);
    }

    [Fact]
    public void BuildForPrinter_UsesRawMessageWhenFriendlyMessageIsMissing()
    {
        var printer = new Printer
        {
            Name = "Freezer Printer",
            Department = "Freezer",
            IpAddress = "10.0.0.9",
        };
        var events = new[]
        {
            new PrinterEvent
            {
                Timestamp = new DateTimeOffset(2026, 5, 14, 19, 0, 0, TimeSpan.Zero),
                EventType = "Warning",
                Severity = "Warning",
                RawMessage = "Gap not detected",
            },
        };

        var csv = PrinterLogCsv.BuildForPrinter(printer, events);

        Assert.Contains("\"Freezer Printer\"", csv);
        Assert.Contains("\"Gap not detected\"", csv);
    }
}
