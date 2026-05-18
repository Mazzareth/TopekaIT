using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Infrastructure.Data;
using TopekaIT.Infrastructure.Repositories;
using Xunit;

namespace TopekaIT.Infrastructure.Tests;

public class LantronixDeviceRepositoryTests
{
    [Fact]
    public async Task PurgeSamplesOlderThanAsync_RemovesOnlyOldSamples()
    {
        var now = DateTimeOffset.UtcNow;
        var options = new DbContextOptionsBuilder<MasterDbContext>()
            .UseInMemoryDatabase($"lantronix-retention-{Guid.NewGuid()}")
            .Options;

        await using (var db = new MasterDbContext(options, TestDataProtection.Provider))
        {
            db.LantronixDevices.Add(new LantronixDevice
            {
                Id = "lan-1",
                Name = "Fuel Controller",
                Hostname = "fuel.local",
                IpAddress = "10.0.0.10",
                DeviceType = "Fuel",
                PollCommand = "I20100",
            });
            db.LantronixPollSamples.AddRange(
                new LantronixPollSample { DeviceId = "lan-1", Timestamp = now.AddDays(-31), Success = true },
                new LantronixPollSample { DeviceId = "lan-1", Timestamp = now.AddDays(-1), Success = true });
            await db.SaveChangesAsync();
        }

        var repo = new LantronixDeviceRepository(new TestMasterDbContextFactory(options));

        var purged = await repo.PurgeSamplesOlderThanAsync(now.AddDays(-30));

        await using var verify = new MasterDbContext(options, TestDataProtection.Provider);
        Assert.Equal(1, purged);
        Assert.Single(verify.LantronixPollSamples);
    }
}
