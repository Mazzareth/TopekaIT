using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface IMobileEquipmentSessionRepository
{
    Task AddAsync(MobileEquipmentSession session, CancellationToken ct = default);
    Task<MobileEquipmentSession?> GetActiveByTokenHashAsync(string tokenHash, DateTimeOffset now, CancellationToken ct = default);
    Task UpdateAsync(MobileEquipmentSession session, CancellationToken ct = default);
}
