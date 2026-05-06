using System;

namespace TopekaIT.Core.Domain.Entities;

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
    
    // Navigation property if needed
    public Asset? Asset { get; set; }
}