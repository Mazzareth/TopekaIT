using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public TicketRepository(IDivisionDbContextFactory factory) { _factory = factory; }

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Tickets.AsNoTracking().OrderByDescending(t => t.UpdatedAt).ToListAsync(ct);
    }

    public async Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Tickets.Update(ticket);
        await db.SaveChangesAsync(ct);
    }
}
