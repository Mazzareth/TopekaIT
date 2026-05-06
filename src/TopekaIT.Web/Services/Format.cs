using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Web.Services;

public static class Format
{
    public static string RelTime(DateTimeOffset ts)
    {
        var diff = DateTimeOffset.UtcNow - ts;
        var abs = diff.Duration();
        var sign = diff.Ticks < 0 ? "in " : "";
        var suffix = diff.Ticks < 0 ? "" : " ago";
        var minutes = (int)Math.Round(abs.TotalMinutes);
        if (minutes < 1) return "just now";
        if (minutes < 60) return $"{sign}{minutes}m{suffix}";
        var hours = (int)Math.Round(abs.TotalHours);
        if (hours < 24) return $"{sign}{hours}h{suffix}";
        var days = (int)Math.Round(abs.TotalDays);
        if (days < 30) return $"{sign}{days}d{suffix}";
        return ts.LocalDateTime.ToString("yyyy-MM-dd");
    }

    public static string ShortDate(DateTimeOffset? ts)
        => ts.HasValue ? ts.Value.LocalDateTime.ToString("MMM d") : "—";

    public static string StatusLabel(TicketStatus s) => s switch
    {
        TicketStatus.Open => "Open",
        TicketStatus.InProgress => "In Progress",
        TicketStatus.OnHold => "On Hold",
        TicketStatus.Resolved => "Resolved",
        _ => s.ToString(),
    };

    public static string StatusPill(TicketStatus s) => s switch
    {
        TicketStatus.Open => "pill-info",
        TicketStatus.InProgress => "pill-warn",
        TicketStatus.OnHold => "pill-dim",
        TicketStatus.Resolved => "pill-up",
        _ => "pill-dim",
    };

    public static string PrinterLabel(PrinterStatus s) => s switch
    {
        PrinterStatus.Up => "Online",
        PrinterStatus.Warn => "Warning",
        PrinterStatus.Down => "Offline",
        _ => s.ToString(),
    };

    public static string PrinterPill(PrinterStatus s) => s switch
    {
        PrinterStatus.Up => "pill-up",
        PrinterStatus.Warn => "pill-warn",
        PrinterStatus.Down => "pill-down",
        _ => "pill-dim",
    };

    public static string PrinterClass(PrinterStatus s) => s.ToString().ToLowerInvariant();

    public static string PriorityLabel(TicketPriority p) => p switch
    {
        TicketPriority.Low => "Low",
        TicketPriority.Med => "Medium",
        TicketPriority.High => "High",
        TicketPriority.Urgent => "Urgent",
        _ => p.ToString(),
    };

    public static string PriorityPill(TicketPriority p) => p switch
    {
        TicketPriority.Low => "pill-dim",
        TicketPriority.Med => "pill-info",
        TicketPriority.High => "pill-warn",
        TicketPriority.Urgent => "pill-down",
        _ => "pill-dim",
    };
}
