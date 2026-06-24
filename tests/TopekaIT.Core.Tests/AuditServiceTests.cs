using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Audits should count what was scanned, what was missing, and what did not match expectations.
/// </summary>
public class AuditServiceTests
{
    [Fact]
    public async Task StartRecordAndCompleteAudit_FlagsUnexpectedAndMissingCounts()
    {
        var expected = Asset("asset-1", "TAG-1", "holder-1", "locker-1");
        var missing = Asset("asset-2", "TAG-2", "holder-2", "locker-2");
        var repo = new FakeAuditRepository();
        var service = new AuditService(repo, new AssetService(new FakeAssetRepository(expected, missing), new ActivityService(new FakeActivityRepository())));

        var session = await service.StartSessionAsync("6IA", "manager-1");
        var scanned = await service.RecordScanAsync(session.Id, "TAG-1", "holder-1", "locker-1");
        var unexpected = await service.RecordScanAsync(session.Id, "UNKNOWN");
        var completed = await service.CompleteSessionAsync(session.Id, "Done");

        Assert.Equal(AuditResult.Expected, scanned.Result);
        Assert.Equal(AuditResult.Unexpected, unexpected.Result);
        Assert.NotNull(completed);
        Assert.NotNull(completed!.CompletedAt);
        Assert.Equal(2, completed.TotalScanned);
        Assert.Equal(1, completed.MissingCount);
        Assert.Equal(1, completed.UnexpectedCount);
        Assert.Equal(2, completed.Discrepancies);
        Assert.Contains(repo.Entries, e => e.AssetId == missing.Id && e.Result == AuditResult.Missing);
    }

    [Fact]
    public async Task RecordScan_RecordsHolderAndLockerDiscrepancy()
    {
        var asset = Asset("asset-1", "TAG-1", "expected-holder", "expected-locker");
        var repo = new FakeAuditRepository();
        var service = new AuditService(repo, new AssetService(new FakeAssetRepository(asset), new ActivityService(new FakeActivityRepository())));
        var session = await service.StartSessionAsync("6IA", "manager-1");

        var entry = await service.RecordScanAsync(session.Id, "TAG-1", "actual-holder", "actual-locker");

        Assert.Equal(AuditResult.Discrepancy, entry.Result);
        Assert.True(entry.IsDiscrepancy);
        Assert.Equal("expected-holder", entry.ExpectedHolderId);
        Assert.Equal("actual-holder", entry.ActualHolderId);
        Assert.Equal("expected-locker", entry.ExpectedLockerId);
        Assert.Equal("actual-locker", entry.ActualLockerId);
    }

    private static Asset Asset(string id, string tag, string holderId, string lockerId) => new()
    {
        Id = id,
        Tag = tag,
        Serial = tag + "-SN",
        Model = "TC77",
        Category = AssetCategory.PodTc77,
        Type = "pod-tc77",
        Status = AssetStatus.Out,
        Flags = StatusFlags.WithHolder,
        HolderId = holderId,
        LockerId = lockerId,
    };

    private sealed class FakeAuditRepository : IAuditRepository
    {
        public List<AuditSession> Sessions { get; } = new();
        public List<AuditEntry> Entries { get; } = new();

        public Task<AuditSession?> GetSessionAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Sessions.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AuditEntry>>(Entries.Where(e => e.SessionId == sessionId).ToList());

        public Task AddSessionAsync(AuditSession session, CancellationToken ct = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task AddEntryAsync(AuditEntry entry, CancellationToken ct = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task AddEntriesAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default)
        {
            Entries.AddRange(entries);
            return Task.CompletedTask;
        }

        public Task UpdateSessionAsync(AuditSession session, CancellationToken ct = default) => Task.CompletedTask;
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
            => Task.FromResult<IEnumerable<LoanRecord>>(Array.Empty<LoanRecord>());
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityEvent>>(Array.Empty<ActivityEvent>());

        public Task AddAsync(ActivityEvent ev, CancellationToken ct = default) => Task.CompletedTask;
    }
}
