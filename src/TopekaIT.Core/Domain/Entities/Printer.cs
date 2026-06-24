using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A printer the portal watches and sometimes configures. Live status belongs to monitoring, while identity stays here.
/// </summary>
public class Printer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public string Model { get; set; } = PrinterModels.T8000;
    public string IpAddress { get; set; } = "";
    public PrinterStatus Status { get; set; }

    // PrinterMonitoringService owns these live ping values.
    public DateTimeOffset? LastPingAt { get; set; }
    public int? LastLatencyMs { get; set; }
    public int ConsecutiveFailures { get; set; }

    // Populated from printer sysinfo when setup or monitoring can read it.
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? MacAddress { get; set; }
    public string? Location { get; set; }
    public string? Contact { get; set; }
}
