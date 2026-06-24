using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// RMA repository tests for active records and persisted return status.
/// </summary>
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

    [Fact]
    public async Task AddAsync_SavesRecordToDatabase()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"rma-add-{Guid.NewGuid()}")
            .Options;

        var repo = new RmaRecordRepository(new TestDivisionDbContextFactory(options));
        var record = new RmaRecord
        {
            Id = "rma-test",
            AssetId = "asset-test",
            AssetTag = "TAG-TEST",
            DateSubmitted = DateTimeOffset.UtcNow,
            Comments = "Testing",
            Section = "Zebra"
        };

        await repo.AddAsync(record);

        await using var db = new TopekaDbContext(options, TestDataProtection.Provider);
        var loaded = await db.RmaRecords.FindAsync("rma-test");
        Assert.NotNull(loaded);
        Assert.Equal("TAG-TEST", loaded.AssetTag);
        Assert.Equal("Zebra", loaded.Section);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingRecord()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"rma-update-{Guid.NewGuid()}")
            .Options;

        var record = new RmaRecord
        {
            Id = "rma-update-test",
            AssetId = "asset-test",
            AssetTag = "TAG",
            Comments = "Initial"
        };

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.RmaRecords.Add(record);
            await db.SaveChangesAsync();
        }

        var repo = new RmaRecordRepository(new TestDivisionDbContextFactory(options));
        record.Comments = "Updated Comments";
        await repo.UpdateAsync(record);

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            var loaded = await db.RmaRecords.FindAsync("rma-update-test");
            Assert.NotNull(loaded);
            Assert.Equal("Updated Comments", loaded.Comments);
        }
    }

    [Fact]
    public async Task RemoveAsync_DeletesRecordFromDatabase()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"rma-delete-{Guid.NewGuid()}")
            .Options;

        var record = new RmaRecord
        {
            Id = "rma-del",
            AssetId = "asset-test",
            AssetTag = "TAG"
        };

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.RmaRecords.Add(record);
            await db.SaveChangesAsync();
        }

        var repo = new RmaRecordRepository(new TestDivisionDbContextFactory(options));
        await repo.RemoveAsync("rma-del");

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            var loaded = await db.RmaRecords.FindAsync("rma-del");
            Assert.Null(loaded);
        }
    }

    private static Asset Asset(string id, string tag) => new()
    {
        Id = id,
        Tag = tag,
        Serial = tag,
        Model = "WT6000",
    };
}
