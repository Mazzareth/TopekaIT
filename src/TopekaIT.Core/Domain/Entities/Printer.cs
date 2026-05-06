using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class Printer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Model { get; set; } = PrinterModels.T8000;
    public string IpAddress { get; set; } = "";
    public PrinterStatus Status { get; set; }

    // Derived live state — updated by PrinterMonitoringService on every ping
    public DateTimeOffset? LastPingAt { get; set; }
    public int? LastLatencyMs { get; set; }
    public int ConsecutiveFailures { get; set; }

    // T8000 sysinfo fields (populated later via SNMP/npsh)
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? MacAddress { get; set; }
    public string? Location { get; set; }
    public string? Contact { get; set; }
}
