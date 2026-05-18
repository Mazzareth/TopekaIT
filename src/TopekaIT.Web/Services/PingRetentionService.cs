using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

/// <summary>
/// Periodically purges old operational telemetry rows to keep the databases lean.
/// Default retention: 30 days. Runs once per day.
/// </summary>
public class PingRetentionService : BackgroundService
{
    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<PingRetentionService> _logger;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _interval;

    public PingRetentionService(
        IDbContextFactory<MasterDbContext> masterFactory,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<PingRetentionService> logger,
        IConfiguration configuration)
    {
        _masterFactory = masterFactory;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
        var retentionDays = configuration.GetValue<int?>("OperationsRetention:RetentionDays")
            ?? configuration.GetValue<int>("PrinterMonitoring:RetentionDays", 30);
        _retention = TimeSpan.FromDays(retentionDays);
        _interval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Operations retention service started. Retention: {Days} days", (int)_retention.TotalDays);

        // Give startup migrations and hosted service registration time to settle before deleting old telemetry.
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTimeOffset.UtcNow - _retention;
                await using var masterDb = await _masterFactory.CreateDbContextAsync(stoppingToken);
                var divisions = await masterDb.Divisions.AsNoTracking().ToListAsync(stoppingToken);
                var lantronixSamplesPurged = await new LantronixDeviceRepository(_masterFactory)
                    .PurgeSamplesOlderThanAsync(cutoff, stoppingToken);
                var pingSamplesPurged = 0;
                var printerEventsPurged = 0;

                foreach (var division in divisions)
                {
                    var factory = new DirectDivisionDbContextFactory(division.ConnectionString, _dataProtectionProvider);
                    var pingHistory = new PingHistoryRepository(factory);
                    var printerEvents = new PrinterEventRepository(factory);
                    await pingHistory.PurgeOlderThanAsync(cutoff, stoppingToken);
                    pingSamplesPurged++;
                    printerEventsPurged += await printerEvents.PurgeEventsOlderThanAsync(cutoff, stoppingToken);
                }
                _logger.LogInformation(
                    "Operations retention sweep completed. Divisions: {DivisionCount}, ping tables swept: {PingSweeps}, printer events purged: {PrinterEventsPurged}, Lantronix samples purged: {LantronixSamplesPurged}",
                    divisions.Count,
                    pingSamplesPurged,
                    printerEventsPurged,
                    lantronixSamplesPurged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during operations retention sweep.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
