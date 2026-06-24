using TopekaIT.Core.Domain.Entities;

namespace TopekaIT.Core.Ports;

/// <summary>
/// The network side of Lantronix polling. Core asks for inventory; Web decides how to actually talk to the box.
/// </summary>
public interface ILantronixFuelClient
{
    Task<LantronixPollTransportResult> PollInventoryAsync(
        LantronixDevice device,
        TimeSpan timeout,
        CancellationToken ct = default);
}

/// <summary>
/// The raw answer from a Lantronix poll before repository parsing turns it into samples.
/// </summary>
public sealed record LantronixPollTransportResult(bool Success, int? LatencyMs, string? RawResponse, string? FailureReason);
