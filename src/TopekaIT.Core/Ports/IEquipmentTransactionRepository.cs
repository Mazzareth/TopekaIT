using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;

namespace TopekaIT.Core.Ports;

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
        CancellationToken ct = default);

    Task AddAsync(EquipmentTransaction transaction, CancellationToken ct = default);
    Task<IReadOnlyList<EquipmentTransaction>> GetRecentAsync(int count = 100, CancellationToken ct = default);
}

public sealed record EquipmentTransactionMutationResult(Asset Asset, EquipmentTransaction Transaction);
