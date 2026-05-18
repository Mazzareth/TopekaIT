using System.Text;
using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Web.Services;

public static class PrinterLogCsv
{
    public static string Build(IEnumerable<PrinterLogEntry> entries)
    {
        var builder = CreateHeader();

        foreach (var entry in entries)
        {
            AppendRow(
                builder,
                entry.Timestamp,
                entry.PrinterName,
                entry.Department,
                entry.IpAddress,
                entry.EventType,
                entry.Severity,
                AlertText(entry.AlertTitle, entry.AlertDetail),
                DisplayMessage(entry.FriendlyMessage, entry.RawMessage),
                entry.RawMessage);
        }

        return builder.ToString();
    }

    public static string BuildForPrinter(Printer printer, IEnumerable<PrinterEvent> events)
    {
        var builder = CreateHeader();

        foreach (var ev in events)
        {
            AppendRow(
                builder,
                ev.Timestamp,
                printer.Name,
                printer.Department,
                printer.IpAddress,
                ev.EventType,
                ev.Severity,
                AlertText(ev.AlertTitle, ev.AlertDetail),
                DisplayMessage(ev.FriendlyMessage, ev.RawMessage),
                ev.RawMessage);
        }

        return builder.ToString();
    }

    private static StringBuilder CreateHeader()
    {
        var builder = new StringBuilder();
        AppendCsvLine(
            builder,
            "Date/Time Local",
            "Date/Time UTC",
            "Printer",
            "Department",
            "IP Address",
            "Event Type",
            "Severity",
            "Alert",
            "Message",
            "Raw Message");

        return builder;
    }

    private static void AppendRow(
        StringBuilder builder,
        DateTimeOffset timestamp,
        string printerName,
        string department,
        string ipAddress,
        string eventType,
        string? severity,
        string alert,
        string message,
        string rawMessage)
    {
        AppendCsvLine(
            builder,
            timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            timestamp.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            printerName,
            department,
            ipAddress,
            eventType,
            severity ?? "",
            alert,
            message,
            rawMessage);
    }

    private static string AlertText(string? title, string? detail)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return detail ?? "";
        }

        return string.IsNullOrWhiteSpace(detail)
            ? title
            : $"{title}: {detail}";
    }

    private static string DisplayMessage(string? friendlyMessage, string rawMessage) =>
        string.IsNullOrWhiteSpace(friendlyMessage) ? rawMessage : friendlyMessage;

    private static void AppendCsvLine(StringBuilder builder, params string?[] values)
    {
        builder.AppendLine(string.Join(",", values.Select(Escape)));
    }

    private static string Escape(string? value)
    {
        value ??= "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
