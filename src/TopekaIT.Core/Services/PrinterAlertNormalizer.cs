using System.Text.RegularExpressions;

namespace TopekaIT.Core.Services;

public static partial class PrinterAlertNormalizer
{
    public const string SnmpAuthenticationFailureAlertKey = "SNMP_AUTHENTICATION_FAILURE";
    private const string SnmpAuthenticationFailureTitle = "SNMP Authentication Failure";
    private const string SnmpAuthenticationFailureCategory = "SNMP Authentication";
    private const string SnmpAuthenticationFailureDetail = "Authentication Failure";

    public static PrinterAlertInfo Normalize(string rawMessage, string? eventType = null, string? severity = null)
    {
        var message = rawMessage.Trim();
        var title = ExtractMessageTitle(message);
        var detail = ExtractQuotedReportedDetail(message) ?? ExtractField(message, "description");
        var normalizedSeverity = NormalizeSeverity(ExtractField(message, "severity") ?? severity ?? eventType);

        if (IsAuthenticationFailure(
            rawMessage: message,
            eventType: eventType,
            alertTitle: title,
            alertDetail: detail))
        {
            return BuildAuthenticationFailureInfo();
        }

        var normalizedDetail = NormalizeDetail(detail);
        var category = DeriveCategory(normalizedDetail, title, eventType);
        var normalizedTitle = NormalizeTitle(title, category);
        var trainingLevel = ExtractTrainingLevel(message);
        var alertKey = BuildAlertKey(normalizedDetail ?? normalizedTitle);
        var friendly = BuildFriendlyMessage(normalizedTitle, normalizedDetail, normalizedSeverity, trainingLevel);

        return new PrinterAlertInfo(
            alertKey,
            normalizedTitle,
            category,
            normalizedDetail,
            friendly,
            normalizedSeverity,
            trainingLevel);
    }

    public static bool IsAuthenticationFailure(
        string? rawMessage = null,
        string? eventType = null,
        string? alertKey = null,
        string? alertTitle = null,
        string? alertDetail = null,
        string? friendlyMessage = null)
    {
        var source = $"{rawMessage} {eventType} {alertKey} {alertTitle} {alertDetail} {friendlyMessage}";
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        return ContainsAuthFailure(source);
    }

    private static PrinterAlertInfo BuildAuthenticationFailureInfo()
    {
        const string displaySeverity = "Warning";
        var friendly = "SNMP authentication failure traps are being received while monitoring this printer. " +
            $"This usually points to SNMP community or listener configuration, not a printer hardware fault. Severity: {displaySeverity}";

        return new PrinterAlertInfo(
            SnmpAuthenticationFailureAlertKey,
            SnmpAuthenticationFailureTitle,
            SnmpAuthenticationFailureCategory,
            SnmpAuthenticationFailureDetail,
            friendly,
            displaySeverity,
            TrainingLevel: null);
    }

    private static string ExtractMessageTitle(string message)
    {
        var match = MessageRegex().Match(message);
        if (!match.Success)
        {
            return message.Length > 120 ? message[..120].Trim() : message;
        }

        return match.Groups["message"].Value.Trim();
    }

    private static string? ExtractQuotedReportedDetail(string message)
    {
        var match = ReportedRegex().Match(message);
        return match.Success ? match.Groups["detail"].Value.Trim() : null;
    }

    private static string? ExtractField(string message, string field)
    {
        var match = Regex.Match(message, $@"(?:^|\b){Regex.Escape(field)}=([^,|)]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim().Trim('"') : null;
    }

    private static int? ExtractTrainingLevel(string message)
    {
        var match = TrainingRegex().Match(message);
        if (!match.Success) return null;
        return int.TryParse(match.Groups["level"].Value, out var level) ? level : null;
    }

    private static string NormalizeTitle(string title, string category)
    {
        var clean = title.Trim().TrimEnd('.');
        clean = clean.Replace(" has a ", " ", StringComparison.OrdinalIgnoreCase);
        clean = clean.Replace(" has an ", " ", StringComparison.OrdinalIgnoreCase);
        clean = clean.Replace("  ", " ");

        if (clean.Length == 0)
        {
            clean = category;
        }

        return ToTitleCase(clean);
    }

    private static string? NormalizeDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        var clean = detail.Trim().TrimEnd('.');
        clean = clean.Replace(" - ", " ", StringComparison.Ordinal);
        clean = Regex.Replace(clean, @"\s+", " ");

        var tokens = clean.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length >= 3 && tokens[0].Equals("CUTTER", StringComparison.OrdinalIgnoreCase) && tokens[1].Equals("FAULT", StringComparison.OrdinalIgnoreCase))
        {
            return $"Cutter Fault - {ToTitleCase(tokens[2])}";
        }

        return ToTitleCase(clean);
    }

    private static string DeriveCategory(string? detail, string title, string? eventType)
    {
        var source = $"{detail} {title} {eventType}".ToUpperInvariant();

        if (ContainsAuthFailure(source)) return SnmpAuthenticationFailureCategory;
        if (source.Contains("RIBBON")) return "Ribbon";
        if (source.Contains("PRINTHEAD") || source.Contains("PRINT HEAD")) return "Printhead";
        if (source.Contains("CUTTER")) return "Cutter Fault";
        if (source.Contains("GAP")) return "Gap Not Detected";
        if (source.Contains("JAM")) return "Jam";
        if (source.Contains("MEDIA") || source.Contains("PAPER")) return "Media";
        if (source.Contains("OUTPUT")) return "Output Tray";
        if (source.Contains("INPUT")) return "Input Tray";

        return string.IsNullOrWhiteSpace(eventType) ? "Printer Alert" : ToTitleCase(eventType);
    }

    private static string NormalizeSeverity(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Info";

        var upper = value.ToUpperInvariant();
        if (upper.Contains("CRITICAL") || upper.Contains("ERROR") || upper.Contains("FAULT")) return "Critical";
        if (upper.Contains("WARN") || upper.Contains("LOW") || upper.Contains("EMPTY")) return "Warning";
        return "Info";
    }

    private static string BuildFriendlyMessage(string title, string? detail, string severity, int? trainingLevel)
    {
        var parts = new List<string> { title };
        if (!string.IsNullOrWhiteSpace(detail))
        {
            parts[0] = $"{parts[0]}: {detail}";
        }

        parts.Add($"Severity: {severity}");
        if (trainingLevel.HasValue)
        {
            parts.Add($"Training Level {trainingLevel.Value}");
        }

        return string.Join(", ", parts);
    }

    private static string BuildAlertKey(string value)
    {
        var upper = Regex.Replace(value.ToUpperInvariant(), @"[^A-Z0-9]+", "_").Trim('_');
        return string.IsNullOrWhiteSpace(upper) ? "PRINTER_ALERT" : upper;
    }

    private static bool ContainsAuthFailure(string value)
    {
        return value.Contains("AUTHENTICATIONFAIL", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("AUTHENTICATION FAILURE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("AUTH FAILURE", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("GENERIC=AUTHEN", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("SNMPTRAPOID.0=1.3.6.1.6.3.1.1.5.5", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("1.3.6.1.6.3.1.1.5.5", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";

        var index = 0;
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w =>
            {
                var lower = w.ToLowerInvariant();
                var current = index++;
                if (current > 0 && lower is "or" or "and" or "of" or "a" or "an" or "the") return lower;
                if (w.Length <= 2 && w.All(char.IsUpper)) return w;
                return char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
            });

        return string.Join(' ', words);
    }

    [GeneratedRegex(@"Message=(?<message>.*?)(?:\s*\|\s*alertRow=|\s*$)", RegexOptions.IgnoreCase)]
    private static partial Regex MessageRegex();

    [GeneratedRegex(@"reported\s+""(?<detail>[^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ReportedRegex();

    [GeneratedRegex(@"training=(?:[a-zA-Z]+)?\((?<level>\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex TrainingRegex();
}

public sealed record PrinterAlertInfo(
    string AlertKey,
    string AlertTitle,
    string AlertCategory,
    string? AlertDetail,
    string FriendlyMessage,
    string Severity,
    int? TrainingLevel);
