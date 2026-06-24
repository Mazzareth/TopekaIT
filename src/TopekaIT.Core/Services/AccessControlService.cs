using TopekaIT.Core.Access;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// Turns a user's tier plus any manual overrides into the real set of things they can do right now.
/// </summary>
public class AccessControlService
{
    private readonly IUserRepository _users;
    private readonly IUserPermissionOverrideRepository _overrides;

    public AccessControlService(IUserRepository users, IUserPermissionOverrideRepository overrides)
    {
        _users = users;
        _overrides = overrides;
    }

    public async Task<UserAccess?> GetUserAccessAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        var user = await _users.GetByIdAsync(userId, ct);
        if (user == null) return null;

        var permissionOverrides = await _overrides.GetForUserAsync(userId, ct);
        var overridesByKey = permissionOverrides.ToDictionary(o => o.PermissionKey, o => o.State, StringComparer.OrdinalIgnoreCase);
        var effective = AccessCatalog.DefaultPermissionsFor(user.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (user.Role != AccessTier.SuperAdmin)
        {
            foreach (var item in permissionOverrides)
            {
                if (!AccessCatalog.TryGet(item.PermissionKey, out _)) continue;

                if (item.State == PermissionOverrideState.Allow)
                {
                    effective.Add(item.PermissionKey);
                }
                else
                {
                    effective.Remove(item.PermissionKey);
                }
            }
        }
        else
        {
            effective = AccessCatalog.Permissions.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return new UserAccess(user, effective, overridesByKey);
    }

    public async Task<bool> HasPermissionAsync(string userId, string permissionKey, CancellationToken ct = default)
    {
        var access = await GetUserAccessAsync(userId, ct);
        return access?.Has(permissionKey) == true;
    }

    public async Task<AccessMutationResult> SetOverrideAsync(
        string actorUserId,
        string targetUserId,
        string permissionKey,
        PermissionOverrideState? state,
        CancellationToken ct = default)
    {
        if (!AccessCatalog.TryGet(permissionKey, out var definition))
        {
            return AccessMutationResult.Denied("Unknown permission.");
        }

        var actor = await _users.GetByIdAsync(actorUserId, ct);
        var target = await _users.GetByIdAsync(targetUserId, ct);
        if (actor == null || target == null)
        {
            return AccessMutationResult.Denied("User not found.");
        }

        if (!AccessCatalog.CanManageTier(actor.Role, target.Role))
        {
            return AccessMutationResult.Denied("You can only edit access for lower-tier users.");
        }

        if (!AccessCatalog.IsPermissionGrantableBy(actor.Role, definition))
        {
            return AccessMutationResult.Denied("That permission is not grantable from your tier.");
        }

        if (state.HasValue)
        {
            await _overrides.UpsertAsync(targetUserId, permissionKey, state.Value, actorUserId, DateTimeOffset.UtcNow, ct);
        }
        else
        {
            await _overrides.RemoveAsync(targetUserId, permissionKey, ct);
        }

        return AccessMutationResult.Success();
    }

    public static bool CanAssignTier(AccessTier actorTier, AccessTier targetTier)
        => actorTier == AccessTier.SuperAdmin || actorTier > targetTier;
}

/// <summary>
/// The resolved access snapshot the UI can ask quick yes/no questions against.
/// </summary>
public sealed record UserAccess(
    User User,
    IReadOnlySet<string> Permissions,
    IReadOnlyDictionary<string, PermissionOverrideState> Overrides)
{
    public AccessTier Tier => User.Role;
    public bool Has(string permissionKey) => Permissions.Contains(permissionKey);
    public PermissionOverrideState? OverrideFor(string permissionKey)
        => Overrides.TryGetValue(permissionKey, out var state) ? state : null;
}

/// <summary>
/// Small answer object for access edits, because "denied, here is why" reads better than throwing for normal UI choices.
/// </summary>
public sealed record AccessMutationResult(bool Succeeded, string? Error)
{
    public static AccessMutationResult Success() => new(true, null);
    public static AccessMutationResult Denied(string error) => new(false, error);
}
