using System.Net;

namespace TopekaIT.Core.Services;

public sealed class PrintNetCommandCatalog
{
    public const string ManualUrl = "https://efficientbi.com/wp-content/uploads/PrintNet-Ethernet-User%E2%80%99s-Manual-257367g_UM_PrintNet_Ethernet_TH.pdf";

    private static readonly HashSet<string> SafeListSubjects = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "arp",
        "diff",
        "dhcp",
        "ifc",
        "logins",
        "lpd",
        "net",
        "pping",
        "prn",
        "pserver",
        "ptrcfg",
        "ptrmgmt",
        "snmp",
        "sysinfo",
        "tcpip",
        "test",
        "tn",
        "uptime",
        "user",
        "var",
    };

    private static readonly HashSet<string> SafeListSubjectsWithOptionalName = new(StringComparer.OrdinalIgnoreCase)
    {
        "dest",
        "logpath",
        "model",
    };

    private static readonly HashSet<string> IdentityFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "label",
        "name",
        "contact",
        "location",
        "prnserial",
    };

    private static readonly HashSet<string> SnmpAlertGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "all",
        "warning",
        "offline",
        "mediainput",
        "mediaoutput",
        "mediapath",
        "marker",
        "cutter",
        "barcode",
        "scanner",
        "intervention",
        "consumable",
        "label",
        "powercart",
        "rfid",
    };

    private static readonly IReadOnlyList<PrintNetCommandDefinition> CommandDefinitions =
    [
        new("list all", PrintNetCommandAccess.Safe, "Read-only", "Lists all current settings."),
        new("list arp", PrintNetCommandAccess.Safe, "Read-only", "Lists the current ARP table."),
        new("list dest [destination]", PrintNetCommandAccess.Safe, "Read-only", "Lists destination settings."),
        new("list diff", PrintNetCommandAccess.Safe, "Read-only", "Lists differences between current and stored settings."),
        new("list ifc", PrintNetCommandAccess.Safe, "Read-only", "Lists interface settings."),
        new("list logins", PrintNetCommandAccess.Safe, "Read-only", "Lists active logins."),
        new("list logpath [logpath]", PrintNetCommandAccess.Safe, "Read-only", "Lists logpath settings."),
        new("list model [model]", PrintNetCommandAccess.Safe, "Read-only", "Lists model settings."),
        new("list net", PrintNetCommandAccess.Safe, "Read-only", "Lists TCP/IP network settings."),
        new("list pping", PrintNetCommandAccess.Safe, "Read-only", "Lists periodic ping settings."),
        new("list pserver", PrintNetCommandAccess.Safe, "Read-only", "Lists print server settings."),
        new("list ptrcfg", PrintNetCommandAccess.Safe, "Read-only", "Lists printer configuration."),
        new("list ptrmgmt", PrintNetCommandAccess.Safe, "Read-only", "Lists printer management port numbers."),
        new("list snmp", PrintNetCommandAccess.Safe, "Read-only", "Lists SNMP trap managers."),
        new("list sysinfo", PrintNetCommandAccess.Safe, "Read-only", "Lists PrintNet system information."),
        new("list tcpip", PrintNetCommandAccess.Safe, "Read-only", "Lists TCP/IP settings."),
        new("list test", PrintNetCommandAccess.Safe, "Read-only", "Lists output test status."),
        new("list tn", PrintNetCommandAccess.Safe, "Read-only", "Lists TN protocol settings."),
        new("list uptime", PrintNetCommandAccess.Safe, "Read-only", "Lists uptime since last reset."),
        new("list user", PrintNetCommandAccess.Safe, "Read-only", "Lists user definitions."),
        new("list var", PrintNetCommandAccess.Safe, "Read-only", "Lists variables."),
        new("list dhcp", PrintNetCommandAccess.Safe, "Read-only", "Lists DHCP information."),
        new("list lpd", PrintNetCommandAccess.Safe, "Read-only", "Lists LPD information."),
        new("?", PrintNetCommandAccess.Safe, "Read-only", "Lists available commands."),
        new("lpstat [jobID]", PrintNetCommandAccess.Safe, "Read-only", "Displays active and queued jobs and printer status."),
        new("ping [-s] <hostIPaddress> [datasize [packetnumber]]", PrintNetCommandAccess.Safe, "Read-only", "Pings another TCP/IP host from the PrintNet interface."),
        new("set sysinfo label|name|contact|location|prnserial [value]", PrintNetCommandAccess.Protected, "Identity", "Changes PrintNet identity metadata."),
        new("set snmp on", PrintNetCommandAccess.Protected, "SNMP", "Enables SNMP service."),
        new("set snmp manager <index> <ip> <community>", PrintNetCommandAccess.Protected, "SNMP", "Configures an SNMP trap manager."),
        new("set snmp trapport <index> <udp-port>", PrintNetCommandAccess.Protected, "SNMP", "Configures the trap manager UDP port."),
        new("set snmp trap <index> active", PrintNetCommandAccess.Protected, "SNMP", "Activates an SNMP trap manager entry."),
        new("set snmp alerts <index> <groups>", PrintNetCommandAccess.Protected, "SNMP", "Configures SNMP alert groups."),
        new("save", PrintNetCommandAccess.Protected, "Persistence", "Persists current runtime settings to flash."),
        new("store ...", PrintNetCommandAccess.Excluded, "Flash/defaults", "Store commands write flash settings and may require reset."),
        new("save default", PrintNetCommandAccess.Excluded, "Defaults", "Overwrites factory defaults."),
        new("reset|reboot|load [default]", PrintNetCommandAccess.Excluded, "Reset", "Restarts or reloads device settings."),
        new("set user ...", PrintNetCommandAccess.Excluded, "Security", "Changes users, passwords, privileges, or SNMP write community."),
        new("disable|enable ...", PrintNetCommandAccess.Excluded, "Protocol control", "Changes protocol availability."),
    ];

    public IReadOnlyList<PrintNetCommandDefinition> Commands => CommandDefinitions;

    public PrintNetCommandAssessment Classify(string? command)
    {
        var normalized = Normalize(command);
        if (normalized.Length == 0)
        {
            return Excluded(normalized, "Command is empty.");
        }

        if (ContainsCommandSeparator(command!))
        {
            return Excluded(normalized, "Only one telnet command may be classified at a time.");
        }

        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (IsExcluded(tokens, normalized, out var excludedReason))
        {
            return Excluded(normalized, excludedReason);
        }

        if (IsSafeListCommand(tokens))
        {
            return new PrintNetCommandAssessment(
                normalized,
                PrintNetCommandAccess.Safe,
                "Manual-listed read-only command.",
                FindDefinition("list"));
        }

        if (IsSafeMiscCommand(tokens, normalized))
        {
            return new PrintNetCommandAssessment(
                normalized,
                PrintNetCommandAccess.Safe,
                "Manual-listed read-only command.",
                FindDefinition(tokens[0]));
        }

        if (IsProtectedCommand(tokens, normalized, out var protectedReason))
        {
            return new PrintNetCommandAssessment(
                normalized,
                PrintNetCommandAccess.Protected,
                protectedReason,
                FindDefinition(tokens[0] == "save" ? "save" : $"{tokens[0]} {tokens[1]}"));
        }

        return Excluded(normalized, "Command is not in the approved PrintNet catalog.");
    }

    public bool IsSafe(string command) => Classify(command).Access == PrintNetCommandAccess.Safe;

    public bool IsProtected(string command) => Classify(command).Access == PrintNetCommandAccess.Protected;

    private static bool IsExcluded(string[] tokens, string normalized, out string reason)
    {
        reason = "";
        if (tokens.Length == 0)
        {
            reason = "Command is empty.";
            return true;
        }

        if (tokens[0] is "store")
        {
            reason = "Store commands write flash settings and are excluded by default.";
            return true;
        }

        if (tokens[0] is "reset" or "reboot" or "load" or "cancel" or "start" or "stop" or "close" or "chr" or "tn")
        {
            reason = "Reset, job-control, connection-control, and raw output commands are excluded by default.";
            return true;
        }

        if (tokens[0] is "disable" or "enable")
        {
            reason = "Protocol enable/disable commands are excluded by default.";
            return true;
        }

        if (normalized == "save default")
        {
            reason = "Saving factory defaults is excluded.";
            return true;
        }

        if (normalized.Contains(" from default", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains(" from stored", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Default and stored-value restore commands are excluded.";
            return true;
        }

        if (tokens.Length >= 2 && tokens[0] == "set" && tokens[1] is "user" or "dest" or "logpath" or "lpd" or "model" or "var")
        {
            reason = "Security, destination, job-path, model, LPD, and variable writes are excluded by default.";
            return true;
        }

        if (tokens is ["set", "snmp", "off"])
        {
            reason = "Disabling SNMP is excluded.";
            return true;
        }

        if (tokens.Length == 5 && tokens[0] == "set" && tokens[1] == "snmp" && tokens[2] == "trap" && tokens[4] == "-active")
        {
            reason = "Disabling an SNMP trap manager is excluded.";
            return true;
        }

        if (tokens.Length >= 3 && tokens[0] == "set" && tokens[1] == "sysinfo" && tokens[2] == "smtpserver")
        {
            reason = "Mail server configuration is excluded by default.";
            return true;
        }

        return false;
    }

    private static bool IsSafeListCommand(string[] tokens)
    {
        if (tokens.Length < 2 || tokens[0] != "list")
        {
            return false;
        }

        var subjectIndex = tokens[1] is "stored" or "default" ? 2 : 1;
        if (subjectIndex >= tokens.Length)
        {
            return false;
        }

        var subject = tokens[subjectIndex];
        if (SafeListSubjects.Contains(subject))
        {
            return tokens.Length == subjectIndex + 1;
        }

        if (!SafeListSubjectsWithOptionalName.Contains(subject))
        {
            return false;
        }

        return tokens.Length == subjectIndex + 1
            || tokens.Length == subjectIndex + 2 && IsSimpleArgument(tokens[subjectIndex + 1]);
    }

    private static bool IsSafeMiscCommand(string[] tokens, string normalized)
    {
        if (normalized == "?")
        {
            return true;
        }

        if (tokens[0] == "lpstat")
        {
            return tokens.Length == 1
                || tokens.Length == 2 && IsSimpleArgument(tokens[1]);
        }

        if (tokens[0] != "ping")
        {
            return false;
        }

        var index = 1;
        if (tokens.Length > index && tokens[index] == "-s")
        {
            index++;
        }

        if (tokens.Length <= index || !IPAddress.TryParse(tokens[index], out _))
        {
            return false;
        }

        var remaining = tokens.Length - index - 1;
        if (remaining is < 0 or > 2)
        {
            return false;
        }

        return tokens.Skip(index + 1).All(v => int.TryParse(v, out var number) && number > 0);
    }

    private static bool IsProtectedCommand(string[] tokens, string normalized, out string reason)
    {
        reason = "";
        if (normalized == "save")
        {
            reason = "Save persists current settings and requires protected execution.";
            return true;
        }

        if (tokens.Length >= 3 && tokens[0] == "set" && tokens[1] == "sysinfo" && IdentityFields.Contains(tokens[2]))
        {
            reason = "Identity metadata changes require protected execution.";
            return true;
        }

        if (tokens.Length < 3 || tokens[0] != "set" || tokens[1] != "snmp")
        {
            return false;
        }

        reason = "SNMP configuration changes require protected execution.";
        return tokens[2] switch
        {
            "on" => tokens.Length == 3,
            "manager" => tokens.Length == 6 && IsValidTrapIndex(tokens[3]) && IPAddress.TryParse(tokens[4], out _) && IsSimpleArgument(tokens[5]),
            "trapport" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && IsValidTrapPort(tokens[4]),
            "trap" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && tokens[4] == "active",
            "alerts" => tokens.Length >= 5 && IsValidTrapIndex(tokens[3]) && tokens.Skip(4).All(IsSnmpAlertGroup),
            "emailaddr" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && tokens[4].Contains('@'),
            "emailformat" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && tokens[4] is "short" or "-short",
            "shortmsglen" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && int.TryParse(tokens[4], out var length) && length is >= 15 and <= 80,
            "email" => tokens.Length == 5 && IsValidTrapIndex(tokens[3]) && tokens[4] == "active",
            _ => false,
        };
    }

    private static bool IsSnmpAlertGroup(string value)
    {
        var group = value.StartsWith('-') ? value[1..] : value;
        return SnmpAlertGroups.Contains(group);
    }

    private static bool IsValidTrapIndex(string value) =>
        int.TryParse(value, out var index) && index is >= 1 and <= 10;

    private static bool IsValidTrapPort(string value) =>
        int.TryParse(value, out var port) && (port == 162 || port is >= 49152 and <= 65535);

    private static bool IsSimpleArgument(string value) =>
        value.Length > 0 && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '@');

    private static bool ContainsCommandSeparator(string command) =>
        command.Any(c => c is '\r' or '\n' or ';' or '&' or '|');

    private static string Normalize(string? command) =>
        string.Join(' ', (command ?? "").Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static PrintNetCommandAssessment Excluded(string normalized, string reason) =>
        new(normalized, PrintNetCommandAccess.Excluded, reason, FindDefinition("excluded"));

    private static PrintNetCommandDefinition? FindDefinition(string prefix)
    {
        if (prefix == "excluded")
        {
            return CommandDefinitions.FirstOrDefault(c => c.Access == PrintNetCommandAccess.Excluded);
        }

        return CommandDefinitions.FirstOrDefault(c => c.Command.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}

public enum PrintNetCommandAccess
{
    Safe,
    Protected,
    Excluded,
}

public sealed record PrintNetCommandDefinition(
    string Command,
    PrintNetCommandAccess Access,
    string Category,
    string Description);

public sealed record PrintNetCommandAssessment(
    string NormalizedCommand,
    PrintNetCommandAccess Access,
    string Reason,
    PrintNetCommandDefinition? Definition)
{
    public bool CanRunWithoutAdditionalGate => Access == PrintNetCommandAccess.Safe;
    public bool RequiresProtectedGate => Access == PrintNetCommandAccess.Protected;
    public bool IsExcluded => Access == PrintNetCommandAccess.Excluded;
}
