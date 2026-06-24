using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class MobileEquipmentSessionRepository : IMobileEquipmentSessionRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public MobileEquipmentSessionRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(MobileEquipmentSession session, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.MobileEquipmentSessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MobileEquipmentSession?> GetActiveByTokenHashAsync(string tokenHash, DateTimeOffset now, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.MobileEquipmentSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(session =>
                session.TokenHash == tokenHash &&
                session.RevokedAt == null &&
                session.ExpiresAt > now,
                ct);
    }

    public async Task UpdateAsync(MobileEquipmentSession session, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.MobileEquipmentSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
