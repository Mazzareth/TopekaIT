using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class PrinterEventRepositoryTests
{
    [Fact]
    public async Task GetActiveIncidentsAsync_ReturnsOnlyUnsuppressedAlertsSeenWithinTwoDays()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"printer-incidents-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options))
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
            new TestTenantContext());

        var incidents = await repo.GetActiveIncidentsAsync();
        var historicalErrors = await repo.GetErrorsAsync(0, now.AddDays(-7), now.AddDays(-4));

        var incident = Assert.Single(incidents);
        Assert.Equal("RECENT_ALERT", incident.AlertKey);
        Assert.Equal("Recent Printer", incident.PrinterName);

        var historicalError = Assert.Single(historicalErrors);
        Assert.Equal("old", historicalError.PrinterId);
    }

    private static Printer Printer(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Department = "Dock",
        IpAddress = $"10.0.0.{id.Length}",
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

    private sealed class TestDivisionDbContextFactory : IDivisionDbContextFactory
    {
        private readonly DbContextOptions<TopekaDbContext> _options;

        public TestDivisionDbContextFactory(DbContextOptions<TopekaDbContext> options)
        {
            _options = options;
        }

        public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new TopekaDbContext(_options));
    }

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
