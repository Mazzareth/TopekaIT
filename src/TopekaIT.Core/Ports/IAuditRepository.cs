using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for audit sessions and the scans that belong to them.
/// </summary>
public interface IAuditRepository
{
    Task<AuditSession?> GetSessionAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetEntriesAsync(string sessionId, CancellationToken ct = default);
    Task AddSessionAsync(AuditSession session, CancellationToken ct = default);
    Task AddEntryAsync(AuditEntry entry, CancellationToken ct = default);
    Task AddEntriesAsync(IEnumerable<AuditEntry> entries, CancellationToken ct = default);
    Task UpdateSessionAsync(AuditSession session, CancellationToken ct = default);
}
