using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Printer event repository tests for raw events, active alert state, and grouped incident reports.
/// </summary>
public class PrinterEventRepositoryTests
{
    [Fact]
    public async Task GetActiveIncidentsAsync_ReturnsOnlyUnsuppressedAlertsSeenWithinTwoDays()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"printer-incidents-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Printers.AddRange(
                Printer("recent", "Recent Printer"),
                Printer("old", "Old Printer"),
                Printer("suppressed", "Suppressed Printer"));

            db.PrinterAlertStates.AddRange(
                Alert(1, "recent", "RECENT_ALERT", now.AddDays(-1)),
                Alert(2, "old", "OLD_ALERT", now.AddDays(-2).AddMinutes(-1)),
                Alert(3, "suppressed", "SUPPRESSED_ALERT", now, suppressed: true));

            db.PrinterEvents.Add(new PrinterEvent
            {
                Id = 1,
                PrinterId = "old",
                Timestamp = now.AddDays(-5),
                EventType = "Error",
                RawMessage = "Message=Old printer error, Severity: Critical",
                Severity = "Error",
            });

            await db.SaveChangesAsync();
        }

        var repo = new PrinterEventRepository(
            new TestDivisionDbContextFactory(options),
            new TestDivisionRepository(),
            new TestTenantContext(),
            TestDataProtection.Provider);

        var incidents = await repo.GetActiveIncidentsAsync();
        var historicalErrors = await repo.GetErrorsAsync(0, now.AddDays(-7), now.AddDays(-4));

        var incident = Assert.Single(incidents);
        Assert.Equal("RECENT_ALERT", incident.AlertKey);
        Assert.Equal("Recent Printer", incident.PrinterName);

        var historicalError = Assert.Single(historicalErrors);
        Assert.Equal("old", historicalError.PrinterId);
    }

    [Fact]
    public async Task PurgeEventsOlderThanAsync_RemovesOnlyRawEventsAndKeepsAlertStates()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"printer-event-retention-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Printers.Add(Printer("recent", "Recent Printer"));
            db.PrinterEvents.AddRange(
                new PrinterEvent { Id = 1, PrinterId = "recent", Timestamp = now.AddDays(-31), EventType = "Error", RawMessage = "old", Severity = "Error" },
                new PrinterEvent { Id = 2, PrinterId = "recent", Timestamp = now.AddDays(-1), EventType = "Error", RawMessage = "recent", Severity = "Error" });
            db.PrinterAlertStates.Add(Alert(1, "recent", "ACTIVE_ALERT", now));
            await db.SaveChangesAsync();
        }

        var repo = new PrinterEventRepository(new TestDivisionDbContextFactory(options));

        var purged = await repo.PurgeEventsOlderThanAsync(now.AddDays(-30));

        await using var verify = new TopekaDbContext(options, TestDataProtection.Provider);
        Assert.Equal(1, purged);
        Assert.Single(verify.PrinterEvents);
        Assert.Single(verify.PrinterAlertStates);
    }

    [Fact]
    public async Task GetByPrinterAsync_AppliesDateRangeBeforeLimit()
    {
        var now = new DateTimeOffset(2026, 5, 14, 18, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"printer-event-range-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Printers.AddRange(
                Printer("target", "Target Printer"),
                Printer("other", "Other Printer"));
            db.PrinterEvents.AddRange(
                Event(1, "target", now.AddHours(-3), "outside old"),
                Event(2, "target", now.AddMinutes(-90), "first in range"),
                Event(3, "target", now.AddMinutes(-45), "second in range"),
                Event(4, "target", now.AddMinutes(-10), "outside recent"),
                Event(5, "other", now.AddMinutes(-60), "other printer"));
            await db.SaveChangesAsync();
        }

        var repo = new PrinterEventRepository(new TestDivisionDbContextFactory(options));

        var events = await repo.GetByPrinterAsync("target", 0, now.AddHours(-2), now.AddMinutes(-30));
        var limited = await repo.GetByPrinterAsync("target", 1, now.AddHours(-2), now.AddMinutes(-30));

        Assert.Equal(new long[] { 3, 2 }, events.Select(e => e.Id));
        var limitedEvent = Assert.Single(limited);
        Assert.Equal(3, limitedEvent.Id);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsSelectedPrintersWithinDateRange()
    {
        var now = new DateTimeOffset(2026, 5, 14, 18, 0, 0, TimeSpan.Zero);
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"printer-log-export-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Printers.AddRange(
                Printer("p1", "Dock Printer"),
                Printer("p2", "Freezer Printer"),
                Printer("p3", "Office Printer"));
            db.PrinterEvents.AddRange(
                Event(1, "p1", now.AddMinutes(-90), "old enough"),
                Event(2, "p1", now.AddMinutes(-20), "dock message"),
                Event(3, "p2", now.AddMinutes(-10), "freezer message"),
                Event(4, "p3", now.AddMinutes(-5), "not selected"));
            await db.SaveChangesAsync();
        }

        var repo = new PrinterEventRepository(new TestDivisionDbContextFactory(options));

        var logs = await repo.GetLogsAsync(new[] { "p1", "p2" }, now.AddMinutes(-30), now);

        Assert.Equal(new long[] { 3, 2 }, logs.Select(e => e.Id));
        Assert.Contains(logs, e => e.PrinterName == "Dock Printer" && e.RawMessage == "dock message");
        Assert.Contains(logs, e => e.PrinterName == "Freezer Printer" && e.RawMessage == "freezer message");
    }

    private static Printer Printer(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Department = "Dock",
        IpAddress = $"10.0.0.{id.Length}",
    };

    private static PrinterEvent Event(long id, string printerId, DateTimeOffset timestamp, string message) => new()
    {
        Id = id,
        PrinterId = printerId,
        Timestamp = timestamp,
        EventType = "Info",
        RawMessage = message,
        Severity = "Info",
    };

    private static PrinterAlertState Alert(long id, string printerId, string alertKey, DateTimeOffset lastSeenAt, bool suppressed = false) => new()
    {
        Id = id,
        PrinterId = printerId,
        AlertKey = alertKey,
        AlertTitle = alertKey,
        AlertCategory = "Printer Alert",
        FriendlyMessage = alertKey,
        Severity = "Critical",
        FirstSeenAt = lastSeenAt.AddMinutes(-5),
        LastSeenAt = lastSeenAt,
        LastEventId = id,
        OccurrenceCount = 1,
        BlipSuppressed = suppressed,
    };
    private sealed class TestDivisionRepository : IDivisionRepository
    {
        private static readonly Division TestDivision = new()
        {
            Id = "topeka",
            Name = "Topeka",
            ConnectionString = "in-memory",
        };

        public Task<IReadOnlyList<Division>> GetAllAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Division>>(new[] { TestDivision });

        public Task<Division?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult<Division?>(TestDivision);

        public Task AddAsync(Division division, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Division division, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public string? DivisionId => "topeka";
        public string? ConnectionString => "in-memory";
        public bool IsResolved => true;
        public void SetDivision(string divisionId, string connectionString) { }
    }
}
