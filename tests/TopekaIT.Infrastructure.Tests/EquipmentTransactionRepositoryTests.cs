using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

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
                a.Flags = StatusFlags.WithHolder;
            });

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
    }
}
