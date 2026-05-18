using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Web.Components.Pages.Worker;
using Xunit;

namespace TopekaIT.Web.Tests;

public class MyTicketsScopeTests
{
    [Fact]
    public void ScopeRequestsForUser_WorkerSeesOnlyOwnRequests()
    {
        var tickets = Tickets();

        var scoped = MyTickets.ScopeRequestsForUser(tickets, "worker-1", canViewDivisionRequests: false);

        Assert.Equal(["mine-newer", "mine-older"], scoped.Select(t => t.Id));
    }

    [Theory]
    [InlineData(AccessTier.Supervisor)]
    [InlineData(AccessTier.Admin)]
    public void ScopeRequestsForUser_SupervisorAndHigherSeeDivisionRequests(AccessTier tier)
    {
        var tickets = Tickets();

        var scoped = MyTickets.ScopeRequestsForUser(tickets, "worker-1", MyTickets.CanViewDivisionRequests(tier));

        Assert.Equal(["other", "mine-newer", "mine-older"], scoped.Select(t => t.Id));
    }

    private static Ticket[] Tickets()
    {
        var now = DateTimeOffset.Parse("2026-05-14T12:00:00Z");

        return
        [
            new Ticket { Id = "mine-older", ReportedById = "worker-1", UpdatedAt = now.AddMinutes(-20) },
            new Ticket { Id = "other", ReportedById = "worker-2", UpdatedAt = now },
            new Ticket { Id = "mine-newer", ReportedById = "worker-1", UpdatedAt = now.AddMinutes(-10) },
        ];
    }
}
