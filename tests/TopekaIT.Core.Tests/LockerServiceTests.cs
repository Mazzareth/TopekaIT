using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;
using Xunit;

namespace TopekaIT.Core.Tests;

public class LockerServiceTests
{
    [Theory]
    [InlineData("NTAG-001")]
    [InlineData("rfid:NTAG-001")]
    [InlineData("nfc:NTAG-001")]
    [InlineData("ntag:NTAG-001")]
    [InlineData("uid:NTAG-001")]
    [InlineData("locker:NTAG-001")]
    [InlineData("https://local.locker/scan?nfc=NTAG-001")]
    public async Task FindByRfidAsync_MatchesRawAndSupportedPayloads(string scanValue)
    {
        var locker = new Locker { Id = "locker-1", Number = "A-01", RfidTagId = "NTAG-001" };
        var service = new LockerService(new FakeLockerRepository(locker), new ActivityService(new FakeActivityRepository()));

        var result = await service.FindByRfidAsync(scanValue);

        Assert.NotNull(result);
        Assert.Equal(locker.Id, result!.Id);
    }

    [Fact]
    public async Task LinkRfidAsync_BlocksDuplicateLockerTags()
    {
        var locker = new Locker { Id = "locker-1", Number = "A-01" };
        var duplicate = new Locker { Id = "locker-2", Number = "A-02", RfidTagId = "NTAG-001" };
        var service = new LockerService(new FakeLockerRepository(locker, duplicate), new ActivityService(new FakeActivityRepository()));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LinkRfidAsync(locker.Id, "nfc:NTAG-001", "manager-1"));

        Assert.Contains("already linked", ex.Message);
    }

    [Fact]
    public async Task ClearRfidLinkAsync_RemovesLockerTag()
    {
        var locker = new Locker
        {
            Id = "locker-1",
            Number = "A-01",
            RfidTagId = "NTAG-001",
            RfidLinkedAt = DateTimeOffset.UtcNow
        };
        var service = new LockerService(new FakeLockerRepository(locker), new ActivityService(new FakeActivityRepository()));

        var result = await service.ClearRfidLinkAsync(locker.Id, "manager-1");

        Assert.NotNull(result);
        Assert.Null(result!.RfidTagId);
        Assert.Null(result.RfidLinkedAt);
    }

    private sealed class FakeLockerRepository : ILockerRepository
    {
        private readonly Dictionary<string, Locker> _lockers;

        public FakeLockerRepository(params Locker[] lockers)
        {
            _lockers = lockers.ToDictionary(locker => locker.Id);
        }

        public Task<IReadOnlyList<Locker>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Locker>>(_lockers.Values.ToList());

        public Task<Locker?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(_lockers.GetValueOrDefault(id));

        public Task AddAsync(Locker locker, CancellationToken ct = default)
        {
            _lockers[locker.Id] = locker;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Locker locker, CancellationToken ct = default)
        {
            _lockers[locker.Id] = locker;
            return Task.CompletedTask;
        }

        public Task<(Locker? Locker, bool AddedOccupant)> AssignOccupantAsync(
            string lockerId,
            string userId,
            bool isPrimary,
            string actorId,
            DateTimeOffset assignedAt,
            CancellationToken ct = default)
        {
            var locker = _lockers.GetValueOrDefault(lockerId);
            if (locker == null) return Task.FromResult<(Locker?, bool)>((null, false));

            locker.Occupants.Add(new LockerOccupant
            {
                LockerId = lockerId,
                UserId = userId,
                IsPrimary = isPrimary,
                AssignedBy = actorId,
                AssignedAt = assignedAt
            });
            return Task.FromResult<(Locker?, bool)>((locker, true));
        }

        public Task<Locker?> UnassignOccupantAsync(
            string lockerId,
            string userId,
            string actorId,
            DateTimeOffset unassignedAt,
            CancellationToken ct = default)
        {
            var locker = _lockers.GetValueOrDefault(lockerId);
            if (locker == null) return Task.FromResult<Locker?>(null);

            foreach (var occupant in locker.Occupants.Where(occupant =>
                string.Equals(occupant.UserId, userId, StringComparison.OrdinalIgnoreCase) &&
                occupant.UnassignedAt == null))
            {
                occupant.UnassignedAt = unassignedAt;
                occupant.UnassignedBy = actorId;
            }

            return Task.FromResult<Locker?>(locker);
        }
    }

    private sealed class FakeActivityRepository : IActivityRepository
    {
        public Task<IReadOnlyList<ActivityEvent>> GetRecentAsync(int count, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ActivityEvent>>(Array.Empty<ActivityEvent>());

        public Task AddAsync(ActivityEvent ev, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
