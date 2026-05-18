using System.Net.NetworkInformation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

public class PrinterMonitoringService : BackgroundService
{
    private readonly IDbContextFactory<MasterDbContext> _masterFactory;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly PrinterSnmpService _snmpService;
    private readonly ILogger<PrinterMonitoringService> _logger;

    // SNMP sysinfo is intentionally refreshed less often than ping status because the printer MIB calls are slower and only enrich inventory metadata.
    private const int SnmpRefreshEveryNCycles = 120;
    private int _cycleCount;

    public PrinterMonitoringService(
        IDbContextFactory<MasterDbContext> masterFactory,
        IDataProtectionProvider dataProtectionProvider,
        PrinterSnmpService snmpService,
        ILogger<PrinterMonitoringService> logger)
    {
        _masterFactory = masterFactory;
        _dataProtectionProvider = dataProtectionProvider;
        _snmpService = snmpService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Printer Monitoring Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPrintersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred executing printer monitoring check.");
            }

            _cycleCount++;

            // Keep the poll interval short enough for live status without making every configured printer a constant network target.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Printer Monitoring Service is stopping.");
    }

    private async Task CheckPrintersAsync(CancellationToken stoppingToken)
    {
        await using var masterDb = await _masterFactory.CreateDbContextAsync(stoppingToken);
        var divisions = await masterDb.Divisions.AsNoTracking().OrderBy(d => d.Name).ToListAsync(stoppingToken);

        foreach (var division in divisions)
        {
            await CheckDivisionAsync(division, stoppingToken);
        }
    }

    private async Task CheckDivisionAsync(Division division, CancellationToken stoppingToken)
    {
        var factory = new DirectDivisionDbContextFactory(division.ConnectionString, _dataProtectionProvider);
        var printerService = new PrinterService(new PrinterRepository(factory));
        var activityService = new ActivityService(new ActivityRepository(factory));
        var pingHistoryRepo = new PingHistoryRepository(factory);
        var allPrinters = await printerService.GetAllAsync(stoppingToken);
        var t8000Printers = allPrinters.Where(p => PrinterModels.SupportsLogging(p.Model)).ToList();

        if (!t8000Printers.Any())
            return;

        var runSnmp = _cycleCount % SnmpRefreshEveryNCycles == 0;

        using var ping = new Ping();

        foreach (var printer in t8000Printers)
        {
            if (string.IsNullOrWhiteSpace(printer.IpAddress)) continue;

            var sample = new PingSample
            {
                PrinterId = printer.Id,
                Timestamp = DateTimeOffset.UtcNow,
            };

            try
            {
                var reply = await ping.SendPingAsync(printer.IpAddress, 3000);
                var isUp = reply.Status == IPStatus.Success;

                sample.Success = isUp;
                sample.LatencyMs = isUp ? (int)reply.RoundtripTime : null;
                sample.FailureReason = isUp ? null : reply.Status.ToString();

                printer.LastPingAt = sample.Timestamp;
                printer.LastLatencyMs = sample.LatencyMs;
                printer.ConsecutiveFailures = isUp ? 0 : printer.ConsecutiveFailures + 1;

                // Activity events are transition-only so routine ping samples do not flood the visible activity feed.
                var newStatus = isUp ? PrinterStatus.Up : PrinterStatus.Down;
                if (printer.Status != newStatus)
                {
                    _logger.LogInformation("Printer {Name} ({Ip}) in {Division} status changed from {Old} to {New}",
                        printer.Name, printer.IpAddress, division.Id, printer.Status, newStatus);

                    var direction = isUp ? "recovered (Up)" : "went down (Down)";
                    await activityService.PushAsync("printer",
                        $"{printer.Name} {direction} — latency {(isUp ? $"{reply.RoundtripTime}ms" : "N/A")}",
                        stoppingToken);

                    printer.Status = newStatus;
                }

                await printerService.UpdateAsync(printer, stoppingToken);
            }
            catch (PingException ex)
            {
                _logger.LogWarning(ex, "Ping failed for printer {Name} at {Ip}", printer.Name, printer.IpAddress);

                sample.Success = false;
                sample.FailureReason = ex.GetType().Name;

                printer.LastPingAt = sample.Timestamp;
                printer.LastLatencyMs = null;
                printer.ConsecutiveFailures += 1;

                if (printer.Status != PrinterStatus.Down)
                {
                    _logger.LogInformation("Printer {Name} ({Ip}) status changed from {Old} to Down (exception)",
                        printer.Name, printer.IpAddress, printer.Status);

                    await activityService.PushAsync("printer",
                        $"{printer.Name} went down (Down) — ping exception: {ex.Message}",
                        stoppingToken);

                    printer.Status = PrinterStatus.Down;
                }

                await printerService.UpdateAsync(printer, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking printer {Name} at {Ip}", printer.Name, printer.IpAddress);

                // The sample may contain partial state after unexpected failures, so avoid persisting it as telemetry.
                continue;
            }

            try
            {
                await pingHistoryRepo.AddAsync(sample, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist PingSample for printer {Name}", printer.Name);
            }

            // Query SNMP metadata only on scheduled cycles and only while the printer is reachable.
            if (runSnmp && printer.Status == PrinterStatus.Up)
            {
                try
                {
                    var sysInfo = await _snmpService.QueryAsync(printer.IpAddress, stoppingToken);
                    if (sysInfo != null)
                    {
                        var changed = false;

                        if (!string.IsNullOrEmpty(sysInfo.SerialNumber) && sysInfo.SerialNumber != printer.SerialNumber)
                        {
                            printer.SerialNumber = sysInfo.SerialNumber;
                            changed = true;
                        }
                        if (!string.IsNullOrEmpty(sysInfo.FirmwareVersion) && sysInfo.FirmwareVersion != printer.FirmwareVersion)
                        {
                            printer.FirmwareVersion = sysInfo.FirmwareVersion;
                            changed = true;
                        }
                        if (!string.IsNullOrEmpty(sysInfo.MacAddress) && sysInfo.MacAddress != printer.MacAddress)
                        {
                            printer.MacAddress = sysInfo.MacAddress;
                            changed = true;
                        }
                        if (!string.IsNullOrEmpty(sysInfo.Location) && sysInfo.Location != printer.Location)
                        {
                            printer.Location = sysInfo.Location;
                            changed = true;
                        }
                        if (!string.IsNullOrEmpty(sysInfo.Contact) && sysInfo.Contact != printer.Contact)
                        {
                            printer.Contact = sysInfo.Contact;
                            changed = true;
                        }

                        if (changed)
                        {
                            _logger.LogInformation("SNMP sysinfo updated for printer {Name}: SN={Serial}, FW={Firmware}",
                                printer.Name, printer.SerialNumber, printer.FirmwareVersion);
                            await printerService.UpdateAsync(printer, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "SNMP sysinfo refresh failed for printer {Name}", printer.Name);
                }
            }
        }
    }
}
