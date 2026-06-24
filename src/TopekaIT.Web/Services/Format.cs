using System.Globalization;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Web.Services;

/// <summary>
/// Display formatting helpers. Keep labels and CSS class decisions here so pages do not each invent their own wording.
/// </summary>
public static class Format
{
    public static string SimpleState(StatusFlags f)
    {
        if (f.HasFlag(StatusFlags.Missing))            return "Missing";
        if (f.HasFlag(StatusFlags.InRMA))              return "RMA";
        if (f.HasFlag(StatusFlags.InRepair))           return "Repair";
        if (f.HasFlag(StatusFlags.OnHold))             return "On Hold";
        if (f.HasFlag(StatusFlags.UnderInvestigation)) return "Investigation";
        if (f.HasFlag(StatusFlags.OnLoan))             return f.HasFlag(StatusFlags.DayLoan) ? "Day Loan" : "Loaned";
        if (f.HasFlag(StatusFlags.WithHolder))         return "In Use";
        if (f.HasFlag(StatusFlags.Spare))              return "Spare";
        return "Available";
    }

    public static string SimpleStateCss(StatusFlags f)
    {
        if (f.HasFlag(StatusFlags.Missing))            return "state-pill state-missing";
        if (f.HasFlag(StatusFlags.InRMA) || f.HasFlag(StatusFlags.InRepair) ||
            f.HasFlag(StatusFlags.OnHold) || f.HasFlag(StatusFlags.UnderInvestigation))
                                                       return "state-pill state-attention";
        if (f.HasFlag(StatusFlags.OnLoan))             return "state-pill state-loaned";
        if (f.HasFlag(StatusFlags.WithHolder))         return "state-pill state-inuse";
        return "state-pill state-available";
    }

    public static bool IsAttentionFlags(StatusFlags f) =>
        f.HasFlag(StatusFlags.InRMA) || f.HasFlag(StatusFlags.InRepair) ||
        f.HasFlag(StatusFlags.Missing) || f.HasFlag(StatusFlags.OnHold) ||
        f.HasFlag(StatusFlags.UnderInvestigation);

    public static string FlagRowCss(StatusFlags f)
    {
        if (IsAttentionFlags(f)) return "warn";
        if (f.HasFlag(StatusFlags.WithHolder) || f.HasFlag(StatusFlags.OnLoan)) return "info";
        return "";
    }

    public static string CategoryLabel(AssetCategory c) => c switch
    {
        AssetCategory.PodTc77  => "TC77",
        AssetCategory.Battery  => "Battery",
        AssetCategory.Scanner  => "Scanner",
        _                      => "SAE",
    };

    public static string ScannerKindLabel(ScannerKind? k) => k switch
    {
        ScannerKind.TwoD  => "2D",
        ScannerKind.OneD  => "1D",
        ScannerKind.Ring  => "Ring",
        ScannerKind.Other => "Scanner",
        _                 => "Scanner",
    };

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

    public static DateTimeOffset? LocalDateStartUtc(string? value)
    {
        if (!TryParseLocalDate(value, out var date)) return null;

        return LocalDateStartUtc(date);
    }

    public static DateTimeOffset? LocalDateStartUtc(DateTime? value)
    {
        if (!value.HasValue) return null;

        return LocalDateStartUtc(DateOnly.FromDateTime(value.Value));
    }

    public static DateTimeOffset? LocalDateEndExclusiveUtc(string? value)
    {
        if (!TryParseLocalDate(value, out var date)) return null;

        return LocalDateEndExclusiveUtc(date);
    }

    public static DateTimeOffset? LocalDateEndExclusiveUtc(DateTime? value)
    {
        if (!value.HasValue) return null;

        return LocalDateEndExclusiveUtc(DateOnly.FromDateTime(value.Value));
    }

    public static DateTimeOffset? LocalDateTimeUtc(DateTime? date, string? time, TimeOnly fallbackTime)
    {
        if (!date.HasValue) return null;

        var localDate = DateOnly.FromDateTime(date.Value);
        var localTime = TryParseLocalTime(time, out var parsedTime)
            ? parsedTime
            : fallbackTime;
        var local = localDate.ToDateTime(localTime);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTimeOffset LocalDateStartUtc(DateOnly date)
    {
        var local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private static DateTimeOffset LocalDateEndExclusiveUtc(DateOnly date)
    {
        var local = date.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
    }

    private static bool TryParseLocalDate(string? value, out DateOnly date)
        => DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static bool TryParseLocalTime(string? value, out TimeOnly time)
        => TimeOnly.TryParseExact(
            value,
            new[] { "HH:mm", "HH:mm:ss" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

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
