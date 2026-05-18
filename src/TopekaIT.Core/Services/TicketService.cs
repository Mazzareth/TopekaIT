using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class TicketService
{
    private const int InitialTicketNumber = 1042;

    private readonly ITicketRepository _repo;

    public TicketService(ITicketRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<Ticket> CreateAsync(string title, string description, string? assetId, AssetKind? assetType, string reportedById, CancellationToken ct = default)
    {
        return await CreateTicketAsync(
            title.Trim(),
            description.Trim(),
            assetId,
            assetType,
            reportedById,
            TicketPriority.Med,
            ct);
    }

    public async Task<Ticket?> UpdateStatusAsync(string id, TicketStatus status, string actingUserId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t == null) return null;
        t.Status = status;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        if (string.IsNullOrEmpty(t.AssigneeId)) t.AssigneeId = actingUserId;
        await _repo.UpdateAsync(t, ct);
        return t;
    }

    public async Task<Ticket> CreateForRepairAsync(string assetId, string assetLabel, string reportedByUserId, AssetStatus status, CancellationToken ct = default)
    {
        var title = $"Device {assetLabel} — {status}";
        var description = $"Automatically created ticket for device {assetLabel}. Marked as {status}.";

        return await CreateTicketAsync(
            title,
            description,
            assetId,
            AssetKind.Asset,
            reportedByUserId,
            TicketPriority.High,
            ct);
    }

    public async Task<Ticket?> UpdateAssigneeAsync(string id, string assigneeId, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t == null) return null;
        t.AssigneeId = assigneeId;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(t, ct);
        return t;
    }

    public async Task<Ticket?> UpdateResolutionAsync(string id, string resolution, CancellationToken ct = default)
    {
        var t = await _repo.GetByIdAsync(id, ct);
        if (t == null) return null;
        t.Resolution = resolution;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(t, ct);
        return t;
    }

    public async Task<Ticket?> SetResolutionAsync(string id, string resolution, CancellationToken ct = default)
    {
        return await UpdateResolutionAsync(id, resolution, ct);
    }

    private async Task<Ticket> CreateTicketAsync(
        string title,
        string description,
        string? assetId,
        AssetKind? assetType,
        string reportedById,
        TicketPriority priority,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var ticket = new Ticket
        {
            Id = await GetNextTicketIdAsync(ct),
            Title = title,
            Description = description,
            AssetId = assetId,
            AssetType = assetType,
            ReportedById = reportedById,
            Status = TicketStatus.Open,
            Priority = priority,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _repo.AddAsync(ticket, ct);
        return ticket;
    }

    private async Task<string> GetNextTicketIdAsync(CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        var maxNum = InitialTicketNumber;
        foreach (var ticket in all)
        {
            if (ticket.Id.StartsWith("T-") && int.TryParse(ticket.Id[2..], out var number) && number > maxNum)
            {
                maxNum = number;
            }
        }

        return $"T-{maxNum + 1}";
    }
}
