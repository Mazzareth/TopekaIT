using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Protects asset scan matching. Physical RFID/NFC stickers now belong to lockers, not assets.
/// </summary>
public class AssetServiceScanTests
{
    [Theory]
    [InlineData("TAG-001")]
    [InlineData("serial:SN-001")]
    [InlineData("sn:SN-001")]
    [InlineData("asset-1")]
    public async Task FindByScanAsync_MatchesAssetIdentifiers(string scanValue)
    {
        var asset = new Asset
        {
            Id = "asset-1",
            Tag = "TAG-001",
            Serial = "SN-001",
            Model = "TC77",
            Category = AssetCategory.PodTc77,
        };
        var service = new AssetService(new FakeAssetRepository(asset), new ActivityService(new FakeActivityRepository()));

        var match = await service.FindByScanAsync(scanValue);

        Assert.NotNull(match);
        Assert.Equal(asset.Id, match!.Id);
    }

    [Theory]
    [InlineData("rfid:TAG-001")]
    [InlineData("nfc:TAG-001")]
    [InlineData("ntag:TAG-001")]
    [InlineData("uid:TAG-001")]
    [InlineData("https://local.asset/scan?nfc=TAG-001")]
    public async Task FindByScanAsync_DoesNotMatchRfidOrNfcPayloads(string scanValue)
    {
        var asset = new Asset
        {
            Id = "asset-1",
            Tag = "TAG-001",
            Serial = "SN-001",
            Model = "TC77",
            Category = AssetCategory.PodTc77,
        };
        var service = new AssetService(new FakeAssetRepository(asset), new ActivityService(new FakeActivityRepository()));

        var match = await service.FindByScanAsync(scanValue);

        Assert.Null(match);
    }

    private sealed class FakeAssetRepository : IAssetRepository
    {
        private readonly Dictionary<string, Asset> _assets;

        public FakeAssetRepository(params Asset[] assets)
        {
            _assets = assets.ToDictionary(a => a.Id);
        }

        public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Asset>>(_assets.Values.ToList());

        public Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_assets.GetValueOrDefault(id));

        public Task AddAsync(Asset asset, CancellationToken ct = default)
        {
            _assets[asset.Id] = asset;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Asset asset, CancellationToken ct = default)
        {
            _assets[asset.Id] = asset;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            _assets.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<Asset>>(_assets.Values.Where(a => a.Status == AssetStatus.Spare));

        public Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default)
            => Task.FromResult(Enumerable.Empty<LoanRecord>());
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityEvent>>(Array.Empty<ActivityEvent>());

        public Task AddAsync(ActivityEvent ev, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
