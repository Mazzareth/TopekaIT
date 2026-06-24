using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Ports;

/// <summary>
/// Storage for station ledger rows, with a helper that saves the asset mutation and receipt together.
/// </summary>
public interface IEquipmentTransactionRepository
{
    Task<EquipmentTransactionMutationResult?> RecordMutationAsync(
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
        CancellationToken ct = default,
        EquipmentTransactionMetadata? metadata = null);

    Task AddAsync(EquipmentTransaction transaction, CancellationToken ct = default);
    Task<IReadOnlyList<EquipmentTransaction>> GetRecentAsync(int count = 100, CancellationToken ct = default);
}

/// <summary>
/// The updated asset plus the transaction row created for that same move.
/// </summary>
public sealed record EquipmentTransactionMutationResult(Asset Asset, EquipmentTransaction Transaction);

public sealed record EquipmentTransactionMetadata(
    string? MobileSessionId,
    string? ReaderDeviceSerial,
    string? ScannedLockerId,
    string? LockerNumberSnapshot,
    string? EmployeeNameSnapshot);
