using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopekaIT.Core.Access;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using TopekaIT.Web.Controllers;
using Xunit;

namespace TopekaIT.Web.Tests;

public class ShellControllerTests
{
    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var controller = CreateController(User("u-001", "worker", "correct-password", AccessTier.Worker));

        var result = await controller.Login(new ShellLoginRequest("worker", "wrong-password"), CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var error = Assert.IsType<ShellLoginError>(unauthorized.Value);
        Assert.Equal("Invalid username or password.", error.Message);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsEffectivePermissionSnapshot()
    {
        var user = User("u-001", "supervisor", "secret", AccessTier.Supervisor, divisionId: "6IA");
        var users = new FakeUserRepository(user);
        var overrides = new FakePermissionOverrideRepository();
        await overrides.UpsertAsync(user.Id, AccessPermissionKeys.TicketsCreate, PermissionOverrideState.Deny, "admin", DateTimeOffset.UtcNow);
        await overrides.UpsertAsync(user.Id, AccessPermissionKeys.TicketsViewQueue, PermissionOverrideState.Allow, "admin", DateTimeOffset.UtcNow);
        var controller = CreateController(users, overrides);

        var result = await controller.Login(new ShellLoginRequest("supervisor", "secret"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ShellLoginResponse>(ok.Value);
        Assert.Equal("u-001", response.User.Id);
        Assert.Equal("Supervisor", response.User.TierLabel);
        Assert.Equal("6IA", response.User.DivisionId);
        Assert.Contains(AccessPermissionKeys.TicketsViewQueue, response.Permissions);
        Assert.DoesNotContain(AccessPermissionKeys.TicketsCreate, response.Permissions);
        Assert.True(users.Users[user.Id].LastActiveAt.HasValue);

        var ticketGroup = Assert.Single(response.PermissionGroups, group => group.Name == "Tickets");
        Assert.True(ticketGroup.Permissions.Single(p => p.Key == AccessPermissionKeys.TicketsViewQueue).IsAllowed);
        Assert.False(ticketGroup.Permissions.Single(p => p.Key == AccessPermissionKeys.TicketsCreate).IsAllowed);
    }

    [Fact]
    public async Task Login_PasswordChangeRequired_ReturnsFlagWithoutDenyingAccess()
    {
        var controller = CreateController(User(
            "u-001",
            "worker",
            "secret",
            AccessTier.Worker,
            mustChangePassword: true));

        var result = await controller.Login(new ShellLoginRequest("worker", "secret"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ShellLoginResponse>(ok.Value);
        Assert.True(response.RequiresPasswordChange);
        Assert.Contains(AccessPermissionKeys.WorkerHome, response.Permissions);
    }

    [Fact]
    public void LoginEndpoint_IsAnonymousJsonApi()
    {
        var controllerRoute = Assert.Single(typeof(ShellController).GetCustomAttributes(typeof(RouteAttribute), inherit: false));
        Assert.Equal("api/shell", ((RouteAttribute)controllerRoute).Template);
        Assert.NotEmpty(typeof(ShellController).GetCustomAttributes(typeof(ApiControllerAttribute), inherit: false));

        var method = typeof(ShellController).GetMethod(nameof(ShellController.Login));
        Assert.NotNull(method);
        var post = Assert.Single(method!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false));
        Assert.Equal("login", ((HttpPostAttribute)post).Template);
        Assert.NotEmpty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false));
    }

    private static ShellController CreateController(User user)
        => CreateController(new FakeUserRepository(user), new FakePermissionOverrideRepository());

    private static ShellController CreateController(
        FakeUserRepository users,
        FakePermissionOverrideRepository overrides)
        => new(new UserService(users), new AccessControlService(users, overrides));

    private static User User(
        string id,
        string username,
        string password,
        AccessTier tier,
        string? divisionId = null,
        bool mustChangePassword = false)
    {
        var hash = PasswordHasher.HashWithMetadata(password);
        return new User
        {
            Id = id,
            Username = username,
            Name = username,
            Role = tier,
            Avatar = UserService.BuildAvatar(username),
            DivisionId = divisionId,
            MustChangePassword = mustChangePassword,
            PasswordHash = hash.hash,
            PasswordSalt = hash.salt,
            PasswordIterations = hash.iterations,
        };
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public FakeUserRepository(params User[] users)
        {
            Users = users.ToDictionary(user => user.Id);
        }

        public Dictionary<string, User> Users { get; }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<User>>(Users.Values.ToList());

        public Task<User?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Users.GetValueOrDefault(id));

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => Task.FromResult(Users.Values.FirstOrDefault(user => string.Equals(user.Username, username, StringComparison.OrdinalIgnoreCase)));

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

    private sealed class FakePermissionOverrideRepository : IUserPermissionOverrideRepository
    {
        private readonly Dictionary<(string UserId, string PermissionKey), UserPermissionOverride> _overrides = new();

        public Task<IReadOnlyList<UserPermissionOverride>> GetForUserAsync(string userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserPermissionOverride>>(_overrides.Values.Where(item => item.UserId == userId).ToList());

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
