using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

/// <summary>
/// Repository tests for station ledger writes, especially asset before/after snapshots.
/// </summary>
public class EquipmentTransactionRepositoryTests
{
    [Fact]
    public async Task RecordMutationAsync_UpdatesAssetAndTransactionTogether()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"equipment-tx-{Guid.NewGuid()}")
            .Options;

        var asset = new Asset
        {
            Id = "asset-1",
            Tag = "TC77-1",
            Serial = "SN-1",
            Model = "TC77",
            Category = AssetCategory.PodTc77,
            Type = "pod-tc77",
            Status = AssetStatus.InCC,
            Flags = StatusFlags.InCC,
        };

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Assets.Add(asset);
            await db.SaveChangesAsync();
        }

        var repo = new EquipmentTransactionRepository(new TestDivisionDbContextFactory(options));
        var result = await repo.RecordMutationAsync(
            asset.Id,
            EquipmentTransactionType.Checkout,
            "6IA",
            "worker-1",
            "manager-1",
            "checkout",
            null,
            null,
            null,
            null,
            "rfid:abc",
            null,
            a =>
            {
                a.Status = AssetStatus.Out;
                a.HolderId = "worker-1";
                a.LockerId = "locker-1";
                a.Flags = StatusFlags.WithHolder;
            },
            metadata: new EquipmentTransactionMetadata(
                "mobile-session-1",
                "WT-123",
                "locker-1",
                "A-01",
                "Worker One"));

        Assert.NotNull(result);
        await using var verify = new TopekaDbContext(options, TestDataProtection.Provider);
        var loadedAsset = await verify.Assets.SingleAsync(a => a.Id == asset.Id);
        var transaction = await verify.EquipmentTransactions.SingleAsync();

        Assert.Equal(AssetStatus.Out, loadedAsset.Status);
        Assert.Equal("worker-1", loadedAsset.HolderId);
        Assert.Equal(StatusFlags.WithHolder, loadedAsset.Flags);
        Assert.Equal(EquipmentTransactionType.Checkout, transaction.Type);
        Assert.Equal("6IA", transaction.DivisionId);
        Assert.Equal(StatusFlags.InCC, transaction.BeforeFlags);
        Assert.Equal(StatusFlags.WithHolder, transaction.AfterFlags);
        Assert.Equal("InCC", transaction.BeforeStatus);
        Assert.Equal("Out", transaction.AfterStatus);
        Assert.Null(transaction.BeforeLockerId);
        Assert.Equal("locker-1", transaction.AfterLockerId);
        Assert.Equal("mobile-session-1", transaction.MobileSessionId);
        Assert.Equal("WT-123", transaction.ReaderDeviceSerial);
        Assert.Equal("locker-1", transaction.ScannedLockerId);
        Assert.Equal("A-01", transaction.LockerNumberSnapshot);
        Assert.Equal("Worker One", transaction.EmployeeNameSnapshot);
    }
}
