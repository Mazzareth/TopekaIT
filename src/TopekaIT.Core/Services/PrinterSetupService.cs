using System.Net;
using System.Text.RegularExpressions;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Handles the printer setup dance: try the login, read enough sysinfo to know what it is, then only run approved setup commands.
/// </summary>
public class PrinterSetupService
{
    public const string PasswordFormatPlaceholder = "[Division Code]@[Zip Code]";

    private readonly IPrinterSetupTelnetClient _telnetClient;
    private readonly IPrinterRepository _printerRepository;
    private readonly IPrinterModelRepository _printerModelRepository;
    private readonly PrinterSetupSettings _settings;
    private readonly PrintNetCommandCatalog _commandCatalog;

    public PrinterSetupService(
        IPrinterSetupTelnetClient telnetClient,
        IPrinterRepository printerRepository,
        IPrinterModelRepository printerModelRepository,
        PrinterSetupSettings settings,
        PrintNetCommandCatalog commandCatalog)
    {
        _telnetClient = telnetClient;
        _printerRepository = printerRepository;
        _printerModelRepository = printerModelRepository;
        _settings = settings;
        _commandCatalog = commandCatalog;
    }

    public async Task<IReadOnlyList<PrinterSetupTestResult>> TestAsync(
        Division division,
        string input,
        CancellationToken ct = default)
    {
        var models = await _printerModelRepository.GetAllAsync(ct);
        var tasks = ParseIpAddresses(input)
            .Select(ip => TestOneAsync(division, ip, models, ct));
        return await Task.WhenAll(tasks);
    }

    public async Task<IReadOnlyList<PrinterSetupRunResult>> RunAllAsync(
        Division division,
        IEnumerable<PrinterSetupRunRequest> requests,
        CancellationToken ct = default)
    {
        var results = new List<PrinterSetupRunResult>();
        var models = await _printerModelRepository.GetAllAsync(ct);
        var validModels = models.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var request in requests)
        {
            if (!IPAddress.TryParse(request.IpAddress, out _))
            {
                results.Add(PrinterSetupRunResult.FailedResult(request.IpAddress, "Invalid IP address."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(request.SelectedModel)
                || !validModels.Contains(request.SelectedModel.Trim()))
            {
                results.Add(PrinterSetupRunResult.FailedResult(request.IpAddress, "Select a portal model before running setup."));
                continue;
            }

            var connection = await LoginAsync(division, request.IpAddress, ct);
            if (!connection.Connected || connection.Session == null)
            {
                results.Add(PrinterSetupRunResult.FailedResult(request.IpAddress, connection.ErrorMessage));
                continue;
            }

            await using var session = connection.Session;
            var commandFailure = await RunSetupCommandsAsync(session, ct);
            if (!string.IsNullOrWhiteSpace(commandFailure))
            {
                results.Add(PrinterSetupRunResult.FailedResult(request.IpAddress, commandFailure));
                continue;
            }

            var sysInfo = await session.SendCommandAsync("list sysinfo", ct);
            var detectedName = ParseDetectedInfo(sysInfo, "").Name;
            var created = await CreateOrUpdatePrinterAsync(request, detectedName, ct);
            results.Add(PrinterSetupRunResult.SuccessResult(request.IpAddress, created));
        }

        return results;
    }

    public static string? BuildDivisionPassword(Division division)
    {
        var code = division.PrinterPasswordCode?.Trim();
        var zip = division.PrinterPasswordZipCode?.Trim();
        return string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(zip)
            ? null
            : $"{code}@{zip}";
    }

    public static PrinterDetectedInfo ParseDetectedInfo(string sysInfoOutput, string ptrCfgOutput)
    {
        var name = ParseSysInfoValue(sysInfoOutput, "Printer Name", "System Name", "Description", "Name", "sysName", "Hostname", "Host Name");
        var model = FirstValue(ptrCfgOutput, "Printer Model", "Model", "Printer Type", "Product", "Type")
            ?? ParseSysInfoValue(sysInfoOutput, "Printer Model", "Model Name", "Model", "Product", "Type");

        model ??= FindModelToken(ptrCfgOutput) ?? FindModelToken(sysInfoOutput);

        return new PrinterDetectedInfo(name ?? "", model ?? "");
    }

    public static string? ParseSysInfoValue(string sysInfoOutput, params string[] keys) =>
        FirstValue(sysInfoOutput, keys);

    public static IReadOnlyList<string> ParseIpAddresses(string input) =>
        input.Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<PrinterSetupTestResult> TestOneAsync(
        Division division,
        string ip,
        IReadOnlyList<PrinterModel> models,
        CancellationToken ct)
    {
        if (!IPAddress.TryParse(ip, out _))
        {
            return PrinterSetupTestResult.FailedResult(ip, "Invalid IP address.");
        }

        var connection = await LoginAsync(division, ip, ct);
        if (!connection.Connected || connection.Session == null)
        {
            return PrinterSetupTestResult.FailedResult(ip, connection.ErrorMessage);
        }

        await using var session = connection.Session;
        var sysInfo = await session.SendCommandAsync("list sysinfo", ct);
        var ptrCfg = await session.SendCommandAsync("list ptrcfg", ct);
        var detected = ParseDetectedInfo(sysInfo, ptrCfg);
        var selectedModel = SelectDefaultModel(detected.Model, models);

        return PrinterSetupTestResult.ConnectedResult(ip, detected.Name, detected.Model, selectedModel);
    }

    private async Task<PrinterSetupConnection> LoginAsync(Division division, string ip, CancellationToken ct)
    {
        var timeout = TimeSpan.FromMilliseconds(Math.Max(_settings.TimeoutMs, 1000));
        var blank = await _telnetClient.TryLoginAsync(ip, _settings.TelnetPort, "root", "", timeout, ct);
        if (blank.Success)
        {
            return new PrinterSetupConnection(true, blank.Session);
        }

        var divisionPassword = BuildDivisionPassword(division);
        if (!string.IsNullOrWhiteSpace(divisionPassword))
        {
            var divisionLogin = await _telnetClient.TryLoginAsync(ip, _settings.TelnetPort, "root", divisionPassword, timeout, ct);
            if (divisionLogin.Success)
            {
                return new PrinterSetupConnection(true, divisionLogin.Session);
            }
        }

        return new PrinterSetupConnection(
            false,
            null,
            $"Password for Printer not valid, Password for printer needs to be {DisplayPasswordFormat(division)} or Blank.");
    }

    private async Task<string?> RunSetupCommandsAsync(IPrinterSetupTelnetSession session, CancellationToken ct)
    {
        var commands = new[]
        {
            "set snmp on",
            $"set snmp manager {_settings.ManagerIndex} {_settings.ManagerIp} {_settings.Community}",
            $"set snmp trapport {_settings.ManagerIndex} {_settings.TrapPort}",
            $"set snmp trap {_settings.ManagerIndex} active",
            "save",
        };

        foreach (var command in commands)
        {
            var commandAssessment = _commandCatalog.Classify(command);
            if (commandAssessment.Access != PrintNetCommandAccess.Protected)
            {
                return $"Command blocked by PrintNet catalog: {command}";
            }

            var output = await session.SendCommandAsync(command, ct);
            if (LooksLikeCommandFailure(output))
            {
                return $"Command failed: {command}";
            }
        }

        var snmpCheck = await session.SendCommandAsync("list snmp", ct);
        if (!snmpCheck.Contains(_settings.ManagerIp)
            || !snmpCheck.Contains(_settings.Community, StringComparison.OrdinalIgnoreCase)
            || !snmpCheck.Contains(_settings.TrapPort.ToString())
            || !snmpCheck.Contains("active", StringComparison.OrdinalIgnoreCase))
        {
            return "Verification failed: SNMP manager/trap configuration not active.";
        }

        return null;
    }

    private async Task<bool> CreateOrUpdatePrinterAsync(PrinterSetupRunRequest request, string detectedName, CancellationToken ct)
    {
        var printers = await _printerRepository.GetAllAsync(ct);
        var existing = printers.FirstOrDefault(p => string.Equals(p.IpAddress?.Trim(), request.IpAddress, StringComparison.OrdinalIgnoreCase));
        var name = string.IsNullOrWhiteSpace(detectedName) ? request.IpAddress : detectedName.Trim();
        var model = request.SelectedModel.Trim();

        if (existing != null)
        {
            var changed = false;
            if (ShouldReplacePlaceholder(existing.Name, existing.IpAddress) && !string.IsNullOrWhiteSpace(name))
            {
                existing.Name = name;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(existing.Model) || string.Equals(existing.Model, PrinterModels.T8000, StringComparison.OrdinalIgnoreCase))
            {
                existing.Model = model;
                changed = true;
            }

            if (changed)
            {
                await _printerRepository.UpdateAsync(existing, ct);
            }

            return false;
        }

        var printer = new Printer
        {
            Id = NextPrinterId(printers),
            Name = name,
            Department = "",
            Model = model,
            IpAddress = request.IpAddress,
            Status = PrinterStatus.Up,
        };

        await _printerRepository.AddAsync(printer, ct);
        return true;
    }

    private static string SelectDefaultModel(string detectedModel, IReadOnlyList<PrinterModel> models)
    {
        if (!string.IsNullOrWhiteSpace(detectedModel))
        {
            var exact = models.FirstOrDefault(m => string.Equals(m.Name, detectedModel.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact.Name;
            }

            if (detectedModel.Trim().StartsWith(PrinterModels.T8000, StringComparison.OrdinalIgnoreCase))
            {
                return models.FirstOrDefault(m => string.Equals(m.Name, PrinterModels.T8000, StringComparison.OrdinalIgnoreCase))?.Name ?? "";
            }
        }

        return "";
    }

    private static string DisplayPasswordFormat(Division division) =>
        !string.IsNullOrWhiteSpace(division.PrinterPasswordCode) && !string.IsNullOrWhiteSpace(division.PrinterPasswordZipCode)
            ? $"{division.PrinterPasswordCode.Trim()}@{division.PrinterPasswordZipCode.Trim()}"
            : PasswordFormatPlaceholder;

    private static string NextPrinterId(IReadOnlyList<Printer> printers)
    {
        var used = printers.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = printers.Count + 1; i < 1000; i++)
        {
            var id = $"p-{i:D2}";
            if (!used.Contains(id))
            {
                return id;
            }
        }

        return $"p-{Guid.NewGuid():N}"[..16];
    }

    private static bool ShouldReplacePlaceholder(string? name, string? ipAddress) =>
        string.IsNullOrWhiteSpace(name)
        || string.Equals(name.Trim(), ipAddress?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeCommandFailure(string output) =>
        output.Contains("invalid", StringComparison.OrdinalIgnoreCase)
        || output.Contains("unknown", StringComparison.OrdinalIgnoreCase)
        || output.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || output.Contains("error", StringComparison.OrdinalIgnoreCase)
        || output.Contains("syntax", StringComparison.OrdinalIgnoreCase)
        || output.Contains("usage", StringComparison.OrdinalIgnoreCase);

    private static string? FirstValue(string output, params string[] keys)
    {
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            foreach (var key in keys)
            {
                var match = Regex.Match(
                    line,
                    $"^{Regex.Escape(key)}\\s*[:=]\\s*(?<value>.+)$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    var value = match.Groups["value"].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static string? FindModelToken(string output)
    {
        var match = Regex.Match(output, @"\bT8000[-\w]*\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Value : null;
    }

    private sealed record PrinterSetupConnection(bool Connected, IPrinterSetupTelnetSession? Session, string? ErrorMessage = null);
}

/// <summary>
/// The knobs for the auto-setup flow. These are boring on purpose so division setup does not hide magic values in code.
/// </summary>
public sealed class PrinterSetupSettings
{
    public string ManagerIp { get; set; } = "10.36.155.64";
    public string Community { get; set; } = "Public";
    public int TrapPort { get; set; } = 162;
    public int ManagerIndex { get; set; } = 1;
    public int TelnetPort { get; set; } = 23;
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// The bits we can scrape from printer output before the portal decides how to store it.
/// </summary>
public sealed record PrinterDetectedInfo(string Name, string Model);

/// <summary>
/// One test result for "can we talk to this printer and what does it look like?"
/// </summary>
public sealed record PrinterSetupTestResult(
    string IpAddress,
    bool Connected,
    string Status,
    string? ErrorMessage,
    string DetectedName,
    string DetectedModel,
    string SelectedModel)
{
    public static PrinterSetupTestResult ConnectedResult(string ipAddress, string detectedName, string detectedModel, string selectedModel) =>
        new(ipAddress, true, "Connected", null, detectedName, detectedModel, selectedModel);

    public static PrinterSetupTestResult FailedResult(string ipAddress, string? errorMessage) =>
        new(ipAddress, false, "Failed", errorMessage ?? "Connection failed.", "", "", "");
}

/// <summary>
/// The selected setup work for one printer after the test pass has shown what is on the network.
/// </summary>
public sealed record PrinterSetupRunRequest(
    string IpAddress,
    string DetectedName,
    string DetectedModel,
    string SelectedModel);

/// <summary>
/// The final answer for a setup attempt, including whether the portal had to create a new printer row.
/// </summary>
public sealed record PrinterSetupRunResult(
    string IpAddress,
    bool Success,
    string Status,
    string? ErrorMessage,
    bool CreatedPrinter)
{
    public static PrinterSetupRunResult SuccessResult(string ipAddress, bool createdPrinter) =>
        new(ipAddress, true, createdPrinter ? "Configured and added" : "Configured", null, createdPrinter);

    public static PrinterSetupRunResult FailedResult(string ipAddress, string? errorMessage) =>
        new(ipAddress, false, "Failed", errorMessage ?? "Setup failed.", false);
}
