using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Domain.Entities;

/// <summary>
/// A work request tied to a person and optionally a device. Simple on purpose; the queue does the organizing.
/// </summary>
public class Ticket
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string? AssetId { get; set; }
    public AssetKind? AssetType { get; set; }
    public string ReportedById { get; set; } = "";
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? AssigneeId { get; set; }
    public string? Resolution { get; set; }
}
