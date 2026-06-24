using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

/// <summary>
/// EF storage for global Lantronix devices and their polling samples.
/// </summary>
public class LantronixDeviceRepository : ILantronixDeviceRepository
{
    private readonly IDbContextFactory<MasterDbContext> _factory;

    public LantronixDeviceRepository(IDbContextFactory<MasterDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<LantronixDevice>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.LantronixDevices
            .AsNoTracking()
            .Include(d => d.Division)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
    }

    public async Task<LantronixDevice?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.LantronixDevices
            .AsNoTracking()
            .Include(d => d.Division)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IReadOnlyList<LantronixPollSample>> GetSamplesAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxSamples = 500,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var samples = await db.LantronixPollSamples
            .AsNoTracking()
            .Where(s => s.DeviceId == deviceId && s.Timestamp >= from && s.Timestamp <= to)
            .OrderByDescending(s => s.Timestamp)
            .Take(maxSamples)
            .ToListAsync(ct);

        samples.Reverse();
        return samples;
    }

    public async Task<IReadOnlyList<LantronixPollSample>> GetRecentSamplesAsync(string deviceId, int count, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.LantronixPollSamples
            .AsNoTracking()
            .Where(s => s.DeviceId == deviceId)
            .OrderByDescending(s => s.Timestamp)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task RecordPollAsync(LantronixDevice device, LantronixPollSample sample, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.LantronixDevices.Attach(device);
        var entry = db.Entry(device);
        entry.Property(x => x.LastPollAt).IsModified = true;
        entry.Property(x => x.LastPollSucceeded).IsModified = true;
        entry.Property(x => x.LastLatencyMs).IsModified = true;
        entry.Property(x => x.LastFailureReason).IsModified = true;
        entry.Property(x => x.LastFuelVolume).IsModified = true;
        entry.Property(x => x.LastTcVolume).IsModified = true;
        entry.Property(x => x.LastUllage).IsModified = true;
        entry.Property(x => x.LastHeight).IsModified = true;
        entry.Property(x => x.LastWater).IsModified = true;
        entry.Property(x => x.LastTemperature).IsModified = true;

        db.LantronixPollSamples.Add(sample);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> PurgeSamplesOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var oldSamples = await db.LantronixPollSamples
            .Where(s => s.Timestamp < cutoff)
            .ToListAsync(ct);
        db.LantronixPollSamples.RemoveRange(oldSamples);
        await db.SaveChangesAsync(ct);
        return oldSamples.Count;
    }
}
