using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for audit sessions and scan entries.
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public AuditRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<AuditSession?> GetSessionAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AuditSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(string sessionId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AuditEntries
            .AsNoTracking()
            .Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.ScannedAt)
            .ToListAsync(ct);
    }

    public async Task AddSessionAsync(AuditSession session, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AuditSessions.Add(session);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddEntryAsync(AuditEntry entry, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AuditEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddEntriesAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AuditEntries.AddRange(entries);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateSessionAsync(AuditSession session, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.AuditSessions.Update(session);
        await db.SaveChangesAsync(ct);
    }
}
