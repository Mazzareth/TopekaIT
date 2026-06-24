using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Services;

/// <summary>
/// Background polling loop for Lantronix devices. It stays gentle: poll, record, sleep, repeat.
/// </summary>
public class LantronixAutoPollService : BackgroundService
{
    private const string DefaultDeviceId = "lan-6i-fuel";
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LantronixAutoPollService> _logger;
    private readonly LantronixAutoPollOptions _options;

    public LantronixAutoPollService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<LantronixAutoPollService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = configuration.GetSection("LantronixAutoPoll").Get<LantronixAutoPollOptions>() ?? new();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (!_options.Enabled)
        {
            _logger.LogInformation("Lantronix auto poll is disabled.");
            return;
        }

        var deviceId = string.IsNullOrWhiteSpace(_options.DeviceId)
            ? DefaultDeviceId
            : _options.DeviceId.Trim();
        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));

        _logger.LogInformation(
            "Lantronix auto poll started for {DeviceId}. Interval: {IntervalMinutes} minutes",
            deviceId,
            interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = await PollIfDueAsync(deviceId, interval, stoppingToken);
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lantronix auto poll failed unexpectedly.");
                await Task.Delay(ErrorDelay, stoppingToken);
            }
        }
    }

    private async Task<TimeSpan> PollIfDueAsync(string deviceId, TimeSpan interval, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<LantronixDeviceService>();
        var device = (await service.GetAllAsync(ct))
            .FirstOrDefault(d => d.Id.Equals(deviceId, StringComparison.OrdinalIgnoreCase));

        if (device == null)
        {
            _logger.LogWarning("Lantronix auto poll skipped because device {DeviceId} was not found.", deviceId);
            return interval;
        }

        var delay = GetDelayUntilDue(device, interval);
        if (delay > TimeSpan.Zero)
        {
            _logger.LogDebug(
                "Lantronix auto poll skipped for {DeviceId}; next poll is due in {DelayMinutes:n1} minutes.",
                deviceId,
                delay.TotalMinutes);
            return delay;
        }

        var result = await service.PollAsync(device.Id, ct);
        if (result.Sample.Success)
        {
            _logger.LogInformation(
                "Lantronix auto poll succeeded for {DeviceId}. Volume: {Volume} gal",
                device.Id,
                result.Sample.Volume);
        }
        else
        {
            _logger.LogWarning(
                "Lantronix auto poll failed for {DeviceId}: {FailureReason}",
                device.Id,
                result.Sample.FailureReason);
        }

        return interval;
    }

    private static TimeSpan GetDelayUntilDue(LantronixDevice device, TimeSpan interval)
    {
        if (!device.LastPollAt.HasValue)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTimeOffset.UtcNow - device.LastPollAt.Value;
        return elapsed >= interval
            ? TimeSpan.Zero
            : interval - elapsed;
    }

    private sealed class LantronixAutoPollOptions
    {
        public bool Enabled { get; set; } = true;
        public string DeviceId { get; set; } = DefaultDeviceId;
        public double IntervalMinutes { get; set; } = DefaultInterval.TotalMinutes;
    }
}
