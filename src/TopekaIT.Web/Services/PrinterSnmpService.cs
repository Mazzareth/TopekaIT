using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TopekaIT.Web.Services;

/// <summary>
/// Queries T8000 printers via SNMP for system information that helps inventory feel less hand-entered.
/// </summary>
public class PrinterSnmpService
{
    private readonly ILogger<PrinterSnmpService> _logger;
    private readonly string _community;
    private readonly int _timeoutMs;

    // Standard MIB-II OIDs.
    private static readonly ObjectIdentifier OidSysDescr = new("1.3.6.1.2.1.1.1.0");
    private static readonly ObjectIdentifier OidSysName = new("1.3.6.1.2.1.1.5.0");
    private static readonly ObjectIdentifier OidSysContact = new("1.3.6.1.2.1.1.4.0");
    private static readonly ObjectIdentifier OidSysLocation = new("1.3.6.1.2.1.1.6.0");

    // Zebra enterprise MIB OIDs.
    private static readonly ObjectIdentifier OidZebraSerial = new("1.3.6.1.4.1.10642.1.3.0");
    private static readonly ObjectIdentifier OidZebraFirmware = new("1.3.6.1.4.1.10642.1.10.0");

    // Standard dot3 MIB for MAC address.
    private static readonly ObjectIdentifier OidPhysAddress = new("1.3.6.1.2.1.2.2.1.6.1");

    public PrinterSnmpService(ILogger<PrinterSnmpService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _community = configuration.GetValue<string>("PrinterMonitoring:SnmpCommunity") ?? "public";
        _timeoutMs = configuration.GetValue<int>("PrinterMonitoring:SnmpTimeoutMs", 3000);
    }

    /// <summary>
    /// Queries a printer via SNMP and returns sysinfo fields.
    /// Returns null if the query fails entirely.
    /// </summary>
    public async Task<PrinterSysInfo?> QueryAsync(string ipAddress, CancellationToken ct = default)
    {
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), 161);
            var community = new OctetString(_community);

            var oids = new List<Variable>
            {
                new(OidSysDescr),
                new(OidSysName),
                new(OidSysContact),
                new(OidSysLocation),
                new(OidZebraSerial),
                new(OidZebraFirmware),
                new(OidPhysAddress),
            };

            using var timeoutCts = new CancellationTokenSource(_timeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

            var results = await Messenger.GetAsync(
                VersionCode.V2,
                endpoint,
                community,
                oids,
                linkedCts.Token);

            var info = new PrinterSysInfo();

            foreach (var variable in results)
            {
                var data = variable.Data;
                if (data == null || data is Null || data is NoSuchObject || data is NoSuchInstance)
                    continue;

                var value = data.ToString().Trim();

                if (variable.Id == OidSysDescr)
                {
                    info.Description = value;
                    if (string.IsNullOrEmpty(info.FirmwareVersion))
                        info.FirmwareVersion = ExtractFirmwareFromDescription(value);
                }
                else if (variable.Id == OidSysName)
                {
                    info.Hostname = value;
                }
                else if (variable.Id == OidSysContact)
                {
                    info.Contact = value;
                }
                else if (variable.Id == OidSysLocation)
                {
                    info.Location = value;
                }
                else if (variable.Id == OidZebraSerial)
                {
                    info.SerialNumber = value;
                }
                else if (variable.Id == OidZebraFirmware)
                {
                    info.FirmwareVersion = value;
                }
                else if (variable.Id == OidPhysAddress)
                {
                    info.MacAddress = FormatMacAddress(data);
                }
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SNMP query failed for {Ip}", ipAddress);
            return null;
        }
    }

    private static string? ExtractFirmwareFromDescription(string description)
    {
        var fwIndex = description.IndexOf("FW:", StringComparison.OrdinalIgnoreCase);
        if (fwIndex >= 0)
            return description[(fwIndex + 3)..].Trim();

        fwIndex = description.IndexOf("Firmware:", StringComparison.OrdinalIgnoreCase);
        if (fwIndex >= 0)
            return description[(fwIndex + 9)..].Trim();

        return null;
    }

    private static string? FormatMacAddress(ISnmpData data)
    {
        if (data is OctetString octet && octet.GetRaw().Length == 6)
        {
            var bytes = octet.GetRaw();
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }
        return data.ToString();
    }
}

/// <summary>
/// The printer identity fields SNMP can fill in when the device answers.
/// </summary>
public class PrinterSysInfo
{
    public string? Description { get; set; }
    public string? Hostname { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? MacAddress { get; set; }
    public string? Location { get; set; }
    public string? Contact { get; set; }
}
