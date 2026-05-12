using System.Globalization;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class LantronixDeviceService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> PollLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex InventoryRowRegex = new(
        @"^\s*(?<tank>\d+)\s+(?<product>.+?)\s+(?<volume>-?\d+(?:\.\d+)?)\s+(?<tc>-?\d+(?:\.\d+)?)\s+(?<ullage>-?\d+(?:\.\d+)?)\s+(?<height>-?\d+(?:\.\d+)?)\s+(?<water>-?\d+(?:\.\d+)?)\s+(?<temp>-?\d+(?:\.\d+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ILantronixDeviceRepository _repo;
    private readonly ILantronixFuelClient _client;
    private readonly TimeSpan _pollTimeout;

    public LantronixDeviceService(ILantronixDeviceRepository repo, ILantronixFuelClient client)
    {
        _repo = repo;
        _client = client;
        _pollTimeout = TimeSpan.FromSeconds(6);
    }

    public Task<IReadOnlyList<LantronixDevice>> GetAllAsync(CancellationToken ct = default) =>
        _repo.GetAllAsync(ct);

    public Task<IReadOnlyList<LantronixPollSample>> GetSamplesAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxSamples = 500,
        CancellationToken ct = default) =>
        _repo.GetSamplesAsync(deviceId, from, to, maxSamples, ct);

    public Task<IReadOnlyList<LantronixPollSample>> GetRecentSamplesAsync(
        string deviceId,
        int count,
        CancellationToken ct = default) =>
        _repo.GetRecentSamplesAsync(deviceId, count, ct);

    public async Task<LantronixDevicePollResult> PollAsync(string deviceId, CancellationToken ct = default)
    {
        var pollLock = PollLocks.GetOrAdd(deviceId, _ => new SemaphoreSlim(1, 1));
        await pollLock.WaitAsync(ct);
        try
        {
            var device = await _repo.GetByIdAsync(deviceId, ct)
                ?? throw new InvalidOperationException("Lantronix device was not found.");

            var transport = await _client.PollInventoryAsync(device, _pollTimeout, ct);
            var snapshot = transport.Success && !string.IsNullOrWhiteSpace(transport.RawResponse)
                ? ParseInventory(transport.RawResponse)
                : null;

            var success = transport.Success && snapshot != null;
            var failureReason = success
                ? null
                : transport.FailureReason ?? "Inventory report was not found in response.";

            var sample = new LantronixPollSample
            {
                DeviceId = device.Id,
                Timestamp = DateTimeOffset.UtcNow,
                Success = success,
                LatencyMs = transport.LatencyMs,
                FailureReason = Truncate(failureReason, 256),
                RawResponse = Truncate(transport.RawResponse, 4000),
            };

            if (snapshot != null)
            {
                sample.ReportName = "IN-TANK INVENTORY";
                sample.TankNumber = snapshot.TankNumber;
                sample.Product = snapshot.Product;
                sample.Volume = snapshot.Volume;
                sample.TcVolume = snapshot.TcVolume;
                sample.Ullage = snapshot.Ullage;
                sample.Height = snapshot.Height;
                sample.Water = snapshot.Water;
                sample.Temperature = snapshot.Temperature;
            }

            device.LastPollAt = sample.Timestamp;
            device.LastPollSucceeded = sample.Success;
            device.LastLatencyMs = sample.LatencyMs;
            device.LastFailureReason = sample.FailureReason;

            if (snapshot != null)
            {
                device.LastFuelVolume = snapshot.Volume;
                device.LastTcVolume = snapshot.TcVolume;
                device.LastUllage = snapshot.Ullage;
                device.LastHeight = snapshot.Height;
                device.LastWater = snapshot.Water;
                device.LastTemperature = snapshot.Temperature;
            }

            await _repo.RecordPollAsync(device, sample, ct);
            return new LantronixDevicePollResult(device, sample);
        }
        finally
        {
            pollLock.Release();
        }
    }

    public static LantronixInventorySnapshot? ParseInventory(string response)
    {
        var lines = response
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(CleanLine)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var inInventoryReport = false;
        foreach (var line in lines)
        {
            if (line.Contains("IN-TANK INVENTORY", StringComparison.OrdinalIgnoreCase))
            {
                inInventoryReport = true;
                continue;
            }

            if (!inInventoryReport && lines.Any(l => l.Contains("IN-TANK INVENTORY", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (line.Contains("TANK PRODUCT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var match = InventoryRowRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            return new LantronixInventorySnapshot(
                int.Parse(match.Groups["tank"].Value, CultureInfo.InvariantCulture),
                match.Groups["product"].Value.Trim(),
                ParseDecimal(match.Groups["volume"].Value),
                ParseDecimal(match.Groups["tc"].Value),
                ParseDecimal(match.Groups["ullage"].Value),
                ParseDecimal(match.Groups["height"].Value),
                ParseDecimal(match.Groups["water"].Value),
                ParseDecimal(match.Groups["temp"].Value));
        }

        return null;
    }

    private static decimal ParseDecimal(string value) =>
        decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static string CleanLine(string line) =>
        new(line.Where(ch => !char.IsControl(ch) || char.IsWhiteSpace(ch)).ToArray());

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength];
    }
}

public sealed record LantronixDevicePollResult(LantronixDevice Device, LantronixPollSample Sample);

public sealed record LantronixInventorySnapshot(
    int TankNumber,
    string Product,
    decimal Volume,
    decimal TcVolume,
    decimal Ullage,
    decimal Height,
    decimal Water,
    decimal Temperature);
