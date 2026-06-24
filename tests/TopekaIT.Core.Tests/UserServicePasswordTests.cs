using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// User security tests for password upgrades, temporary passwords, and station PIN behavior.
/// </summary>
public class UserServicePasswordTests
{
    [Fact]
    public async Task ValidateCredentialsAsync_UpgradesLegacyHashAfterSuccessfulLogin()
    {
        var legacy = PasswordHasher.Hash("old-password", PasswordHasher.LegacyIterations);
        var user = new User
        {
            Id = "u-001",
            Name = "Test User",
            Username = "test",
            Role = AccessTier.Worker,
            PasswordHash = legacy.hash,
            PasswordSalt = legacy.salt,
            PasswordIterations = legacy.iterations,
        };
        var repo = new FakeUserRepository(user);
        var service = new UserService(repo);

        var validated = await service.ValidateCredentialsAsync("test", "old-password");

        Assert.NotNull(validated);
        Assert.Equal(PasswordHasher.CurrentIterations, repo.Users["u-001"].PasswordIterations);
        Assert.True(PasswordHasher.Verify("old-password", repo.Users["u-001"].PasswordHash, repo.Users["u-001"].PasswordSalt, PasswordHasher.CurrentIterations));
    }

    [Fact]
    public async Task CreateAndResetPassword_MarkPasswordAsTemporary()
    {
        var repo = new FakeUserRepository();
        var service = new UserService(repo);

        var created = await service.CreateAsync("Test User", "test", "initial", AccessTier.Worker, "6I-A");
        var reset = await service.ResetPasswordAsync(created.Id, "temporary");

        Assert.Equal("temporary", reset);
        Assert.True(repo.Users[created.Id].MustChangePassword);
        Assert.Equal(PasswordHasher.CurrentIterations, repo.Users[created.Id].PasswordIterations);
    }

    [Fact]
    public async Task UpdateAsync_WithNewPassword_ClearsTemporaryPasswordFlag()
    {
        var repo = new FakeUserRepository(new User
        {
            Id = "u-001",
            Name = "Test User",
            Username = "test",
            Role = AccessTier.Worker,
            MustChangePassword = true,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            PasswordIterations = PasswordHasher.LegacyIterations,
        });
        var service = new UserService(repo);
        var user = repo.Users["u-001"];

        await service.UpdateAsync(user, "new-password");

        Assert.False(repo.Users["u-001"].MustChangePassword);
        Assert.Equal(PasswordHasher.CurrentIterations, repo.Users["u-001"].PasswordIterations);
        Assert.True(PasswordHasher.Verify("new-password", repo.Users["u-001"].PasswordHash, repo.Users["u-001"].PasswordSalt, PasswordHasher.CurrentIterations));
    }

    [Fact]
    public async Task SetStationPinAsync_ValidatesAndResetsPinWithoutExposingHash()
    {
        var user = User("u-001", "worker", AccessTier.Worker, "6IA");
        var repo = new FakeUserRepository(user);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA")));

        await service.SetStationPinAsync(user.Id, "123456");
        var valid = await service.ValidateStationPinAsync("123456", "6IA");
        await service.ClearStationPinAsync(user.Id);
        var cleared = await service.ValidateStationPinAsync("123456", "6IA");

        Assert.NotNull(valid);
        Assert.Equal(user.Id, valid.Employee.Id);
        Assert.NotEqual("123456", repo.Users[user.Id].StationPinHash);
        Assert.Null(cleared);
        Assert.Null(repo.Users[user.Id].StationPinHash);
    }

    [Fact]
    public async Task SetStationPinAsync_RejectsInvalidAndSameDivisionDuplicateButAllowsCrossDivision()
    {
        var first = User("u-001", "first", AccessTier.Worker, "6IA");
        var duplicate = User("u-002", "duplicate", AccessTier.Worker, "6IA");
        var crossDivision = User("u-003", "cross", AccessTier.Worker, "KCK");
        var repo = new FakeUserRepository(first, duplicate, crossDivision);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA"), Division("KCK")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetStationPinAsync(first.Id, "12A456"));
        await service.SetStationPinAsync(first.Id, "123456");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SetStationPinAsync(duplicate.Id, "123456"));
        await service.SetStationPinAsync(crossDivision.Id, "123456");

        Assert.NotNull(await service.ValidateStationPinAsync("123456", "6IA"));
        Assert.NotNull(await service.ValidateStationPinAsync("123456", "KCK"));
    }

    [Fact]
    public async Task ValidateStationPinAsync_InfersAssignedDivisionWhenPinIsUnique()
    {
        var topekaWorker = User("u-001", "topeka-worker", AccessTier.Worker, "6IA");
        var otherWorker = User("u-002", "other-worker", AccessTier.Worker, "KCK");
        var repo = new FakeUserRepository(topekaWorker, otherWorker);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA"), Division("KCK")));

        await service.SetStationPinAsync(topekaWorker.Id, "123456");
        await service.SetStationPinAsync(otherWorker.Id, "654321");

        var noDivisionSelected = await service.ValidateStationPinAsync("123456", null);
        var wrongDivisionSelected = await service.ValidateStationPinAsync("123456", "KCK");

        Assert.NotNull(noDivisionSelected);
        Assert.Equal(topekaWorker.Id, noDivisionSelected!.Employee.Id);
        Assert.Equal("6IA", noDivisionSelected.Employee.DivisionId);
        Assert.NotNull(wrongDivisionSelected);
        Assert.Equal(topekaWorker.Id, wrongDivisionSelected!.Employee.Id);
        Assert.Equal("6IA", wrongDivisionSelected.Employee.DivisionId);
    }

    [Fact]
    public async Task ValidateStationPinAsync_RequiresDivisionWhenPinIsSharedAcrossDivisions()
    {
        var topekaWorker = User("u-001", "topeka-worker", AccessTier.Worker, "6IA");
        var otherWorker = User("u-002", "other-worker", AccessTier.Worker, "KCK");
        var repo = new FakeUserRepository(topekaWorker, otherWorker);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA"), Division("KCK")));

        await service.SetStationPinAsync(topekaWorker.Id, "123456");
        await service.SetStationPinAsync(otherWorker.Id, "123456");

        var noDivisionSelected = await service.ValidateStationPinAsync("123456", null);
        var selectedDivision = await service.ValidateStationPinAsync("123456", "KCK");

        Assert.Null(noDivisionSelected);
        Assert.NotNull(selectedDivision);
        Assert.Equal(otherWorker.Id, selectedDivision!.Employee.Id);
    }

    [Fact]
    public async Task ValidateStationPinAsync_CanDisableCrossDivisionFallbackForPinnedStation()
    {
        var topekaWorker = User("u-001", "topeka-worker", AccessTier.Worker, "6IA");
        var otherWorker = User("u-002", "other-worker", AccessTier.Worker, "KCK");
        var repo = new FakeUserRepository(topekaWorker, otherWorker);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA"), Division("KCK")));

        await service.SetStationPinAsync(topekaWorker.Id, "123456");
        await service.SetStationPinAsync(otherWorker.Id, "654321");

        var flexibleStation = await service.ValidateStationPinAsync("123456", "KCK");
        var pinnedStation = await service.ValidateStationPinAsync("123456", "KCK", allowCrossDivisionFallback: false);

        Assert.NotNull(flexibleStation);
        Assert.Equal(topekaWorker.Id, flexibleStation!.Employee.Id);
        Assert.Null(pinnedStation);
    }

    [Fact]
    public async Task ValidateStationPinAsync_ReturnsManagerAndAdminAuthority()
    {
        var worker = User("u-001", "worker", AccessTier.Worker, "6IA");
        var supervisor = User("u-002", "supervisor", AccessTier.Supervisor, "6IA");
        var admin = User("u-003", "admin", AccessTier.Admin, "6IA");
        var repo = new FakeUserRepository(worker, supervisor, admin);
        var service = new UserService(repo, new FakeDivisionRepository(Division("6IA")));
        await service.SetStationPinAsync(worker.Id, "111111");
        await service.SetStationPinAsync(supervisor.Id, "222222");
        await service.SetStationPinAsync(admin.Id, "333333");

        var workerResult = await service.ValidateStationPinAsync("111111", "6IA");
        var supervisorResult = await service.ValidateStationPinAsync("222222", "6IA");
        var adminResult = await service.ValidateStationPinAsync("333333", "6IA");

        Assert.False(workerResult!.HasSupervisorAuthority);
        Assert.True(supervisorResult!.HasSupervisorAuthority);
        Assert.False(supervisorResult.HasAdminAuthority);
        Assert.True(adminResult!.HasSupervisorAuthority);
        Assert.True(adminResult.HasAdminAuthority);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<string, User> Users { get; }

        public FakeUserRepository(params User[] users)
        {
            Users = users.ToDictionary(u => u.Id);
        }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(Users.Values.ToList());

        public Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Users.GetValueOrDefault(id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(Users.Values.FirstOrDefault(u => u.Username == username));

        public Task AddAsync(User user, CancellationToken ct = default)
        {
            Users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User user, CancellationToken ct = default)
        {
            Users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            Users.Remove(id);
            return Task.CompletedTask;
        }
    }

    private static User User(string id, string username, AccessTier tier, string divisionId) => new()
    {
        Id = id,
        Name = username,
        Username = username,
        Role = tier,
        DivisionId = divisionId,
        PasswordHash = "hash",
        PasswordSalt = "salt",
    };

    private static Division Division(string id) => new()
    {
        Id = id,
        Name = id,
        ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
    };

    private sealed class FakeDivisionRepository : IDivisionRepository
    {
        private readonly Dictionary<string, Division> _divisions;

        public FakeDivisionRepository(params Division[] divisions)
        {
            _divisions = divisions.ToDictionary(d => d.Id);
        }

        public Task<IReadOnlyList<Division>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Division>>(_divisions.Values.ToList());

        public Task<Division?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_divisions.GetValueOrDefault(id));

        public Task AddAsync(Division division, CancellationToken ct = default)
        {
            _divisions[division.Id] = division;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Division division, CancellationToken ct = default)
        {
            _divisions[division.Id] = division;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            _divisions.Remove(id);
            return Task.CompletedTask;
        }
    }
}
