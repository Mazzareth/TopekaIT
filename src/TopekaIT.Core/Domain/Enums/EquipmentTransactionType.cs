namespace TopekaIT.Core.Domain.Enums;

/// <summary>
/// The kind of device movement the station recorded.
/// </summary>
public enum EquipmentTransactionType
{
    Checkout,
    Checkin,
    NonBlockingIssue,
    BlockingIssue,
    Swap,
    ManagerAssignment,
    RmaHandoff,
    AssignmentConfirmation
}
