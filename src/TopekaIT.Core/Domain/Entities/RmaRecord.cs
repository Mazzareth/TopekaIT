using System;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A device's trip through DST/RMA. It tracks the handoff, expected return, and whether the thing came back.
/// </summary>
public class RmaRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string AssetId { get; set; } = "";
    public DateTimeOffset? DateSubmitted { get; set; }
    public DateTimeOffset? ItHandOffDate { get; set; }
    public DateTimeOffset? TentativeReturnDate { get; set; }
    public bool IsReceived { get; set; }
    public DateTimeOffset? ReceivedDate { get; set; }
    public string Comments { get; set; } = "";
    public string Section { get; set; } = "";
    public string AssetTag { get; set; } = "";
    public bool IsTagged { get; set; }
    public bool IsLost { get; set; }
    public DateTimeOffset? DateTagged { get; set; }

    public Asset? Asset { get; set; }
}
