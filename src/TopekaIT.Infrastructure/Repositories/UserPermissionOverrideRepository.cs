using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for manual access overrides in the master database.
/// </summary>
public class UserPermissionOverrideRepository : IUserPermissionOverrideRepository
{
    private readonly IDbContextFactory<MasterDbContext> _factory;

    public UserPermissionOverrideRepository(IDbContextFactory<MasterDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<UserPermissionOverride>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.UserPermissionOverrides
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.PermissionKey)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(
        string userId,
        string permissionKey,
        PermissionOverrideState state,
        string updatedById,
        DateTimeOffset updatedAt,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.UserPermissionOverrides.FindAsync(new object?[] { userId, permissionKey }, ct);
        if (existing == null)
        {
            db.UserPermissionOverrides.Add(new UserPermissionOverride
            {
                UserId = userId,
                PermissionKey = permissionKey,
                State = state,
                UpdatedById = updatedById,
                UpdatedAt = updatedAt,
            });
        }
        else
        {
            existing.State = state;
            existing.UpdatedById = updatedById;
            existing.UpdatedAt = updatedAt;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(string userId, string permissionKey, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.UserPermissionOverrides.FindAsync(new object?[] { userId, permissionKey }, ct);
        if (existing == null) return;

        db.UserPermissionOverrides.Remove(existing);
        await db.SaveChangesAsync(ct);
    }
}
