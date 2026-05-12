namespace TopekaIT.Core.Domain.Entities;

public class LantronixDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? DivisionId { get; set; }
    public Division? Division { get; set; }
    public string Hostname { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 10001;
    public string PollCommand { get; set; } = LantronixDeviceDefaults.InventoryCommand;
    public string DeviceType { get; set; } = "Lantronix XPort";
    public string? SerialSettings { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastPollAt { get; set; }
    public bool? LastPollSucceeded { get; set; }
    public int? LastLatencyMs { get; set; }
    public string? LastFailureReason { get; set; }
    public decimal? LastFuelVolume { get; set; }
    public decimal? LastTcVolume { get; set; }
    public decimal? LastUllage { get; set; }
    public decimal? LastHeight { get; set; }
    public decimal? LastWater { get; set; }
    public decimal? LastTemperature { get; set; }
}

public static class LantronixDeviceDefaults
{
    public const string InventoryCommand = "I20100";
}
