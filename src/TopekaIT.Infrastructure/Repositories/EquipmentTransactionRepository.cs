using Microsoft.EntityFrameworkCore;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;
using TopekaIT.Infrastructure.Data;

namespace TopekaIT.Infrastructure.Repositories;

public class EquipmentTransactionRepository : IEquipmentTransactionRepository
{
    private readonly IDivisionDbContextFactory _factory;

    public EquipmentTransactionRepository(IDivisionDbContextFactory factory)
    {
        _factory = factory;
    }

    public async Task<EquipmentTransactionMutationResult?> RecordMutationAsync(
        string assetId,
        EquipmentTransactionType type,
        string divisionId,
        string? employeeId,
        string? actorId,
        string? notes,
        string? ticketId,
        string? ticketLink,
        string? rmaRecordId,
        string? rmaLink,
        string? scanSource,
        string? linkedAssetId,
        Action<Asset> mutateAsset,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var isInMemory = string.Equals(db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
        await using var tx = isInMemory ? null : await db.Database.BeginTransactionAsync(ct);

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == assetId, ct);
        if (asset == null) return null;

        var before = Snapshot(asset);
        mutateAsset(asset);
        var after = Snapshot(asset);

        var transaction = new EquipmentTransaction
        {
            Type = type,
            DivisionId = divisionId,
            AssetId = asset.Id,
            LinkedAssetId = linkedAssetId,
            EmployeeId = employeeId,
            CurrentHolderId = before.HolderId,
            ActorId = actorId,
            Timestamp = DateTimeOffset.UtcNow,
            Notes = NormalizeNullable(notes),
            TicketId = ticketId,
            TicketLink = ticketLink,
            RmaRecordId = rmaRecordId,
            RmaLink = rmaLink,
            ScanSource = NormalizeNullable(scanSource),
            BeforeStatus = before.Status,
            AfterStatus = after.Status,
            BeforeHolderId = before.HolderId,
            AfterHolderId = after.HolderId,
            BeforeLockerId = before.LockerId,
            AfterLockerId = after.LockerId,
            BeforeFlags = before.Flags,
            AfterFlags = after.Flags,
            BeforeState = before.State,
            AfterState = after.State,
        };

        db.EquipmentTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);

        if (tx != null)
        {
            await tx.CommitAsync(ct);
        }

        return new EquipmentTransactionMutationResult(asset, transaction);
    }

    public async Task AddAsync(EquipmentTransaction transaction, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.EquipmentTransactions.Add(transaction);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EquipmentTransaction>> GetRecentAsync(int count = 100, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.EquipmentTransactions
            .Include(t => t.Asset)
            .AsNoTracking()
            .OrderByDescending(t => t.Timestamp)
            .Take(Math.Max(0, count))
            .ToListAsync(ct);
    }

    private static AssetSnapshot Snapshot(Asset asset) => new(
        asset.Status.ToString(),
        asset.HolderId,
        asset.LockerId,
        asset.Flags,
        $"status={asset.Status};flags={asset.Flags};holder={asset.HolderId ?? ""};locker={asset.LockerId ?? ""};location={asset.LastSeenLocation ?? ""}");

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record AssetSnapshot(
        string Status,
        string? HolderId,
        string? LockerId,
        StatusFlags Flags,
        string State);
}
