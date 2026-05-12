using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Ports;

public interface IUserPermissionOverrideRepository
{
    Task<IReadOnlyList<UserPermissionOverride>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(string userId, string permissionKey, PermissionOverrideState state, string updatedById, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task RemoveAsync(string userId, string permissionKey, CancellationToken ct = default);
}
