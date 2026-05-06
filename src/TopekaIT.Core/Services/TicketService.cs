using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class TicketService
{
    private readonly ITicketRepository _repo;

    public TicketService(ITicketRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default) => _repo.GetAllAsync(ct);
    public Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default) => _repo.GetByIdAsync(id, ct);

    public async Task<Ticket> CreateAsync(string title, string description, string? assetId, AssetKind? assetType, string reportedById, CancellationToken ct = default)
    {
        var all = await _repo.GetAllAsync(ct);
        int maxNum = 1042;
        foreach (var t in all)
        {
            if (t.Id.StartsWith("T-") && int.TryParse(t.Id[2..], out var n) && n > maxNum) maxNum = n;
        }
        var id = $"T-{maxNum + 1}";
        var ticket = new Ticket
        {
            Id = id,
            Title = title.Trim(),
            Description = description.Trim(),
            AssetId = assetId,
            AssetType = assetType,
            ReportedById = reportedById,
            Status = TicketStatus.Open,
            Priority = TicketPriority.Med,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await _repo.AddAsync(ticket, ct);
        return ticket;
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
        
        var all = await _repo.GetAllAsync(ct);
        int maxNum = 1042;
        foreach (var t in all)
        {
            if (t.Id.StartsWith("T-") && int.TryParse(t.Id[2..], out var n) && n > maxNum) maxNum = n;
        }
        var id = $"T-{maxNum + 1}";
        var ticket = new Ticket
        {
            Id = id,
            Title = title,
            Description = description,
            AssetId = assetId,
            AssetType = AssetKind.Asset,
            ReportedById = reportedByUserId,
            Status = TicketStatus.Open,
            Priority = TicketPriority.High,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        await _repo.AddAsync(ticket, ct);
        return ticket;
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
        var t = await _repo.GetByIdAsync(id, ct);
        if (t == null) return null;
        t.Resolution = resolution;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(t, ct);
        return t;
    }
}
