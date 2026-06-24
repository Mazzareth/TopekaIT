namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// The ticket lifecycle the queue expects.
/// </summary>
public enum TicketStatus
{
    Open,
    InProgress,
    OnHold,
    Resolved
}
