using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class LockerRepositoryTests
{
    [Fact]
    public async Task AssignOccupantAsync_MovesUserFromOtherLockerAndKeepsSingleActiveAssignment()
    {
        var options = CreateOptions();
        var originalAssignedAt = new DateTimeOffset(2026, 5, 14, 8, 0, 0, TimeSpan.Zero);
        var movedAt = originalAssignedAt.AddHours(1);
        var updatedAt = movedAt.AddHours(1);

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Lockers.AddRange(
                Locker("locker-a", "A-01"),
                Locker("locker-b", "B-02"));
            db.LockerOccupants.Add(new LockerOccupant
            {
                LockerId = "locker-a",
                UserId = "user-1",
                IsPrimary = true,
                AssignedAt = originalAssignedAt,
                AssignedBy = "seed",
            });

            await db.SaveChangesAsync();
        }

        var repo = new LockerRepository(new TestDivisionDbContextFactory(options));

        var (locker, addedOccupant) = await repo.AssignOccupantAsync(
            "locker-b",
            "user-1",
            true,
            "actor-1",
            movedAt);

        Assert.NotNull(locker);
        Assert.True(addedOccupant);
        Assert.Equal("B-02", locker.Number);

        var (_, addedDuplicate) = await repo.AssignOccupantAsync(
            "locker-b",
            "user-1",
            false,
            "actor-2",
            updatedAt);

        Assert.False(addedDuplicate);

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            var assignments = await db.LockerOccupants
                .Where(o => o.UserId == "user-1")
                .OrderBy(o => o.AssignedAt)
                .ToListAsync();

            Assert.Equal(2, assignments.Count);

            var oldAssignment = assignments[0];
            Assert.Equal("locker-a", oldAssignment.LockerId);
            Assert.Equal(movedAt, oldAssignment.UnassignedAt);
            Assert.Equal("actor-1", oldAssignment.UnassignedBy);

            var activeAssignment = Assert.Single(assignments, o => o.UnassignedAt == null);
            Assert.Equal("locker-b", activeAssignment.LockerId);
            Assert.False(activeAssignment.IsPrimary);
            Assert.Equal(movedAt, activeAssignment.AssignedAt);
            Assert.Equal("actor-1", activeAssignment.AssignedBy);
        }
    }

    [Fact]
    public async Task UnassignOccupantAsync_MarksActiveAssignmentWithoutRemovingHistory()
    {
        var options = CreateOptions();
        var assignedAt = new DateTimeOffset(2026, 5, 14, 9, 0, 0, TimeSpan.Zero);
        var unassignedAt = assignedAt.AddHours(2);

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            db.Lockers.Add(Locker("locker-a", "A-01"));
            db.LockerOccupants.Add(new LockerOccupant
            {
                LockerId = "locker-a",
                UserId = "user-1",
                IsPrimary = true,
                AssignedAt = assignedAt,
                AssignedBy = "seed",
            });

            await db.SaveChangesAsync();
        }

        var repo = new LockerRepository(new TestDivisionDbContextFactory(options));

        var locker = await repo.UnassignOccupantAsync(
            "locker-a",
            "user-1",
            "actor-1",
            unassignedAt);

        Assert.NotNull(locker);
        Assert.Equal("A-01", locker.Number);

        await using (var db = new TopekaDbContext(options, TestDataProtection.Provider))
        {
            var assignment = await db.LockerOccupants.SingleAsync(o => o.UserId == "user-1");

            Assert.Equal("locker-a", assignment.LockerId);
            Assert.Equal(assignedAt, assignment.AssignedAt);
            Assert.Equal(unassignedAt, assignment.UnassignedAt);
            Assert.Equal("actor-1", assignment.UnassignedBy);
        }
    }

    private static DbContextOptions<TopekaDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<TopekaDbContext>()
            .UseInMemoryDatabase($"locker-repository-{Guid.NewGuid()}")
            .Options;

    private static Locker Locker(string id, string number) => new()
    {
        Id = id,
        Number = number,
        IsActive = true,
    };
}
