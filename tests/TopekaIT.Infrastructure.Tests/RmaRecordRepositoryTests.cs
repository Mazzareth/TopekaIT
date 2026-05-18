using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class RmaRecordRepositoryTests
{
    [Fact]
    public async Task GetActiveAsync_ReturnsOpenRmaRecordsOldestFirst()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"rma-active-{Guid.NewGuid()}")
            .Options;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Assets.AddRange(
                Asset("asset-old", "OLD"),
                Asset("asset-new", "NEW"),
                Asset("asset-done", "DONE"));
            db.RmaRecords.AddRange(
                new RmaRecord { Id = "rma-old", AssetId = "asset-old", AssetTag = "OLD", DateSubmitted = now.AddDays(-20) },
                new RmaRecord { Id = "rma-new", AssetId = "asset-new", AssetTag = "NEW", DateSubmitted = now.AddDays(-2) },
                new RmaRecord { Id = "rma-done", AssetId = "asset-done", AssetTag = "DONE", DateSubmitted = now.AddDays(-30), IsReceived = true, ReceivedDate = now.AddDays(-1) });
            await db.SaveChangesAsync();
        }

        var repo = new RmaRecordRepository(new TestDivisionDbContextFactory(options));

        var active = await repo.GetActiveAsync();

        Assert.Collection(active,
            first => Assert.Equal("rma-old", first.Id),
            second => Assert.Equal("rma-new", second.Id));
    }

    private static Asset Asset(string id, string tag) => new()
    {
        Id = id,
        Tag = tag,
        Serial = tag,
        Model = "WT6000",
    };

    private sealed class TestDivisionDbContextFactory : IDivisionDbContextFactory
    {
        private readonly DbContextOptions<TopekaDbContext> _options;

        public TestDivisionDbContextFactory(DbContextOptions<TopekaDbContext> options)
        {
            _options = options;
        }

        public Task<TopekaDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
            Task.FromResult(new TopekaDbContext(_options, TestDataProtection.Provider));
    }
}
