using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class MobileEquipmentServiceTests
{
    [Fact]
    public async Task StartSessionAsync_CreatesSessionForDivisionUserAndReaderSerial()
    {
        var fixture = Fixture();

        var result = await fixture.Mobile.StartSessionAsync("worker", "pass123", "6IA", "WT-123", "android", "1.0");

        Assert.NotNull(result);
        Assert.Equal("worker-1", result!.UserId);
        Assert.Equal("WT-123", result.ReaderDeviceSerial);
        Assert.Single(fixture.Sessions.Sessions);
        Assert.Equal(MobileEquipmentService.HashToken(result.Token), fixture.Sessions.Sessions[0].TokenHash);
    }

    [Fact]
    public async Task HandleTapAsync_TogglesCheckoutAndCheckinThroughLocker()
    {
        var fixture = Fixture();
        var login = await fixture.Mobile.StartSessionAsync("worker", "pass123", "6IA", "WT-123", "android", "1.0");

        var checkout = await fixture.Mobile.HandleTapAsync(login!.Token, "nfc:NTAG-001");
        var checkin = await fixture.Mobile.HandleTapAsync(login.Token, "nfc:NTAG-001");

        Assert.Equal(MobileEquipmentTapStatus.CheckedOut, checkout.Status);
        Assert.Equal("asset-1", checkout.AssetId);
        Assert.Equal("locker-1", checkout.LockerId);
        Assert.Equal("worker-1", checkout.EmployeeId);
        Assert.Equal(MobileEquipmentTapStatus.CheckedIn, checkin.Status);
        Assert.Equal(AssetStatus.InLocker, fixture.Asset.Status);
        Assert.Null(fixture.Asset.HolderId);
        Assert.Equal("locker-1", fixture.Asset.LockerId);
        Assert.Equal(2, fixture.Transactions.Transactions.Count);
        Assert.All(fixture.Transactions.Transactions, transaction =>
        {
            Assert.Equal(login.SessionId, transaction.MobileSessionId);
            Assert.Equal("WT-123", transaction.ReaderDeviceSerial);
            Assert.Equal("locker-1", transaction.ScannedLockerId);
            Assert.Equal("A-01", transaction.LockerNumberSnapshot);
            Assert.Equal("Worker One", transaction.EmployeeNameSnapshot);
        });
    }

    [Fact]
    public async Task HandleTapAsync_BlocksWrongUserUnlessSupervisorOverrideApplies()
    {
        var fixture = Fixture();
        var wrongUser = await fixture.Mobile.StartSessionAsync("other", "pass123", "6IA", "WT-123", "android", "1.0");
        var supervisor = await fixture.Mobile.StartSessionAsync("supervisor", "pass123", "6IA", "WT-123", "android", "1.0");

        var blocked = await fixture.Mobile.HandleTapAsync(wrongUser!.Token, "nfc:NTAG-001");
        var allowed = await fixture.Mobile.HandleTapAsync(supervisor!.Token, "nfc:NTAG-001", supervisorOverride: true);

        Assert.Equal(MobileEquipmentTapStatus.BlockedWrongUser, blocked.Status);
        Assert.Equal(MobileEquipmentTapStatus.CheckedOut, allowed.Status);
        var transaction = Assert.Single(fixture.Transactions.Transactions);
        Assert.Equal("worker-1", transaction.EmployeeId);
        Assert.Equal("supervisor-1", transaction.ActorId);
        Assert.Equal("Worker One", transaction.EmployeeNameSnapshot);
    }

    [Fact]
    public async Task RecordLocationTapAsync_RecordsDeviceLocationAndInfersEmployeeFromLocker()
    {
        var fixture = Fixture();

        var result = await fixture.Mobile.RecordLocationTapAsync("WT-123", "nfc:NTAG-001");

        Assert.Equal(MobileEquipmentLocationTapStatus.Recorded, result.Status);
        Assert.Equal("asset-1", result.AssetId);
        Assert.Equal("locker-1", result.LockerId);
        Assert.Equal("worker-1", result.EmployeeId);
        Assert.Equal("Worker One", result.EmployeeName);
        Assert.Equal("locker-1", fixture.Asset.LockerId);
        Assert.Equal("A-01", fixture.Asset.LastSeenLocation);
        Assert.Equal(AssetStatus.InLocker, fixture.Asset.Status);
    }

    private static MobileFixture Fixture()
    {
        var asset = new Asset
        {
            Id = "asset-1",
            Tag = "WT-123",
            Serial = "WT-123",
            Model = "WT",
            Category = AssetCategory.Scanner,
            Type = "warehouse-tablet",
            Status = AssetStatus.InLocker,
            Flags = StatusFlags.InLocker,
            LockerId = "locker-1"
        };
        var locker = new Locker
        {
            Id = "locker-1",
            Number = "A-01",
            RfidTagId = "NTAG-001",
            Occupants =
            {
                new LockerOccupant
                {
                    LockerId = "locker-1",
                    UserId = "worker-1",
                    IsPrimary = true,
                    AssignedAt = DateTimeOffset.UtcNow.AddDays(-1)
                }
            }
        };

        var userRepo = new FakeUserRepository(
            User("worker-1", "Worker One", "worker", AccessTier.Worker),
            User("other-1", "Other Worker", "other", AccessTier.Worker),
            User("supervisor-1", "Supervisor One", "supervisor", AccessTier.Supervisor));
        var users = new UserService(userRepo);
        var activity = new ActivityService(new FakeActivityRepository());
        var assets = new FakeAssetRepository(asset);
        var assetService = new AssetService(assets, activity);
        var lockerService = new LockerService(new FakeLockerRepository(locker), activity);
        var rmas = new RmaService(new FakeRmaRecordRepository());
        var tickets = new TicketService(new FakeTicketRepository());
        var transactions = new FakeEquipmentTransactionRepository(assets);
        var station = new EquipmentStationService(assetService, tickets, rmas, users, transactions);
        var sessions = new FakeMobileEquipmentSessionRepository();
        var mobile = new MobileEquipmentService(users, assetService, lockerService, station, sessions);

        return new MobileFixture(mobile, asset, sessions, transactions);
    }

    private static User User(string id, string name, string username, AccessTier tier)
    {
        var password = PasswordHasher.HashWithMetadata("pass123");
        return new User
        {
            Id = id,
            Name = name,
            Username = username,
            PasswordHash = password.hash,
            PasswordSalt = password.salt,
            PasswordIterations = password.iterations,
            Role = tier,
            DivisionId = "6IA"
        };
    }

    private sealed record MobileFixture(
        MobileEquipmentService Mobile,
        Asset Asset,
        FakeMobileEquipmentSessionRepository Sessions,
        FakeEquipmentTransactionRepository Transactions);

    private sealed class FakeMobileEquipmentSessionRepository : IMobileEquipmentSessionRepository
    {
        public List<MobileEquipmentSession> Sessions { get; } = new();

        public Task AddAsync(MobileEquipmentSession session, CancellationToken ct = default)
        {
            Sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<MobileEquipmentSession?> GetActiveByTokenHashAsync(
            string tokenHash,
            DateTimeOffset now,
            CancellationToken ct = default)
        {
            return Task.FromResult(Sessions.FirstOrDefault(session =>
                string.Equals(session.TokenHash, tokenHash, StringComparison.Ordinal) &&
                session.RevokedAt == null &&
                session.ExpiresAt > now));
        }

        public Task UpdateAsync(MobileEquipmentSession session, CancellationToken ct = default)
            => Task.CompletedTask;
    }

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
                ScanSource = scanSource,
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

    private sealed class FakeLockerRepository : ILockerRepository
    {
        private readonly Dictionary<string, Locker> _lockers;

        public FakeLockerRepository(params Locker[] lockers)
        {
            _lockers = lockers.ToDictionary(locker => locker.Id);
        }

        public Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Locker>>(_lockers.Values.ToList());

        public Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_lockers.GetValueOrDefault(id));

        public Task AddAsync(Locker locker, CancellationToken ct = default)
        {
            _lockers[locker.Id] = locker;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Locker locker, CancellationToken ct = default)
        {
            _lockers[locker.Id] = locker;
            return Task.CompletedTask;
        }

        public Task<(Locker? Locker, bool AddedOccupant)> AssignOccupantAsync(
            string lockerId,
            string userId,
            bool isPrimary,
            string actorId,
            DateTimeOffset assignedAt,
            CancellationToken ct = default) =>
            Task.FromResult<(Locker?, bool)>((_lockers.GetValueOrDefault(lockerId), false));

        public Task<Locker?> UnassignOccupantAsync(
            string lockerId,
            string userId,
            string actorId,
            DateTimeOffset unassignedAt,
            CancellationToken ct = default) =>
            Task.FromResult(_lockers.GetValueOrDefault(lockerId));
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
            => Task.FromResult(_users.Values.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)));

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
        public Task<IReadOnlyList<RmaRecord>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RmaRecord>>(Array.Empty<RmaRecord>());

        public Task<RmaRecord?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult<RmaRecord?>(null);

        public Task<IReadOnlyList<RmaRecord>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RmaRecord>>(Array.Empty<RmaRecord>());

        public Task AddAsync(RmaRecord record, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task UpdateAsync(RmaRecord record, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string id, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityEvent>>(Array.Empty<ActivityEvent>());

        public Task AddAsync(ActivityEvent ev, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
