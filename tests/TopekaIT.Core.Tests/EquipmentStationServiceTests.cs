using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Station workflow tests: device state changes, tickets/RMA records, and the ledger row need to agree.
/// </summary>
public class EquipmentStationServiceTests
{
    [Fact]
    public async Task CheckoutAsync_SetsHolderStatusFlagsAndRecordsTransaction()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.InCC, StatusFlags.InCC);
        var fixture = Fixture(asset);

        var result = await fixture.Service.CheckoutAsync(Request(asset.Id, "worker-1"));

        Assert.NotNull(result);
        Assert.Equal(AssetStatus.Out, asset.Status);
        Assert.Equal("worker-1", asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.WithHolder));
        Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal(EquipmentTransactionType.Checkout, fixture.Transactions.Transactions[0].Type);
        Assert.Equal(StatusFlags.InCC, fixture.Transactions.Transactions[0].BeforeFlags);
        Assert.Equal(StatusFlags.WithHolder, fixture.Transactions.Transactions[0].AfterFlags);
    }

    [Fact]
    public async Task AssignByManagerAsync_IssuesDeviceAndRecordsManagerAssignment()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.InCC, StatusFlags.InCC);
        var fixture = Fixture(asset);

        var result = await fixture.Service.AssignByManagerAsync(Request(asset.Id, "worker-1", "issued by manager"));

        Assert.NotNull(result);
        Assert.Equal(AssetStatus.Out, asset.Status);
        Assert.Equal("worker-1", asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.WithHolder));
        var transaction = Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal(EquipmentTransactionType.ManagerAssignment, transaction.Type);
        Assert.Equal("worker-1", transaction.EmployeeId);
        Assert.Equal("manager-1", transaction.ActorId);
        Assert.Equal("issued by manager", transaction.Notes);
    }

    [Fact]
    public async Task ConfirmAssignmentAsync_UpdatesLastSeenWithoutClearingHolder()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.Out, StatusFlags.WithHolder);
        asset.HolderId = "worker-1";
        var fixture = Fixture(asset);

        var result = await fixture.Service.ConfirmAssignmentAsync(Request(asset.Id, "worker-1", "monthly confirmation"));

        Assert.NotNull(result);
        Assert.Equal("worker-1", asset.HolderId);
        Assert.Equal(AssetStatus.Out, asset.Status);
        Assert.True(asset.Flags.HasFlag(StatusFlags.WithHolder));
        Assert.NotNull(asset.LastSeenAt);
        Assert.Equal("Employee confirmation", asset.LastSeenLocation);
        var transaction = Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal(EquipmentTransactionType.AssignmentConfirmation, transaction.Type);
        Assert.Equal("worker-1", transaction.AfterHolderId);
    }

    [Fact]
    public async Task CheckoutViaLockerAsync_UsesLockerUserAndRecordsMobileSnapshot()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.InLocker, StatusFlags.InLocker);
        asset.LockerId = "locker-1";
        var fixture = Fixture(asset);
        var request = MobileRequest(asset.Id);

        var result = await fixture.Service.CheckoutViaLockerAsync(request, "locker-1", "A-01");

        Assert.NotNull(result);
        Assert.Equal(AssetStatus.Out, asset.Status);
        Assert.Equal("locker-1", asset.LockerId);
        Assert.Null(asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.WithHolder));
        var transaction = Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal(EquipmentTransactionType.Checkout, transaction.Type);
        Assert.Equal("worker-1", transaction.EmployeeId);
        Assert.Equal("manager-1", transaction.ActorId);
        Assert.Equal("mobile-session-1", transaction.MobileSessionId);
        Assert.Equal("WT-123", transaction.ReaderDeviceSerial);
        Assert.Equal("locker-1", transaction.ScannedLockerId);
        Assert.Equal("A-01", transaction.LockerNumberSnapshot);
        Assert.Equal("Worker One", transaction.EmployeeNameSnapshot);
        Assert.Equal("locker-1", transaction.BeforeLockerId);
        Assert.Equal("locker-1", transaction.AfterLockerId);
    }

    [Fact]
    public async Task CheckinViaLockerAsync_ReturnsDeviceToLockerAndRecordsMobileSnapshot()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.Out, StatusFlags.WithHolder);
        asset.LockerId = "locker-1";
        var fixture = Fixture(asset);
        var request = MobileRequest(asset.Id);

        var result = await fixture.Service.CheckinViaLockerAsync(request, "locker-1", "A-01");

        Assert.NotNull(result);
        Assert.Equal(AssetStatus.InLocker, asset.Status);
        Assert.Equal("locker-1", asset.LockerId);
        Assert.Null(asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.InLocker));
        var transaction = Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal(EquipmentTransactionType.Checkin, transaction.Type);
        Assert.Equal("worker-1", transaction.EmployeeId);
        Assert.Equal("manager-1", transaction.ActorId);
        Assert.Equal("mobile-session-1", transaction.MobileSessionId);
        Assert.Equal("WT-123", transaction.ReaderDeviceSerial);
        Assert.Equal("locker-1", transaction.ScannedLockerId);
        Assert.Equal("A-01", transaction.LockerNumberSnapshot);
        Assert.Equal("Worker One", transaction.EmployeeNameSnapshot);
        Assert.Equal("locker-1", transaction.BeforeLockerId);
        Assert.Equal("locker-1", transaction.AfterLockerId);
    }

    [Fact]
    public async Task BlockingIssue_ClearsHolderCreatesRepairTicketAndRepairHoldTransaction()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.Out, StatusFlags.WithHolder);
        asset.HolderId = "worker-1";
        var fixture = Fixture(asset);

        var result = await fixture.Service.ReportBlockingIssueAsync(Request(asset.Id, "worker-1", "screen cracked"));

        Assert.NotNull(result);
        Assert.Equal(AssetStatus.Repair, asset.Status);
        Assert.Null(asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.InRepair));
        Assert.Equal(TicketPriority.High, result!.Ticket!.Priority);
        Assert.Equal(EquipmentTransactionType.BlockingIssue, fixture.Transactions.Transactions.Single().Type);
    }

    [Fact]
    public async Task SendToDstRmaAsync_ReusesOpenRmaAndSetsRmaState()
    {
        var asset = Asset("asset-1", "TC77-1", AssetStatus.Out, StatusFlags.WithHolder);
        asset.HolderId = "worker-1";
        var rma = new RmaRecord { Id = "rma-existing", AssetId = asset.Id, AssetTag = asset.Tag, DateSubmitted = DateTimeOffset.UtcNow };
        var fixture = Fixture(asset, rmas: [rma]);

        var result = await fixture.Service.SendToDstRmaAsync(Request(asset.Id, "worker-1"), "DST");

        Assert.NotNull(result);
        Assert.Same(rma, result!.RmaRecord);
        Assert.Equal(AssetStatus.InRMA, asset.Status);
        Assert.Null(asset.HolderId);
        Assert.True(asset.Flags.HasFlag(StatusFlags.InRMA));
        Assert.Single(fixture.Rmas.Records);
        Assert.Equal("rma-existing", fixture.Transactions.Transactions.Single().RmaRecordId);
    }

    private static EquipmentStationRequest Request(string assetId, string employeeId, string? notes = null) =>
        new("6IA", assetId, employeeId, "manager-1", notes, "scan-value");

    private static EquipmentStationRequest MobileRequest(string assetId) =>
        new(
            "6IA",
            assetId,
            "worker-1",
            "manager-1",
            "mobile locker tap",
            "rfid:NTAG-001",
            "mobile-session-1",
            "WT-123",
            "locker-1",
            "A-01",
            "Worker One");

    private static Asset Asset(string id, string tag, AssetStatus status, StatusFlags flags) => new()
    {
        Id = id,
        Tag = tag,
        Serial = tag + "-SN",
        Model = "TC77",
        Category = AssetCategory.PodTc77,
        Type = "pod-tc77",
        Status = status,
        Flags = flags,
    };

    private static StationFixture Fixture(Asset asset, RmaRecord[]? rmas = null)
    {
        var assets = new FakeAssetRepository(asset);
        var activity = new ActivityService(new FakeActivityRepository());
        var assetService = new AssetService(assets, activity);
        var tickets = new TicketService(new FakeTicketRepository());
        var rmaRepo = new FakeRmaRecordRepository(rmas ?? []);
        var rmaService = new RmaService(rmaRepo);
        var users = new UserService(new FakeUserRepository(User("worker-1", AccessTier.Worker), User("manager-1", AccessTier.Supervisor)));
        var transactions = new FakeEquipmentTransactionRepository(assets);
        var station = new EquipmentStationService(assetService, tickets, rmaService, users, transactions);
        return new StationFixture(station, transactions, rmaRepo);
    }

    private static User User(string id, AccessTier tier) => new()
    {
        Id = id,
        Name = id,
        Username = id,
        Role = tier,
        DivisionId = "6IA",
    };

    private sealed record StationFixture(
        EquipmentStationService Service,
        FakeEquipmentTransactionRepository Transactions,
        FakeRmaRecordRepository Rmas);

    private sealed class FakeEquipmentTransactionRepository : IEquipmentTransactionRepository
    {
        private readonly FakeAssetRepository _assets;
        public List<EquipmentTransaction> Transactions { get; } = new();

        public FakeEquipmentTransactionRepository(FakeAssetRepository assets)
        {
            _assets = assets;
        }

        public Task<EquipmentTransactionMutationResult?> RecordMutationAsync(
            string assetId,
            EquipmentTransactionType type,
            string divisionId,
            string? employeeId,
            string? actorId,
            string? notes,
            string? ticketId,
            string? ticketLink,
            string? rmaRecordId,
            string? rmaLink,
            string? scanSource,
            string? linkedAssetId,
            Action<Asset> mutateAsset,
            CancellationToken ct = default,
            EquipmentTransactionMetadata? metadata = null)
        {
            var asset = _assets.Assets.GetValueOrDefault(assetId);
            if (asset == null) return Task.FromResult<EquipmentTransactionMutationResult?>(null);

            var beforeFlags = asset.Flags;
            var beforeStatus = asset.Status.ToString();
            var beforeHolder = asset.HolderId;
            var beforeLocker = asset.LockerId;
            mutateAsset(asset);
            var transaction = new EquipmentTransaction
            {
                Type = type,
                DivisionId = divisionId,
                AssetId = asset.Id,
                EmployeeId = employeeId,
                ActorId = actorId,
                Notes = notes,
                TicketId = ticketId,
                RmaRecordId = rmaRecordId,
                ScanSource = scanSource,
                LinkedAssetId = linkedAssetId,
                MobileSessionId = metadata?.MobileSessionId,
                ReaderDeviceSerial = metadata?.ReaderDeviceSerial,
                ScannedLockerId = metadata?.ScannedLockerId,
                LockerNumberSnapshot = metadata?.LockerNumberSnapshot,
                EmployeeNameSnapshot = metadata?.EmployeeNameSnapshot,
                BeforeFlags = beforeFlags,
                AfterFlags = asset.Flags,
                BeforeStatus = beforeStatus,
                AfterStatus = asset.Status.ToString(),
                BeforeHolderId = beforeHolder,
                AfterHolderId = asset.HolderId,
                BeforeLockerId = beforeLocker,
                AfterLockerId = asset.LockerId,
            };
            Transactions.Add(transaction);
            return Task.FromResult<EquipmentTransactionMutationResult?>(new(asset, transaction));
        }

        public Task AddAsync(EquipmentTransaction transaction, CancellationToken ct = default)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EquipmentTransaction>> GetRecentAsync(int count = 100, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<EquipmentTransaction>>(Transactions.Take(count).ToList());
    }

    private sealed class FakeAssetRepository : IAssetRepository
    {
        public Dictionary<string, Asset> Assets { get; }

        public FakeAssetRepository(params Asset[] assets)
        {
            Assets = assets.ToDictionary(a => a.Id);
        }

        public Task<IReadOnlyList<Asset>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Asset>>(Assets.Values.ToList());

        public Task<Asset?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Assets.GetValueOrDefault(id));

        public Task AddAsync(Asset asset, CancellationToken ct = default)
        {
            Assets[asset.Id] = asset;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Asset asset, CancellationToken ct = default)
        {
            Assets[asset.Id] = asset;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            Assets.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Asset>> GetSparePoolAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<Asset>>(Assets.Values.Where(a => a.Status == AssetStatus.Spare));

        public Task<IEnumerable<LoanRecord>> GetActiveLoansAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<LoanRecord>>(Array.Empty<LoanRecord>());
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        private readonly Dictionary<string, Ticket> _tickets = new();

        public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Ticket>>(_tickets.Values.ToList());

        public Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_tickets.GetValueOrDefault(id));

        public Task AddAsync(Ticket ticket, CancellationToken ct = default)
        {
            _tickets[ticket.Id] = ticket;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
        {
            _tickets[ticket.Id] = ticket;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRmaRecordRepository : IRmaRecordRepository
    {
        public List<RmaRecord> Records { get; }

        public FakeRmaRecordRepository(params RmaRecord[] records)
        {
            Records = records.ToList();
        }

        public Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RmaRecord>>(Records.Where(r => !r.IsReceived).ToList());

        public Task<RmaRecord?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Records.FirstOrDefault(r => r.Id == id));

        public Task<IReadOnlyList<RmaRecord>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RmaRecord>>(Records.ToList());

        public Task AddAsync(RmaRecord record, CancellationToken ct = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RmaRecord record, CancellationToken ct = default) => Task.CompletedTask;

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            Records.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, User> _users;

        public FakeUserRepository(params User[] users)
        {
            _users = users.ToDictionary(u => u.Id);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(_users.Values.ToList());

        public Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_users.GetValueOrDefault(id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(_users.Values.FirstOrDefault(u => u.Username == username));

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            _users.Remove(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count = 20, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityEvent>>(Array.Empty<ActivityEvent>());

        public Task AddAsync(ActivityEvent ev, CancellationToken ct = default) => Task.CompletedTask;
    }
}
