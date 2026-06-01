using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

public class EquipmentTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public EquipmentTransactionType Type { get; set; }
    public string DivisionId { get; set; } = "";
    public string AssetId { get; set; } = "";
    public string? LinkedAssetId { get; set; }
    public string? EmployeeId { get; set; }
    public string? CurrentHolderId { get; set; }
    public string? ActorId { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
    public string? TicketId { get; set; }
    public string? TicketLink { get; set; }
    public string? RmaRecordId { get; set; }
    public string? RmaLink { get; set; }
    public string? ScanSource { get; set; }
    public string? BeforeStatus { get; set; }
    public string? AfterStatus { get; set; }
    public string? BeforeHolderId { get; set; }
    public string? AfterHolderId { get; set; }
    public string? BeforeLockerId { get; set; }
    public string? AfterLockerId { get; set; }
    public StatusFlags BeforeFlags { get; set; }
    public StatusFlags AfterFlags { get; set; }
    public string? BeforeState { get; set; }
    public string? AfterState { get; set; }

    public Asset? Asset { get; set; }
}
