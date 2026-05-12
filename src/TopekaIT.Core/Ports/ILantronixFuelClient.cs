using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

public interface ILantronixFuelClient
{
    Task<LantronixPollTransportResult> PollInventoryAsync(
        LantronixDevice device,
        TimeSpan timeout,
        CancellationToken ct = default);
}

public sealed record LantronixPollTransportResult(bool Success, int? LatencyMs, string? RawResponse, string? FailureReason);
