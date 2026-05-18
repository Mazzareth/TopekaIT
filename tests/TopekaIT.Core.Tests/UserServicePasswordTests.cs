using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

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
}
