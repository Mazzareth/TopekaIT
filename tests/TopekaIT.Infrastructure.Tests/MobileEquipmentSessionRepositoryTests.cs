using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class MobileEquipmentSessionRepositoryTests
{
    [Fact]
    public async Task GetActiveByTokenHashAsync_ReturnsOnlyUnexpiredUnrevokedSessions()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"mobile-sessions-{Guid.NewGuid()}")
            .Options;
        var now = DateTimeOffset.UtcNow;

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.MobileEquipmentSessions.AddRange(
                Session("active", "token-active", now.AddHours(1)),
                Session("expired", "token-expired", now.AddMinutes(-1)),
                Session("revoked", "token-revoked", now.AddHours(1), now));
            await db.SaveChangesAsync();
        }

        var repo = new MobileEquipmentSessionRepository(new TestDivisionDbContextFactory(options));

        var active = await repo.GetActiveByTokenHashAsync("token-active", now);
        var expired = await repo.GetActiveByTokenHashAsync("token-expired", now);
        var revoked = await repo.GetActiveByTokenHashAsync("token-revoked", now);

        Assert.NotNull(active);
        Assert.Equal("active", active!.Id);
        Assert.Null(expired);
        Assert.Null(revoked);
    }

    [Fact]
    public async Task UpdateAsync_PersistsLastSeen()
    {
        var options = new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"mobile-session-update-{Guid.NewGuid()}")
            .Options;
        var now = DateTimeOffset.UtcNow;
        var session = Session("active", "token-active", now.AddHours(1));

        var repo = new MobileEquipmentSessionRepository(new TestDivisionDbContextFactory(options));
        await repo.AddAsync(session);

        session.LastSeenAt = now.AddMinutes(5);
        await repo.UpdateAsync(session);

        await using var verify = new TopekaDbContext(options, TestDataProtection.Provider);
        var loaded = await verify.MobileEquipmentSessions.SingleAsync();
        Assert.Equal(now.AddMinutes(5), loaded.LastSeenAt);
    }

    private static MobileEquipmentSession Session(
        string id,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? revokedAt = null)
    {
        return new MobileEquipmentSession
        {
            Id = id,
            TokenHash = tokenHash,
            UserId = "worker-1",
            DivisionId = "6IA",
            ReaderDeviceSerial = "WT-123",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt
        };
    }
}
