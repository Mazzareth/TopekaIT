using System.Net;
using System.Net.Sockets;
using System.Text;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

/// <summary>
/// Listens for SNMP alert/trap postings from printers and stores them as printer events.
/// </summary>
public class PrinterSnmpTrapSinkService : BackgroundService
{
    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly ILogger<PrinterSnmpTrapSinkService> _logger;
    private readonly bool _enabled;
    private readonly int _port;
    private readonly string _community;
    private readonly int _timeoutMs;
    private readonly SemaphoreSlim _mapLock = new(1, 1);
    private volatile IReadOnlyDictionary<string, PrinterRoute> _printerMap =
        new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _printerMapRefreshedAt;
    private static readonly TimeSpan PrinterMapRefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly string PrtAlertEntryPrefix = "1.3.6.1.2.1.43.18.1.1";
    private static readonly string PrtAlertIndexPrefix = $"{PrtAlertEntryPrefix}.1.";

    public PrinterSnmpTrapSinkService(
        IDbContextFactory<MasterDbContext> masterFactory,
        ILogger<PrinterSnmpTrapSinkService> logger,
        IConfiguration configuration)
    {
        _masterFactory = masterFactory;
        _logger = logger;
        _enabled = configuration.GetValue("PrinterSnmpTrapSink:Enabled", true);
        _port = configuration.GetValue("PrinterSnmpTrapSink:Port", 162);
        _community = configuration.GetValue<string>("PrinterMonitoring:SnmpCommunity") ?? "public";
        _timeoutMs = configuration.GetValue("PrinterMonitoring:SnmpTimeoutMs", 3000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Printer SNMP Trap Sink is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Printer SNMP Trap Sink could not bind UDP port {Port}. Check whether another SNMP trap service is using it.", _port);
                return;
            }

            _logger.LogInformation("Printer SNMP Trap Sink listening for UDP traps on port {Port}", _port);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    UdpReceiveResult result;
                    try
                    {
                        result = await udp.ReceiveAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogWarning(ex, "Printer SNMP Trap Sink UDP receive error on port {Port}; restarting listener", _port);
                        break;
                    }

                    _ = HandleTrapAsync(result, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer SNMP Trap Sink listener error on port {Port}; will restart", _port);
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Printer SNMP Trap Sink stopped.");
    }

    private async Task HandleTrapAsync(UdpReceiveResult result, CancellationToken ct)
    {
        var remoteIp = NormalizeIpAddress(result.RemoteEndPoint.Address);

        try
        {
            var messages = MessageFactory.ParseMessages(result.Buffer, new UserRegistry());
            if (messages.Count == 0)
            {
                _logger.LogWarning("Received SNMP trap payload from {Ip}, but it did not contain any SNMP messages.", remoteIp);
                return;
            }

            var route = await ResolvePrinterAsync(remoteIp, ct);
            if (route == null)
            {
                foreach (var message in messages)
                {
                    _logger.LogWarning(
                        "No printer registered for SNMP trap source IP {Ip}; discarded trap: {Message}",
                        remoteIp,
                        FormatTrapMessage(message, remoteIp, alertRow: null));
                }

                return;
            }

            foreach (var message in messages)
            {
                await PersistTrapAsync(message, route, remoteIp, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error handling SNMP trap from {Ip}", remoteIp);
        }
    }

    private async Task PersistTrapAsync(ISnmpMessage message, PrinterRoute route, string remoteIp, CancellationToken ct)
    {
        var alertRow = await TryQueryPrinterAlertRowAsync(message, remoteIp, ct);
        var rawMessage = FormatTrapMessage(message, remoteIp, alertRow);
        var (eventType, severity) = ClassifyTrap(rawMessage, alertRow);
        var alert = PrinterAlertNormalizer.Normalize(rawMessage, eventType, severity);

        var ev = new PrinterEvent
        {
            PrinterId = route.PrinterId,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            RawMessage = rawMessage,
            Severity = alert.Severity,
            AlertKey = alert.AlertKey,
            AlertTitle = alert.AlertTitle,
            AlertCategory = alert.AlertCategory,
            AlertDetail = alert.AlertDetail,
            FriendlyMessage = alert.FriendlyMessage,
            AlertTrainingLevel = alert.TrainingLevel,
        };

        try
        {
            var eventRepo = new PrinterEventRepository(new DirectDivisionDbContextFactory(route.ConnectionString));
            await eventRepo.AddAsync(ev, ct);
            _logger.LogInformation("Captured SNMP trap event for {PrinterId}: {EventType} {Message}", route.PrinterId, eventType, rawMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist SNMP trap PrinterEvent for printer {PrinterId}", route.PrinterId);
        }
    }

    private async Task<PrinterRoute?> ResolvePrinterAsync(string ip, CancellationToken ct)
    {
        await RefreshPrinterMapIfNeededAsync(ct);
        var map = _printerMap;
        return map.TryGetValue(ip, out var route) ? route : null;
    }

    private async Task RefreshPrinterMapIfNeededAsync(CancellationToken ct)
    {
        var map = _printerMap;
        if (map.Count > 0 && DateTimeOffset.UtcNow - _printerMapRefreshedAt < PrinterMapRefreshInterval)
        {
            return;
        }

        await _mapLock.WaitAsync(ct);
        try
        {
            var mapInner = _printerMap;
            if (mapInner.Count > 0 && DateTimeOffset.UtcNow - _printerMapRefreshedAt < PrinterMapRefreshInterval)
            {
                return;
            }

            var next = new Dictionary<string, PrinterRoute>(StringComparer.OrdinalIgnoreCase);
            await using var masterDb = await _masterFactory.CreateDbContextAsync(ct);
            var divisions = await masterDb.Divisions.AsNoTracking().ToListAsync(ct);

            foreach (var division in divisions)
            {
                var factory = new DirectDivisionDbContextFactory(division.ConnectionString);
                var printers = await new PrinterRepository(factory).GetAllAsync(ct);
                foreach (var printer in printers.Where(p => PrinterModels.SupportsLogging(p.Model) && !string.IsNullOrWhiteSpace(p.IpAddress)))
                {
                    next[NormalizeIpAddress(printer.IpAddress)] = new PrinterRoute(printer.Id, division.ConnectionString);
                }
            }

            _printerMapRefreshedAt = DateTimeOffset.UtcNow;
            _printerMap = next;
        }
        finally
        {
            _mapLock.Release();
        }
    }

    private async Task<PrinterAlertRow?> TryQueryPrinterAlertRowAsync(ISnmpMessage message, string remoteIp, CancellationToken ct)
    {
        var rowSuffix = TryGetPrinterAlertRowSuffix(message);
        if (string.IsNullOrEmpty(rowSuffix))
        {
            return null;
        }

        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(remoteIp), 161);
            var community = new OctetString(_community);
            var oids = new List<Variable>
            {
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.2.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.3.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.4.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.5.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.6.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.7.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.8.{rowSuffix}")),
                new(new ObjectIdentifier($"{PrtAlertEntryPrefix}.9.{rowSuffix}")),
            };

            using var timeoutCts = new CancellationTokenSource(_timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            var results = await Messenger.GetAsync(VersionCode.V1, endpoint, community, oids, linkedCts.Token);

            return PrinterAlertRow.FromSnmpResults(rowSuffix, results);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            _logger.LogDebug(ex, "Could not enrich Printer-MIB alert row {RowSuffix} from {Ip}", rowSuffix, remoteIp);
            return null;
        }
    }

    private static string? TryGetPrinterAlertRowSuffix(ISnmpMessage message)
    {
        var variable = message.Scope?.Pdu?.Variables?
            .FirstOrDefault(v => v.Id.ToString().StartsWith(PrtAlertIndexPrefix, StringComparison.Ordinal));

        if (variable == null)
        {
            return null;
        }

        var oid = variable.Id.ToString();
        return oid.Length > PrtAlertIndexPrefix.Length
            ? oid[PrtAlertIndexPrefix.Length..]
            : null;
    }

    private static string FormatTrapMessage(ISnmpMessage message, string remoteIp, PrinterAlertRow? alertRow)
    {
        var variables = message.Scope?.Pdu?.Variables;
        var variableSummary = variables?.Count > 0
            ? string.Join("; ", variables.Select(FormatVariable))
            : "";

        var parts = new List<string>
        {
            $"SNMP {message.Version} trap from {remoteIp}",
        };

        var friendlyMessage = BuildFriendlyTrapMessage(alertRow, variableSummary);
        if (!string.IsNullOrEmpty(friendlyMessage))
        {
            parts.Add(friendlyMessage);
        }

        var faultHint = BuildFaultHint(variableSummary);
        if (!string.IsNullOrEmpty(faultHint))
        {
            parts.Add(faultHint);
        }

        if (alertRow != null)
        {
            parts.Add(alertRow.ToSummary());
        }

        if (message is TrapV1Message trapV1)
        {
            parts.Add($"enterprise={trapV1.Enterprise}");
            parts.Add($"generic={trapV1.Generic}");
            parts.Add($"specific={trapV1.Specific}");
        }
        else if (message is TrapV2Message trapV2)
        {
            parts.Add($"enterprise={trapV2.Enterprise}");
        }

        if (!string.IsNullOrEmpty(variableSummary))
        {
            parts.Add(variableSummary);
        }

        return string.Join(" | ", parts);
    }

    private static string FormatVariable(Variable variable)
    {
        var value = variable.Data.ToString();
        return $"{variable.Id}={value}";
    }

    private static string BuildFriendlyTrapMessage(PrinterAlertRow? alertRow, string variableSummary)
    {
        if (alertRow == null)
        {
            return "";
        }

        var subject = FormatAlertSubject(alertRow);
        var description = alertRow.Description?.Trim();
        var detail = alertRow.Code switch
        {
            3 or 501 => $"{subject} is open.",
            4 or 502 => $"{subject} was closed.",
            8 or 1306 or 1312 => $"{subject} reported a jam.",
            12 or 807 or 1106 => $"{subject} is almost empty.",
            13 or 808 or 1101 or 1102 or 1103 => $"{subject} is empty.",
            18 => $"{subject} was opened.",
            19 => $"{subject} was closed.",
            20 or 503 => $"{subject} turned on.",
            21 or 504 => $"{subject} turned off.",
            22 => $"{subject} is offline.",
            23 => $"{subject} is in power saver mode.",
            24 => $"{subject} is warming up.",
            29 => $"{subject} has a recoverable fault.",
            30 => $"{subject} has an unrecoverable fault.",
            33 => $"{subject} has a motor fault.",
            34 => $"{subject} reported low memory.",
            35 => $"{subject} is under temperature.",
            36 => $"{subject} is over temperature.",
            37 => $"{subject} has a timing fault.",
            38 => $"{subject} has a thermistor fault.",
            507 => $"{subject} is ready to print.",
            801 => $"{subject} is missing.",
            802 => $"{subject} changed media size.",
            809 or 810 or 1310 => $"{subject} needs media input.",
            811 => $"{subject} has a tray position fault.",
            1104 or 1121 => $"{subject} is missing.",
            1305 or 1311 => $"{subject} has a feed path fault.",
            1501 => $"{subject} is full.",
            _ => BuildDescriptionMessage(subject, description, variableSummary),
        };

        if (string.IsNullOrWhiteSpace(detail))
        {
            return "";
        }

        var qualifiers = new List<string>();
        if (!string.IsNullOrWhiteSpace(description) &&
            !detail.Contains(description, StringComparison.OrdinalIgnoreCase))
        {
            qualifiers.Add($"reported \"{description}\"");
        }

        if (!string.IsNullOrWhiteSpace(alertRow.Location))
        {
            qualifiers.Add($"location {alertRow.Location}");
        }

        if (alertRow.SeverityLevel.HasValue)
        {
            qualifiers.Add($"severity {FormatFriendlySeverity(alertRow.SeverityLevel.Value)}");
        }

        if (qualifiers.Count > 0)
        {
            detail = $"{detail} ({string.Join(", ", qualifiers)})";
        }

        return $"Message={detail}";
    }

    private static string BuildDescriptionMessage(string subject, string? description, string variableSummary)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            return $"{subject} reported {description.Trim()}.";
        }

        var faultHint = BuildFaultHint(variableSummary);
        if (!string.IsNullOrWhiteSpace(faultHint))
        {
            return $"{subject} reported {faultHint["Fault=".Length..].ToLowerInvariant()}.";
        }

        return "";
    }

    private static string FormatAlertSubject(PrinterAlertRow alertRow)
    {
        return alertRow.Group switch
        {
            6 => "Cover",
            8 => "Input tray",
            9 => "Output tray",
            10 => "Marker",
            11 => "Supply",
            13 => "Media path",
            30 => "Finisher",
            31 => "Finisher supply",
            _ => "Printer",
        };
    }

    private static string FormatFriendlySeverity(int value) => value switch
    {
        3 => "critical",
        4 => "warning",
        5 => "warning",
        _ => "info",
    };

    private static string BuildFaultHint(string message)
    {
        var upper = message.ToUpperInvariant();

        if (upper.Contains("GAP"))
        {
            return "Fault=Gap not detected";
        }

        if (upper.Contains("PAPER") || upper.Contains("MEDIA"))
        {
            return "Fault=Media/paper";
        }

        if (upper.Contains("RIBBON"))
        {
            return "Fault=Ribbon";
        }

        if (upper.Contains("HEAD") && (upper.Contains("OPEN") || upper.Contains("LIFT")))
        {
            return "Fault=Printhead open";
        }

        if (upper.Contains("JAM"))
        {
            return "Fault=Jam";
        }

        if (upper.Contains("LOW") || upper.Contains("EMPTY"))
        {
            return "Fault=Supply level";
        }

        if (upper.Contains("ER_") || upper.Contains("ERR") || upper.Contains("ERROR") || upper.Contains("FAULT"))
        {
            return "Fault=Printer error";
        }

        return "";
    }

    private static (string EventType, string? Severity) ClassifyTrap(string message, PrinterAlertRow? alertRow)
    {
        if (alertRow?.SeverityLevel == 3)
        {
            return ("Error", "Error");
        }

        if (alertRow?.SeverityLevel is 4 or 5)
        {
            return ("Warning", "Warning");
        }

        var upper = message.ToUpperInvariant();
        if (upper.Contains("ERROR") ||
            upper.Contains("FAULT") ||
            upper.Contains("FAIL") ||
            upper.Contains("GAP") ||
            upper.Contains("PAPER") ||
            upper.Contains("JAM") ||
            upper.Contains(" ER_") ||
            upper.Contains("ERR"))
        {
            return ("Error", "Error");
        }

        if (upper.Contains("WARN") || upper.Contains("LOW") || upper.Contains("EMPTY"))
        {
            return ("Warning", "Warning");
        }

        return ("Info", "Info");
    }

    private static string NormalizeIpAddress(IPAddress? address)
    {
        if (address == null)
        {
            return "unknown";
        }

        return address.IsIPv4MappedToIPv6
            ? address.MapToIPv4().ToString()
            : address.ToString();
    }

    private static string NormalizeIpAddress(string ip)
    {
        return IPAddress.TryParse(ip, out var address)
            ? NormalizeIpAddress(address)
            : ip.Trim();
    }

    private sealed class PrinterAlertRow
    {
        public string RowSuffix { get; init; } = "";
        public int? SeverityLevel { get; init; }
        public int? TrainingLevel { get; init; }
        public int? Group { get; init; }
        public int? GroupIndex { get; init; }
        public string? Location { get; init; }
        public int? Code { get; init; }
        public string? Description { get; init; }
        public string? Time { get; init; }

        public static PrinterAlertRow FromSnmpResults(string rowSuffix, IEnumerable<Variable> variables)
        {
            var row = new PrinterAlertRowBuilder { RowSuffix = rowSuffix };

            foreach (var variable in variables)
            {
                if (variable.Data == null || variable.Data is Null || variable.Data is NoSuchObject || variable.Data is NoSuchInstance)
                {
                    continue;
                }

                var oid = variable.Id.ToString();
                var value = variable.Data.ToString().Trim();

                if (oid.StartsWith($"{PrtAlertEntryPrefix}.2.", StringComparison.Ordinal))
                {
                    row.SeverityLevel = TryParseInt(value);
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.3.", StringComparison.Ordinal))
                {
                    row.TrainingLevel = TryParseInt(value);
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.4.", StringComparison.Ordinal))
                {
                    row.Group = TryParseInt(value);
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.5.", StringComparison.Ordinal))
                {
                    row.GroupIndex = TryParseInt(value);
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.6.", StringComparison.Ordinal))
                {
                    row.Location = value;
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.7.", StringComparison.Ordinal))
                {
                    row.Code = TryParseInt(value);
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.8.", StringComparison.Ordinal))
                {
                    row.Description = value;
                }
                else if (oid.StartsWith($"{PrtAlertEntryPrefix}.9.", StringComparison.Ordinal))
                {
                    row.Time = value;
                }
            }

            return row.Build();
        }

        public string ToSummary()
        {
            var parts = new List<string>
            {
                $"alertRow={RowSuffix}",
            };

            if (SeverityLevel.HasValue)
            {
                parts.Add($"severity={FormatSeverity(SeverityLevel.Value)}");
            }

            if (Code.HasValue)
            {
                parts.Add($"code={FormatAlertCode(Code.Value)}");
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                parts.Add($"description={Description}");
            }

            if (Group.HasValue)
            {
                parts.Add($"group={FormatGroup(Group.Value)}");
            }

            if (GroupIndex.HasValue)
            {
                parts.Add($"groupIndex={GroupIndex.Value}");
            }

            if (!string.IsNullOrWhiteSpace(Location))
            {
                parts.Add($"location={Location}");
            }

            if (TrainingLevel.HasValue)
            {
                parts.Add($"training={FormatTrainingLevel(TrainingLevel.Value)}");
            }

            return string.Join(", ", parts);
        }

        private static int? TryParseInt(string value)
        {
            return int.TryParse(value, out var parsed) ? parsed : null;
        }

        private static string FormatSeverity(int value) => value switch
        {
            1 => "other(1)",
            3 => "critical(3)",
            4 => "warning(4)",
            5 => "warningBinaryChangeEvent(5)",
            _ => value.ToString(),
        };

        private static string FormatAlertCode(int value) => value switch
        {
            1 => "other(1)",
            2 => "unknown(2)",
            3 => "coverOpen(3)",
            4 => "coverClosed(4)",
            7 => "configurationChange(7)",
            8 => "jam(8)",
            12 => "subunitAlmostEmpty(12)",
            13 => "subunitEmpty(13)",
            18 => "subunitOpened(18)",
            19 => "subunitClosed(19)",
            20 => "subunitTurnedOn(20)",
            21 => "subunitTurnedOff(21)",
            22 => "subunitOffline(22)",
            23 => "subunitPowerSaver(23)",
            24 => "subunitWarmingUp(24)",
            29 => "subunitRecoverableFailure(29)",
            30 => "subunitUnrecoverableFailure(30)",
            33 => "subunitMotorFailure(33)",
            34 => "subunitMemoryExhausted(34)",
            35 => "subunitUnderTemperature(35)",
            36 => "subunitOverTemperature(36)",
            37 => "subunitTimingFailure(37)",
            38 => "subunitThermistorFailure(38)",
            501 => "doorOpen(501)",
            502 => "doorClosed(502)",
            503 => "powerUp(503)",
            504 => "powerDown(504)",
            507 => "printerReadyToPrint(507)",
            801 => "inputMediaTrayMissing(801)",
            802 => "inputMediaSizeChange(802)",
            807 => "inputMediaSupplyLow(807)",
            808 => "inputMediaSupplyEmpty(808)",
            809 => "inputMediaChangeRequest(809)",
            810 => "inputManualInputRequest(810)",
            811 => "inputTrayPositionFailure(811)",
            1101 => "markerTonerEmpty(1101)",
            1102 => "markerInkEmpty(1102)",
            1103 => "markerPrintRibbonEmpty(1103)",
            1104 => "markerSupplyMissing(1104)",
            1106 => "markerPrintRibbonAlmostEmpty(1106)",
            1121 => "markerPrintRibbonMissing(1121)",
            1305 => "mediaPathFailure(1305)",
            1306 => "mediaPathJam(1306)",
            1310 => "mediaPathInputRequest(1310)",
            1311 => "mediaPathInputFeedError(1311)",
            1312 => "mediaPathInputJam(1312)",
            1501 => "outputMediaTrayFull(1501)",
            _ => value.ToString(),
        };

        private static string FormatGroup(int value) => value switch
        {
            1 => "other(1)",
            2 => "unknown(2)",
            3 => "hostResourcesMIBStorageTable(3)",
            4 => "hostResourcesMIBDeviceTable(4)",
            5 => "generalPrinter(5)",
            6 => "cover(6)",
            7 => "localization(7)",
            8 => "input(8)",
            9 => "output(9)",
            10 => "marker(10)",
            11 => "markerSupplies(11)",
            12 => "markerColorant(12)",
            13 => "mediaPath(13)",
            14 => "channel(14)",
            15 => "interpreter(15)",
            16 => "consoleDisplayBuffer(16)",
            17 => "consoleLights(17)",
            18 => "alert(18)",
            30 => "finDevice(30)",
            31 => "finSupply(31)",
            32 => "finSupplyMediaInput(32)",
            33 => "finAttribute(33)",
            50 => "scanDevice(50)",
            51 => "scanner(51)",
            52 => "scanMediaPath(52)",
            60 => "faxDevice(60)",
            61 => "faxModem(61)",
            70 => "outputChannel(70)",
            _ => value.ToString(),
        };

        private static string FormatTrainingLevel(int value) => value switch
        {
            1 => "other(1)",
            2 => "unknown(2)",
            3 => "untrained(3)",
            4 => "trained(4)",
            5 => "fieldService(5)",
            6 => "management(6)",
            7 => "noInterventionRequired(7)",
            _ => value.ToString(),
        };
    }

    private sealed class PrinterAlertRowBuilder
    {
        public string RowSuffix { get; init; } = "";
        public int? SeverityLevel { get; set; }
        public int? TrainingLevel { get; set; }
        public int? Group { get; set; }
        public int? GroupIndex { get; set; }
        public string? Location { get; set; }
        public int? Code { get; set; }
        public string? Description { get; set; }
        public string? Time { get; set; }

        public PrinterAlertRow Build() => new()
        {
            RowSuffix = RowSuffix,
            SeverityLevel = SeverityLevel,
            TrainingLevel = TrainingLevel,
            Group = Group,
            GroupIndex = GroupIndex,
            Location = Location,
            Code = Code,
            Description = Description,
            Time = Time,
        };
    }

    private sealed record PrinterRoute(string PrinterId, string ConnectionString);
}
