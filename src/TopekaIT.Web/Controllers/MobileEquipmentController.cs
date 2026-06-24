using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopekaIT.Core.Ports;
using TopekaIT.Core.Services;

namespace TopekaIT.Web.Controllers;

[ApiController]
[Route("api/mobile/equipment")]
public sealed class MobileEquipmentController : ControllerBase
{
    private readonly MobileEquipmentService _mobile;
    private readonly DivisionService _divisions;
    private readonly ITenantContext _tenant;

    public MobileEquipmentController(
        MobileEquipmentService mobile,
        DivisionService divisions,
        ITenantContext tenant)
    {
        _mobile = mobile;
        _divisions = divisions;
        _tenant = tenant;
    }

    [HttpPost("sessions")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileEquipmentSessionResponse>> StartSession(
        [FromBody] MobileEquipmentSessionRequest request,
        CancellationToken ct)
    {
        if (!await ResolveDivisionAsync(request.DivisionId, ct))
        {
            return BadRequest(new MobileEquipmentErrorResponse("Unknown division."));
        }

        var result = await _mobile.StartSessionAsync(
            request.Username,
            request.Password,
            request.DivisionId,
            request.ReaderDeviceSerial,
            request.Platform,
            request.AppVersion,
            ct);

        if (result == null)
        {
            return Unauthorized(new MobileEquipmentErrorResponse("Invalid username or password."));
        }

        return Ok(new MobileEquipmentSessionResponse(
            result.Token,
            result.SessionId,
            result.ExpiresAt,
            result.UserId,
            result.UserName,
            result.DivisionId,
            result.ReaderDeviceSerial));
    }

    [HttpPost("taps")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileEquipmentTapResponse>> Tap(
        [FromBody] MobileEquipmentTapRequest request,
        CancellationToken ct)
    {
        if (!await ResolveDivisionAsync(request.DivisionId, ct))
        {
            return BadRequest(new MobileEquipmentTapResponse(
                MobileEquipmentTapStatus.UnknownLockerTag.ToString(),
                "Unknown division.",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var result = await _mobile.HandleTapAsync(
            request.SessionToken,
            request.TappedTag,
            request.SupervisorOverride,
            ct);

        var response = new MobileEquipmentTapResponse(
            result.Status.ToString(),
            result.Message,
            result.AssetId,
            result.AssetLabel,
            result.LockerId,
            result.LockerNumber,
            result.EmployeeId,
            result.EmployeeName,
            result.ReaderDeviceSerial,
            result.TransactionId,
            result.Timestamp);

        return result.Status is MobileEquipmentTapStatus.CheckedOut or MobileEquipmentTapStatus.CheckedIn
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpPost("location-taps")]
    [AllowAnonymous]
    public async Task<ActionResult<MobileEquipmentLocationTapResponse>> LocationTap(
        [FromBody] MobileEquipmentLocationTapRequest request,
        CancellationToken ct)
    {
        if (!await ResolveDivisionAsync(request.DivisionId, ct))
        {
            return BadRequest(new MobileEquipmentLocationTapResponse(
                MobileEquipmentLocationTapStatus.UnknownLockerTag.ToString(),
                "Unknown division.",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var result = await _mobile.RecordLocationTapAsync(
            request.ReaderDeviceSerial,
            request.TappedTag,
            request.ObservedAt,
            ct);

        var response = new MobileEquipmentLocationTapResponse(
            result.Status.ToString(),
            result.Message,
            result.AssetId,
            result.AssetLabel,
            result.LockerId,
            result.LockerNumber,
            result.EmployeeId,
            result.EmployeeName,
            result.ReaderDeviceSerial,
            result.Timestamp,
            result.LastSeenLocation);

        return result.Status == MobileEquipmentLocationTapStatus.Recorded
            ? Ok(response)
            : BadRequest(response);
    }

    private async Task<bool> ResolveDivisionAsync(string divisionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(divisionId)) return false;

        var division = await _divisions.GetByIdAsync(divisionId.Trim(), ct);
        if (division == null) return false;

        _tenant.SetDivision(division.Id, division.ConnectionString);
        return true;
    }
}

public sealed record MobileEquipmentSessionRequest(
    string Username,
    string Password,
    string DivisionId,
    string ReaderDeviceSerial,
    string? Platform,
    string? AppVersion);

public sealed record MobileEquipmentSessionResponse(
    string Token,
    string SessionId,
    DateTimeOffset ExpiresAt,
    string UserId,
    string UserName,
    string DivisionId,
    string ReaderDeviceSerial);

public sealed record MobileEquipmentTapRequest(
    string DivisionId,
    string SessionToken,
    string TappedTag,
    bool SupervisorOverride = false);

public sealed record MobileEquipmentTapResponse(
    string Status,
    string Message,
    string? AssetId,
    string? AssetLabel,
    string? LockerId,
    string? LockerNumber,
    string? EmployeeId,
    string? EmployeeName,
    string? ReaderDeviceSerial,
    string? TransactionId,
    DateTimeOffset? Timestamp);

public sealed record MobileEquipmentLocationTapRequest(
    string DivisionId,
    string ReaderDeviceSerial,
    string TappedTag,
    DateTimeOffset? ObservedAt = null,
    string? Platform = null,
    string? AppVersion = null);

public sealed record MobileEquipmentLocationTapResponse(
    string Status,
    string Message,
    string? AssetId,
    string? AssetLabel,
    string? LockerId,
    string? LockerNumber,
    string? EmployeeId,
    string? EmployeeName,
    string? ReaderDeviceSerial,
    DateTimeOffset? Timestamp,
    string? LastSeenLocation);

public sealed record MobileEquipmentErrorResponse(string Message);
