using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

/// <summary>
/// Ticket behavior tests. They mostly protect id generation and the repair-ticket shortcuts used by equipment flows.
/// </summary>
public class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_GeneratesNextTicketIdFromExistingTickets()
    {
        var repo = new FakeTicketRepository(
            Ticket("T-1043"),
            Ticket("T-1100"),
            Ticket("not-a-ticket"),
            Ticket("T-pending"));
        var service = new TicketService(repo);

        var ticket = await service.CreateAsync("  Printer down  ", "  Needs help  ", "asset-1", AssetKind.Printer, "user-1");

        Assert.Equal("T-1101", ticket.Id);
        Assert.Equal("Printer down", ticket.Title);
        Assert.Equal("Needs help", ticket.Description);
        Assert.Equal(TicketPriority.Med, ticket.Priority);
        Assert.Same(ticket, repo.AddedTickets.Single());
    }

    [Fact]
    public async Task CreateAsync_StartsAfterInitialTicketNumberWhenNoHigherTicketsExist()
    {
        var repo = new FakeTicketRepository(Ticket("T-10"), Ticket("T-1042"));
        var service = new TicketService(repo);

        var ticket = await service.CreateAsync("Title", "Description", null, null, "user-1");

        Assert.Equal("T-1043", ticket.Id);
    }

    [Fact]
    public async Task CreateForRepairAsync_CreatesHighPriorityAssetRepairTicket()
    {
        var repo = new FakeTicketRepository(Ticket("T-1200"));
        var service = new TicketService(repo);

        var ticket = await service.CreateForRepairAsync("asset-9", "TC77-09", "worker-1", AssetStatus.Repair);

        Assert.Equal("T-1201", ticket.Id);
        Assert.Equal("Device TC77-09 — Repair", ticket.Title);
        Assert.Equal("Automatically created ticket for device TC77-09. Marked as Repair.", ticket.Description);
        Assert.Equal("asset-9", ticket.AssetId);
        Assert.Equal(AssetKind.Asset, ticket.AssetType);
        Assert.Equal("worker-1", ticket.ReportedById);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Same(ticket, repo.AddedTickets.Single());
    }

    [Fact]
    public async Task UpdateResolutionAsync_UpdatesResolution()
    {
        var ticket = Ticket("T-1043");
        var repo = new FakeTicketRepository(ticket);
        var service = new TicketService(repo);
        var originalUpdatedAt = ticket.UpdatedAt;

        var resolved = await service.UpdateResolutionAsync("T-1043", "Replaced cable");

        Assert.Same(ticket, resolved);
        Assert.Equal("Replaced cable", ticket.Resolution);
        Assert.True(ticket.UpdatedAt > originalUpdatedAt);
        Assert.Equal(1, repo.GetByIdCalls);
        Assert.Same(ticket, repo.UpdatedTickets.Single());
    }

    [Fact]
    public async Task UpdateResolutionAsync_ReturnsNullWhenTicketIsMissing()
    {
        var repo = new FakeTicketRepository();
        var service = new TicketService(repo);

        var resolved = await service.UpdateResolutionAsync("missing", "No ticket");

        Assert.Null(resolved);
        Assert.Empty(repo.UpdatedTickets);
    }

    private static Ticket Ticket(string id) => new()
    {
        Id = id,
        Title = "Existing ticket",
        Description = "Existing description",
        ReportedById = "user-1",
        Status = TicketStatus.Open,
        Priority = TicketPriority.Med,
        CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        UpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
    };

    private sealed class FakeTicketRepository : ITicketRepository
    {
        private readonly Dictionary<string, Ticket> _tickets;

        public List<Ticket> AddedTickets { get; } = new();
        public List<Ticket> UpdatedTickets { get; } = new();
        public int GetByIdCalls { get; private set; }

        public FakeTicketRepository(params Ticket[] tickets)
        {
            _tickets = tickets.ToDictionary(t => t.Id);
        }

        public Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Ticket>>(_tickets.Values.ToList());

        public Task<Ticket?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            GetByIdCalls++;
            return Task.FromResult(_tickets.GetValueOrDefault(id));
        }

        public Task AddAsync(Ticket ticket, CancellationToken ct = default)
        {
            _tickets[ticket.Id] = ticket;
            AddedTickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
        {
            _tickets[ticket.Id] = ticket;
            UpdatedTickets.Add(ticket);
            return Task.CompletedTask;
        }
    }
}
