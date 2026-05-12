using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface ILantronixDeviceRepository
{
    Task<IReadOnlyList<LantronixDevice>> GetAllAsync(CancellationToken ct = default);
    Task<LantronixDevice?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<LantronixPollSample>> GetSamplesAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxSamples = 500,
        CancellationToken ct = default);
    Task<IReadOnlyList<LantronixPollSample>> GetRecentSamplesAsync(string deviceId, int count, CancellationToken ct = default);
    Task RecordPollAsync(LantronixDevice device, LantronixPollSample sample, CancellationToken ct = default);
}
