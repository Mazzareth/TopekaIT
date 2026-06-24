using System.Security.Cryptography;
using System.Text;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Domain.Enums;
using TopekaIT.Core.Ports;

namespace TopekaIT.Core.Services;

public class MobileEquipmentService
{
    private readonly UserService _users;
    private readonly AssetService _assets;
    private readonly LockerService _lockers;
    private readonly EquipmentStationService _station;
    private readonly IMobileEquipmentSessionRepository _sessions;
    private readonly TimeProvider _clock;

    public MobileEquipmentService(
        UserService users,
        AssetService assets,
        LockerService lockers,
        EquipmentStationService station,
        IMobileEquipmentSessionRepository sessions,
        TimeProvider? clock = null)
    {
        _users = users;
        _assets = assets;
        _lockers = lockers;
        _station = station;
        _sessions = sessions;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<MobileEquipmentSessionStartResult?> StartSessionAsync(
        string username,
        string password,
        string divisionId,
        string readerDeviceSerial,
        string? platform,
        string? appVersion,
        CancellationToken ct = default)
    {
        var normalizedSerial = NormalizeRequired(readerDeviceSerial, "Reader device serial is required.");
        var user = await _users.ValidateCredentialsAsync(username, password, ct);
        var normalizedDivisionId = NormalizeRequired(divisionId, "Division is required.");
        if (user == null || string.IsNullOrWhiteSpace(user.DivisionId) ||
            !string.Equals(user.DivisionId, normalizedDivisionId, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        await _users.MarkActiveAsync(user.Id, _clock.GetUtcNow(), ct);

        var token = CreateToken();
        var now = _clock.GetUtcNow();
        var session = new MobileEquipmentSession
        {
            Id = Guid.NewGuid().ToString("N")[..16],
            TokenHash = HashToken(token),
            UserId = user.Id,
            DivisionId = user.DivisionId,
            ReaderDeviceSerial = normalizedSerial,
            Platform = NormalizeOptional(platform),
            AppVersion = NormalizeOptional(appVersion),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddHours(12)
        };

        await _sessions.AddAsync(session, ct);

        return new MobileEquipmentSessionStartResult(
            token,
            session.Id,
            session.ExpiresAt,
            user.Id,
            user.Name,
            user.DivisionId,
            session.ReaderDeviceSerial);
    }

    public async Task<MobileEquipmentTapResult> HandleTapAsync(
        string sessionToken,
        string tappedTag,
        bool supervisorOverride = false,
        CancellationToken ct = default)
    {
        var now = _clock.GetUtcNow();
        var session = await _sessions.GetActiveByTokenHashAsync(HashToken(sessionToken), now, ct);
        if (session == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.InvalidSession, "Session expired or invalid.");
        }

        var actor = await _users.GetByIdAsync(session.UserId, ct);
        if (actor == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.InvalidSession, "Session user was not found.");
        }

        var locker = await _lockers.FindByRfidAsync(tappedTag, ct);
        if (locker == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.UnknownLockerTag, "Locker tag is not registered.");
        }

        var occupant = ActiveOccupant(locker);
        if (occupant == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.LockerUnassigned, $"Locker {locker.Number} has no active user.");
        }

        var employee = await _users.GetByIdAsync(occupant.UserId, ct);
        if (employee == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.LockerUnassigned, $"Locker {locker.Number} is assigned to an unknown user.");
        }

        var actorMatchesLocker = string.Equals(actor.Id, employee.Id, StringComparison.OrdinalIgnoreCase);
        var canOverride = supervisorOverride && actor.Role >= AccessTier.Supervisor;
        if (!actorMatchesLocker && !canOverride)
        {
            return MobileEquipmentTapResult.Blocked(
                MobileEquipmentTapStatus.BlockedWrongUser,
                $"Locker {locker.Number} is assigned to {employee.Name}.");
        }

        var asset = await _assets.FindByScanAsync(session.ReaderDeviceSerial, ct);
        if (asset == null)
        {
            return MobileEquipmentTapResult.Blocked(
                MobileEquipmentTapStatus.UnknownReaderDevice,
                $"Reader device {session.ReaderDeviceSerial} is not registered as an asset.");
        }

        var assetLockerId = string.IsNullOrWhiteSpace(asset.LockerId) ? null : asset.LockerId;
        var lockerMatchesAsset = string.IsNullOrWhiteSpace(assetLockerId) ||
            string.Equals(assetLockerId, locker.Id, StringComparison.OrdinalIgnoreCase);
        if (!lockerMatchesAsset && !canOverride)
        {
            return MobileEquipmentTapResult.Blocked(
                MobileEquipmentTapStatus.BlockedWrongLocker,
                "This device is assigned to a different locker.");
        }

        var request = BuildStationRequest(session, actor, employee, locker, asset, tappedTag);
        var checkedOutThroughLocker = string.Equals(asset.LockerId, locker.Id, StringComparison.OrdinalIgnoreCase) &&
            (asset.Status == AssetStatus.Out || asset.Flags.HasFlag(StatusFlags.WithHolder));

        var result = checkedOutThroughLocker
            ? await _station.CheckinViaLockerAsync(request, locker.Id, locker.Number, ct)
            : await _station.CheckoutViaLockerAsync(request, locker.Id, locker.Number, ct);

        if (result == null)
        {
            return MobileEquipmentTapResult.Blocked(MobileEquipmentTapStatus.UnknownReaderDevice, "Device state could not be updated.");
        }

        session.LastSeenAt = now;
        await _sessions.UpdateAsync(session, ct);

        return new MobileEquipmentTapResult(
            checkedOutThroughLocker ? MobileEquipmentTapStatus.CheckedIn : MobileEquipmentTapStatus.CheckedOut,
            checkedOutThroughLocker ? "Device checked in." : "Device checked out.",
            result.Asset.Id,
            AssetLabel(result.Asset),
            locker.Id,
            locker.Number,
            employee.Id,
            employee.Name,
            session.ReaderDeviceSerial,
            result.Transaction.Id,
            result.Transaction.Timestamp);
    }

    public async Task<MobileEquipmentLocationTapResult> RecordLocationTapAsync(
        string readerDeviceSerial,
        string tappedTag,
        DateTimeOffset? observedAt = null,
        CancellationToken ct = default)
    {
        var normalizedSerial = NormalizeRequired(readerDeviceSerial, "Reader device serial is required.");

        var locker = await _lockers.FindByRfidAsync(tappedTag, ct);
        if (locker == null)
        {
            return MobileEquipmentLocationTapResult.Blocked(
                MobileEquipmentLocationTapStatus.UnknownLockerTag,
                "Locker tag is not registered.");
        }

        var asset = await _assets.FindByScanAsync(normalizedSerial, ct);
        if (asset == null)
        {
            return MobileEquipmentLocationTapResult.Blocked(
                MobileEquipmentLocationTapStatus.UnknownReaderDevice,
                $"Reader device {normalizedSerial} is not registered as an asset.");
        }

        await _assets.AssignToLockerAsync(asset.Id, locker.Id, locker.Number, "mobile NFC", ct);
        var updatedAsset = await _assets.GetByIdAsync(asset.Id, ct) ?? asset;

        var occupant = ActiveOccupant(locker);
        var employee = occupant == null ? null : await _users.GetByIdAsync(occupant.UserId, ct);
        var timestamp = observedAt ?? _clock.GetUtcNow();

        return new MobileEquipmentLocationTapResult(
            MobileEquipmentLocationTapStatus.Recorded,
            $"Device recorded at locker {locker.Number}.",
            updatedAsset.Id,
            AssetLabel(updatedAsset),
            locker.Id,
            locker.Number,
            employee?.Id,
            employee?.Name,
            normalizedSerial,
            timestamp,
            updatedAsset.LastSeenLocation);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes((token ?? "").Trim()));
        return Convert.ToHexString(bytes);
    }

    private static EquipmentStationRequest BuildStationRequest(
        MobileEquipmentSession session,
        User actor,
        User employee,
        Locker locker,
        Asset asset,
        string tappedTag)
    {
        var notes = $"{employee.Name} tapped locker {locker.Number} from mobile reader {session.ReaderDeviceSerial}.";
        return new EquipmentStationRequest(
            session.DivisionId,
            asset.Id,
            employee.Id,
            actor.Id,
            notes,
            tappedTag,
            session.Id,
            session.ReaderDeviceSerial,
            locker.Id,
            locker.Number,
            employee.Name);
    }

    private static LockerOccupant? ActiveOccupant(Locker locker) =>
        locker.Occupants
            .Where(occupant => occupant.UnassignedAt == null)
            .OrderByDescending(occupant => occupant.IsPrimary)
            .ThenByDescending(occupant => occupant.AssignedAt)
            .FirstOrDefault();

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string NormalizeRequired(string value, string message)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException(message);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string AssetLabel(Asset asset)
    {
        if (!string.IsNullOrWhiteSpace(asset.Tag)) return asset.Tag;
        if (!string.IsNullOrWhiteSpace(asset.Serial)) return asset.Serial;
        return asset.Id;
    }
}

public sealed record MobileEquipmentSessionStartResult(
    string Token,
    string SessionId,
    DateTimeOffset ExpiresAt,
    string UserId,
    string UserName,
    string DivisionId,
    string ReaderDeviceSerial);

public sealed record MobileEquipmentTapResult(
    MobileEquipmentTapStatus Status,
    string Message,
    string? AssetId,
    string? AssetLabel,
    string? LockerId,
    string? LockerNumber,
    string? EmployeeId,
    string? EmployeeName,
    string? ReaderDeviceSerial,
    string? TransactionId,
    DateTimeOffset? Timestamp)
{
    public static MobileEquipmentTapResult Blocked(MobileEquipmentTapStatus status, string message) =>
        new(status, message, null, null, null, null, null, null, null, null, null);
}

public enum MobileEquipmentTapStatus
{
    CheckedOut,
    CheckedIn,
    InvalidSession,
    UnknownLockerTag,
    LockerUnassigned,
    UnknownReaderDevice,
    BlockedWrongUser,
    BlockedWrongLocker
}

public sealed record MobileEquipmentLocationTapResult(
    MobileEquipmentLocationTapStatus Status,
    string Message,
    string? AssetId,
    string? AssetLabel,
    string? LockerId,
    string? LockerNumber,
    string? EmployeeId,
    string? EmployeeName,
    string? ReaderDeviceSerial,
    DateTimeOffset? Timestamp,
    string? LastSeenLocation)
{
    public static MobileEquipmentLocationTapResult Blocked(MobileEquipmentLocationTapStatus status, string message) =>
        new(status, message, null, null, null, null, null, null, null, null, null);
}

public enum MobileEquipmentLocationTapStatus
{
    Recorded,
    UnknownLockerTag,
    UnknownReaderDevice
}
