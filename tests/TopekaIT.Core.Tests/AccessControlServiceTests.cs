using TopekaIT.Core.Access;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class AccessControlServiceTests
{
    [Fact]
    public void DefaultPermissions_FollowTierHierarchy()
    {
        var worker = AccessCatalog.DefaultPermissionsFor(AccessTier.Worker);
        var supervisor = AccessCatalog.DefaultPermissionsFor(AccessTier.Supervisor);
        var admin = AccessCatalog.DefaultPermissionsFor(AccessTier.Admin);
        var it = AccessCatalog.DefaultPermissionsFor(AccessTier.IT);
        var superAdmin = AccessCatalog.DefaultPermissionsFor(AccessTier.SuperAdmin);

        Assert.Contains(AccessPermissionKeys.TicketsCreate, worker);
        Assert.DoesNotContain(AccessPermissionKeys.TicketsViewQueue, worker);
        Assert.Contains(AccessPermissionKeys.AssetsViewSupervisorConsole, supervisor);
        Assert.Contains(AccessPermissionKeys.PrintersViewAdmin, admin);
        Assert.DoesNotContain(AccessPermissionKeys.PrintersAutoSetup, admin);
        Assert.DoesNotContain(AccessPermissionKeys.AdminViewLantronix, admin);
        Assert.Contains(AccessPermissionKeys.PrintersAutoSetup, it);
        Assert.Contains(AccessPermissionKeys.AdminCreateDivisions, superAdmin);
    }

    [Fact]
    public async Task EffectiveAccess_AppliesAllowAndDenyOverrides()
    {
        var users = new FakeUserRepository(
            User("worker", AccessTier.Worker),
            User("supervisor", AccessTier.Supervisor));
        var overrides = new FakePermissionOverrideRepository();
        await overrides.UpsertAsync("worker", AccessPermissionKeys.TicketsViewQueue, PermissionOverrideState.Allow, "it", DateTimeOffset.UtcNow);
        await overrides.UpsertAsync("supervisor", AccessPermissionKeys.TicketsCreate, PermissionOverrideState.Deny, "it", DateTimeOffset.UtcNow);
        var service = new AccessControlService(users, overrides);

        Assert.True(await service.HasPermissionAsync("worker", AccessPermissionKeys.TicketsViewQueue));
        Assert.False(await service.HasPermissionAsync("supervisor", AccessPermissionKeys.TicketsCreate));
    }

    [Fact]
    public async Task EffectiveAccess_SuperAdminAlwaysHasFullAccess()
    {
        var users = new FakeUserRepository(User("super", AccessTier.SuperAdmin));
        var overrides = new FakePermissionOverrideRepository();
        await overrides.UpsertAsync("super", AccessPermissionKeys.AdminCreateDivisions, PermissionOverrideState.Deny, "super", DateTimeOffset.UtcNow);
        var service = new AccessControlService(users, overrides);

        Assert.True(await service.HasPermissionAsync("super", AccessPermissionKeys.AdminCreateDivisions));
    }

    [Fact]
    public async Task SetOverride_OnlyAllowsLowerTierGrantablePermissions()
    {
        var users = new FakeUserRepository(
            User("worker", AccessTier.Worker),
            User("supervisor", AccessTier.Supervisor),
            User("admin", AccessTier.Admin),
            User("it", AccessTier.IT),
            User("super", AccessTier.SuperAdmin));
        var overrides = new FakePermissionOverrideRepository();
        var service = new AccessControlService(users, overrides);

        var itGrant = await service.SetOverrideAsync("it", "worker", AccessPermissionKeys.TicketsViewQueue, PermissionOverrideState.Allow);
        var adminGrant = await service.SetOverrideAsync("admin", "worker", AccessPermissionKeys.TicketsViewQueue, PermissionOverrideState.Allow);
        var supervisorGrant = await service.SetOverrideAsync("supervisor", "worker", AccessPermissionKeys.TicketsCreate, PermissionOverrideState.Allow);
        var itSelfTierGrant = await service.SetOverrideAsync("it", "admin", AccessPermissionKeys.PrintersAutoSetup, PermissionOverrideState.Allow);
        var lantronixViewGrant = await service.SetOverrideAsync("super", "admin", AccessPermissionKeys.AdminViewLantronix, PermissionOverrideState.Allow);
        var superAdminOnlyGrant = await service.SetOverrideAsync("super", "worker", AccessPermissionKeys.AdminCreateDivisions, PermissionOverrideState.Allow);

        Assert.True(itGrant.Succeeded);
        Assert.False(adminGrant.Succeeded);
        Assert.True(supervisorGrant.Succeeded);
        Assert.False(itSelfTierGrant.Succeeded);
        Assert.True(lantronixViewGrant.Succeeded);
        Assert.False(superAdminOnlyGrant.Succeeded);
    }

    [Fact]
    public void AccessTierParser_NormalizesLegacyManager()
    {
        Assert.True(AccessTierExtensions.TryParseTier("Manager", out var tier));
        Assert.Equal(AccessTier.Supervisor, tier);
    }

    private static User User(string id, AccessTier tier) => new()
    {
        Id = id,
        Name = id,
        Username = id,
        Role = tier,
        PasswordHash = "hash",
        PasswordSalt = "salt",
    };

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

    private sealed class FakePermissionOverrideRepository : IUserPermissionOverrideRepository
    {
        private readonly Dictionary<(string UserId, string PermissionKey), UserPermissionOverride> _overrides = new();

        public Task<IReadOnlyList<UserPermissionOverride>> GetForUserAsync(string userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserPermissionOverride>>(_overrides.Values.Where(o => o.UserId == userId).ToList());

        public Task UpsertAsync(
            string userId,
            string permissionKey,
            PermissionOverrideState state,
            string updatedById,
            DateTimeOffset updatedAt,
            CancellationToken ct = default)
        {
            _overrides[(userId, permissionKey)] = new UserPermissionOverride
            {
                UserId = userId,
                PermissionKey = permissionKey,
                State = state,
                UpdatedById = updatedById,
                UpdatedAt = updatedAt,
            };
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string userId, string permissionKey, CancellationToken ct = default)
        {
            _overrides.Remove((userId, permissionKey));
            return Task.CompletedTask;
        }
    }
}
