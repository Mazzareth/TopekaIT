using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

/// <summary>
/// The kiosk brain. PIN in, device scan in, then this writes the asset state plus the ticket/RMA/transaction trail.
/// </summary>
public class EquipmentStationService
{
    private readonly AssetService _assets;
    private readonly TicketService _tickets;
    private readonly RmaService _rmas;
    private readonly UserService _users;
    private readonly IEquipmentTransactionRepository _transactions;

    public EquipmentStationService(
        AssetService assets,
        TicketService tickets,
        RmaService rmas,
        UserService users,
        IEquipmentTransactionRepository transactions)
    {
        _assets = assets;
        _tickets = tickets;
        _rmas = rmas;
        _users = users;
        _transactions = transactions;
    }

    public Task<StationPinValidationResult?> ValidateStationPinAsync(
        string pin,
        string? divisionId,
        bool allowCrossDivisionFallback = true,
        CancellationToken ct = default) =>
        _users.ValidateStationPinAsync(pin, divisionId, allowCrossDivisionFallback, ct);

    public Task<Asset?> FindByScanAsync(string scanValue, CancellationToken ct = default) =>
        _assets.FindByScanAsync(scanValue, ct);

    public async Task<EquipmentStationResult?> CheckoutAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        var result = await RecordAsync(
            request,
            EquipmentTransactionType.Checkout,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkCheckedOut(asset, request.EmployeeId, request.Notes),
            ct);

        return result;
    }

    public async Task<EquipmentStationResult?> CheckinAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        return await RecordAsync(
            request,
            EquipmentTransactionType.Checkin,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkAvailable(asset, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> ReportNonBlockingIssueAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        var asset = await _assets.GetByIdAsync(request.AssetId, ct);
        if (asset == null) return null;

        var ticket = await _tickets.CreateAsync(
            $"Equipment issue - {AssetLabel(asset)}",
            BuildIssueDescription(asset, request.Notes, blocking: false),
            asset.Id,
            AssetKind.Asset,
            request.EmployeeId,
            ct);

        return await RecordAsync(
            request,
            EquipmentTransactionType.NonBlockingIssue,
            ticket,
            rma: null,
            linkedAssetId: null,
            a => MarkNonBlockingIssue(a, request.EmployeeId, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> ReportBlockingIssueAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        var asset = await _assets.GetByIdAsync(request.AssetId, ct);
        if (asset == null) return null;

        var ticket = await _tickets.CreateForRepairAsync(
            asset.Id,
            AssetLabel(asset),
            request.EmployeeId,
            AssetStatus.Repair,
            ct);

        return await RecordAsync(
            request,
            EquipmentTransactionType.BlockingIssue,
            ticket,
            rma: null,
            linkedAssetId: null,
            a => MarkRepairHold(a, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> AssignByManagerAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        return await RecordAsync(
            request,
            EquipmentTransactionType.ManagerAssignment,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkCheckedOut(asset, request.EmployeeId, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> ConfirmAssignmentAsync(EquipmentStationRequest request, CancellationToken ct = default)
    {
        return await RecordAsync(
            request,
            EquipmentTransactionType.AssignmentConfirmation,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkAssignmentConfirmed(asset, request.EmployeeId, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> CheckoutViaLockerAsync(
        EquipmentStationRequest request,
        string lockerId,
        string lockerNumber,
        CancellationToken ct = default)
    {
        return await RecordAsync(
            request,
            EquipmentTransactionType.Checkout,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkCheckedOutThroughLocker(asset, lockerId, lockerNumber, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> CheckinViaLockerAsync(
        EquipmentStationRequest request,
        string lockerId,
        string lockerNumber,
        CancellationToken ct = default)
    {
        return await RecordAsync(
            request,
            EquipmentTransactionType.Checkin,
            ticket: null,
            rma: null,
            linkedAssetId: null,
            asset => MarkAvailableInLocker(asset, lockerId, lockerNumber, request.Notes),
            ct);
    }

    public async Task<EquipmentStationResult?> SendToDstRmaAsync(EquipmentStationRequest request, string section, DateTimeOffset? tentativeReturnDate = null, CancellationToken ct = default)
    {
        var asset = await _assets.GetByIdAsync(request.AssetId, ct);
        if (asset == null) return null;

        var rma = await _rmas.CreateRmaAsync(
            asset.Id,
            AssetLabel(asset),
            string.IsNullOrWhiteSpace(request.Notes) ? "Sent to DST/RMA from equipment station." : request.Notes,
            string.IsNullOrWhiteSpace(section) ? "DST/RMA" : section.Trim(),
            tentativeReturnDate,
            ct);

        var ticket = await _tickets.CreateForRepairAsync(
            asset.Id,
            AssetLabel(asset),
            request.ActorId ?? request.EmployeeId,
            AssetStatus.InRMA,
            ct);

        return await RecordAsync(
            request,
            EquipmentTransactionType.RmaHandoff,
            ticket,
            rma,
            linkedAssetId: null,
            a => MarkRmaHandoff(a, request.Notes),
            ct);
    }

    public async Task<EquipmentStationSwapResult> SwapAsync(EquipmentStationSwapRequest request, CancellationToken ct = default)
    {
        var oldAsset = await _assets.GetByIdAsync(request.OldAssetId, ct)
            ?? throw new InvalidOperationException("Original asset was not found.");
        var replacement = await _assets.GetByIdAsync(request.ReplacementAssetId, ct)
            ?? throw new InvalidOperationException("Replacement asset was not found.");

        if (HasBlockingState(replacement))
        {
            throw new InvalidOperationException("Replacement asset is not available for checkout.");
        }

        Ticket oldTicket = request.BlockingIssue
            ? await _tickets.CreateForRepairAsync(oldAsset.Id, AssetLabel(oldAsset), request.EmployeeId, AssetStatus.Repair, ct)
            : await _tickets.CreateAsync(
                $"Equipment swap issue - {AssetLabel(oldAsset)}",
                BuildIssueDescription(oldAsset, request.Notes, blocking: false),
                oldAsset.Id,
                AssetKind.Asset,
                request.EmployeeId,
                ct);

        var oldRequest = request.ToStationRequest(request.OldAssetId);
        var oldResult = await RecordAsync(
            oldRequest,
            EquipmentTransactionType.Swap,
            oldTicket,
            rma: null,
            request.ReplacementAssetId,
            asset =>
            {
                if (request.BlockingIssue)
                {
                    MarkRepairHold(asset, request.Notes);
                }
                else
                {
                    MarkAvailable(asset, request.Notes);
                    asset.Flags |= StatusFlags.UnderInvestigation;
                }
            },
            ct) ?? throw new InvalidOperationException("Original asset could not be updated.");

        var replacementRequest = request.ToStationRequest(request.ReplacementAssetId);
        var replacementResult = await RecordAsync(
            replacementRequest,
            EquipmentTransactionType.Swap,
            ticket: null,
            rma: null,
            request.OldAssetId,
            asset => MarkCheckedOut(asset, request.EmployeeId, request.Notes),
            ct) ?? throw new InvalidOperationException("Replacement asset could not be updated.");

        return new EquipmentStationSwapResult(oldResult, replacementResult, oldTicket);
    }

    private async Task<EquipmentStationResult?> RecordAsync(
        EquipmentStationRequest request,
        EquipmentTransactionType type,
        Ticket? ticket,
        RmaRecord? rma,
        string? linkedAssetId,
        Action<Asset> mutate,
        CancellationToken ct)
    {
        var mutation = await _transactions.RecordMutationAsync(
            request.AssetId,
            type,
            request.DivisionId,
            request.EmployeeId,
            request.ActorId,
            request.Notes,
            ticket?.Id,
            ticket == null ? null : $"/it/tickets?search={Uri.EscapeDataString(ticket.Id)}",
            rma?.Id,
            rma == null ? null : $"/it/rma?search={Uri.EscapeDataString(rma.AssetTag)}",
            request.ScanSource,
            linkedAssetId,
            mutate,
            ct,
            request.ToTransactionMetadata());

        return mutation == null ? null : new EquipmentStationResult(mutation.Asset, mutation.Transaction, ticket, rma);
    }

    private static void MarkCheckedOut(Asset asset, string holderId, string? notes)
    {
        SetStatus(asset, AssetStatus.Out);
        asset.HolderId = holderId;
        asset.CheckedOutAt = DateTimeOffset.UtcNow;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = "Checked out";
        SetPrimaryFlag(asset, StatusFlags.WithHolder);
        asset.Flags &= ~(StatusFlags.InRepair | StatusFlags.InRMA | StatusFlags.Missing | StatusFlags.OnHold | StatusFlags.UnderInvestigation);
        AppendNote(asset, notes);
    }

    private static void MarkCheckedOutThroughLocker(Asset asset, string lockerId, string lockerNumber, string? notes)
    {
        SetStatus(asset, AssetStatus.Out);
        asset.LockerId = lockerId;
        asset.HolderId = null;
        asset.CheckedOutAt = DateTimeOffset.UtcNow;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = $"Locker {lockerNumber}";
        SetPrimaryFlag(asset, StatusFlags.WithHolder);
        asset.Flags &= ~(StatusFlags.InRepair | StatusFlags.InRMA | StatusFlags.Missing | StatusFlags.OnHold | StatusFlags.UnderInvestigation);
        AppendNote(asset, notes);
    }

    private static void MarkAvailableInLocker(Asset asset, string lockerId, string lockerNumber, string? notes)
    {
        SetStatus(asset, AssetStatus.InLocker);
        asset.LockerId = lockerId;
        asset.HolderId = null;
        asset.CheckedOutAt = null;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = $"Locker {lockerNumber}";
        SetPrimaryFlag(asset, StatusFlags.InLocker);
        asset.Flags &= ~(StatusFlags.DayLoan | StatusFlags.UnderInvestigation | StatusFlags.InRepair | StatusFlags.InRMA | StatusFlags.OnHold);
        AppendNote(asset, notes);
    }

    private static void MarkAvailable(Asset asset, string? notes)
    {
        var nextStatus = string.IsNullOrWhiteSpace(asset.LockerId) ? AssetStatus.InCC : AssetStatus.InLocker;
        SetStatus(asset, nextStatus);
        asset.HolderId = null;
        asset.CheckedOutAt = null;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = string.IsNullOrWhiteSpace(asset.LockerId) ? "IT cage" : asset.LastSeenLocation;
        SetPrimaryFlag(asset, string.IsNullOrWhiteSpace(asset.LockerId) ? StatusFlags.InCC : StatusFlags.InLocker);
        asset.Flags &= ~(StatusFlags.DayLoan | StatusFlags.UnderInvestigation | StatusFlags.InRepair | StatusFlags.InRMA | StatusFlags.OnHold);
        AppendNote(asset, notes);
    }

    private static void MarkNonBlockingIssue(Asset asset, string holderId, string? notes)
    {
        SetStatus(asset, AssetStatus.Out);
        asset.HolderId = string.IsNullOrWhiteSpace(asset.HolderId) ? holderId : asset.HolderId;
        asset.CheckedOutAt ??= DateTimeOffset.UtcNow;
        SetPrimaryFlag(asset, StatusFlags.WithHolder);
        asset.Flags |= StatusFlags.UnderInvestigation;
        AppendNote(asset, notes);
    }

    private static void MarkRepairHold(Asset asset, string? notes)
    {
        SetStatus(asset, AssetStatus.Repair);
        asset.HolderId = null;
        asset.CheckedOutAt = null;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = "IT repair hold";
        SetPrimaryFlag(asset, StatusFlags.InRepair);
        asset.Flags &= ~(StatusFlags.DayLoan | StatusFlags.UnderInvestigation | StatusFlags.InRMA);
        AppendNote(asset, notes);
    }

    private static void MarkRmaHandoff(Asset asset, string? notes)
    {
        SetStatus(asset, AssetStatus.InRMA);
        asset.HolderId = null;
        asset.CheckedOutAt = null;
        asset.DueAt = null;
        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = "DST/RMA";
        SetPrimaryFlag(asset, StatusFlags.InRMA);
        asset.Flags &= ~(StatusFlags.DayLoan | StatusFlags.UnderInvestigation | StatusFlags.InRepair | StatusFlags.OnHold);
        AppendNote(asset, notes);
    }

    private static void MarkAssignmentConfirmed(Asset asset, string holderId, string? notes)
    {
        if (string.IsNullOrWhiteSpace(asset.HolderId))
        {
            asset.HolderId = holderId;
        }

        asset.LastSeenAt = DateTimeOffset.UtcNow;
        asset.LastSeenLocation = "Employee confirmation";

        if (string.Equals(asset.HolderId, holderId, StringComparison.OrdinalIgnoreCase))
        {
            SetPrimaryFlag(asset, StatusFlags.WithHolder);
            if (asset.Status is not AssetStatus.Out and not AssetStatus.InUse)
            {
                SetStatus(asset, AssetStatus.Out);
            }
        }

        AppendNote(asset, notes);
    }

    private static void SetStatus(Asset asset, AssetStatus status)
    {
        if (asset.Status != status)
        {
            asset.Status = status;
            asset.StatusChangedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void SetPrimaryFlag(Asset asset, StatusFlags primary)
    {
        foreach (var flag in PrimaryFlags)
        {
            asset.Flags &= ~flag;
        }

        asset.Flags |= primary;
    }

    private static bool HasBlockingState(Asset asset) =>
        asset.Flags.HasFlag(StatusFlags.InRepair) ||
        asset.Flags.HasFlag(StatusFlags.InRMA) ||
        asset.Flags.HasFlag(StatusFlags.Missing) ||
        asset.Flags.HasFlag(StatusFlags.OnHold) ||
        !string.IsNullOrWhiteSpace(asset.HolderId);

    private static string BuildIssueDescription(Asset asset, string? notes, bool blocking)
    {
        var severity = blocking ? "Blocking issue" : "Non-blocking issue";
        var body = string.IsNullOrWhiteSpace(notes) ? "No issue notes were entered." : notes.Trim();
        return $"{severity} reported from equipment station for {AssetLabel(asset)}.\n\n{body}";
    }

    private static void AppendNote(Asset asset, string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return;
        var line = $"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm}: {notes.Trim()}";
        asset.Notes = string.IsNullOrWhiteSpace(asset.Notes) ? line : asset.Notes + "\n" + line;
    }

    private static string AssetLabel(Asset asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.Tag)) return asset.Tag;
        if (!string.IsNullOrWhiteSpace(asset.Serial)) return asset.Serial;
        return asset.Id;
    }

    private static readonly StatusFlags[] PrimaryFlags =
    [
        StatusFlags.InLocker, StatusFlags.InCC, StatusFlags.WithHolder,
        StatusFlags.OnLoan, StatusFlags.InRepair, StatusFlags.InRMA,
        StatusFlags.Missing, StatusFlags.OnHold, StatusFlags.Spare,
    ];
}

/// <summary>
/// One station action against one device. ActorId is optional because sometimes the employee and the manager are the same person, sometimes not.
/// </summary>
public sealed record EquipmentStationRequest(
    string DivisionId,
    string AssetId,
    string EmployeeId,
    string? ActorId,
    string? Notes,
    string? ScanSource,
    string? MobileSessionId = null,
    string? ReaderDeviceSerial = null,
    string? ScannedLockerId = null,
    string? LockerNumberSnapshot = null,
    string? EmployeeNameSnapshot = null)
{
    public EquipmentTransactionMetadata? ToTransactionMetadata()
    {
        return string.IsNullOrWhiteSpace(MobileSessionId) &&
            string.IsNullOrWhiteSpace(ReaderDeviceSerial) &&
            string.IsNullOrWhiteSpace(ScannedLockerId) &&
            string.IsNullOrWhiteSpace(LockerNumberSnapshot) &&
            string.IsNullOrWhiteSpace(EmployeeNameSnapshot)
            ? null
            : new(
                MobileSessionId,
                ReaderDeviceSerial,
                ScannedLockerId,
                LockerNumberSnapshot,
                EmployeeNameSnapshot);
    }
}

/// <summary>
/// A two-device station action: the old device gets handled, the replacement goes out, and both sides need the same audit context.
/// </summary>
public sealed record EquipmentStationSwapRequest(
    string DivisionId,
    string OldAssetId,
    string ReplacementAssetId,
    string EmployeeId,
    string? ActorId,
    string? Notes,
    string? ScanSource,
    bool BlockingIssue)
{
    public EquipmentStationRequest ToStationRequest(string assetId) =>
        new(DivisionId, assetId, EmployeeId, ActorId, Notes, ScanSource);
}

/// <summary>
/// What the station needs back after one device move: the updated asset, the ledger row, and any side record it created.
/// </summary>
public sealed record EquipmentStationResult(
    Asset Asset,
    EquipmentTransaction Transaction,
    Ticket? Ticket,
    RmaRecord? RmaRecord);

/// <summary>
/// The paired result for a swap, kept together so the UI does not accidentally show only half the handoff.
/// </summary>
public sealed record EquipmentStationSwapResult(
    EquipmentStationResult Original,
    EquipmentStationResult Replacement,
    Ticket IssueTicket);
