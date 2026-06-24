using System;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A spare-device loan. It answers who borrowed it, why, and whether it made it back.
/// </summary>
public class LoanRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string AssetId { get; set; } = "";
    public string BorrowerId { get; set; } = "";
    public bool IsDayLoan { get; set; }
    public LoanDuration Duration { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset? DateLoaned { get; set; }
    public DateTimeOffset? DateReturned { get; set; }
    public string Comments { get; set; } = "";
    
    public Asset? Asset { get; set; }
}
