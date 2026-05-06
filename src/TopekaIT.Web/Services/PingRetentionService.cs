using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

/// <summary>
/// Periodically purges old PingSample rows to keep the database lean.
/// Default retention: 30 days. Runs once per day.
/// </summary>
public class PingRetentionService : BackgroundService
{
    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly ILogger<PingRetentionService> _logger;
    private readonly TimeSpan _retention;
    private readonly TimeSpan _interval;

    public PingRetentionService(IDbContextFactory<MasterDbContext> masterFactory, ILogger<PingRetentionService> logger, IConfiguration configuration)
    {
        _masterFactory = masterFactory;
        _logger = logger;
        var retentionDays = configuration.GetValue<int>("PrinterMonitoring:RetentionDays", 30);
        _retention = TimeSpan.FromDays(retentionDays);
        _interval = TimeSpan.FromHours(24);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Ping Retention Service started. Retention: {Days} days", (int)_retention.TotalDays);

        // Wait 5 minutes after startup before first sweep
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTimeOffset.UtcNow - _retention;
                await using var masterDb = await _masterFactory.CreateDbContextAsync(stoppingToken);
                var divisions = await masterDb.Divisions.AsNoTracking().ToListAsync(stoppingToken);
                foreach (var division in divisions)
                {
                    var pingHistory = new PingHistoryRepository(new DirectDivisionDbContextFactory(division.ConnectionString));
                    await pingHistory.PurgeOlderThanAsync(cutoff, stoppingToken);
                }
                _logger.LogInformation("Ping retention sweep completed. Purged samples older than {Days} days.", (int)_retention.TotalDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ping retention sweep.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
