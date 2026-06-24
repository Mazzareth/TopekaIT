namespace TopekaIT.Core.Ports;

/// <summary>
/// The telnet adapter used by printer setup. Core owns the script; this port owns the wire.
/// </summary>
public interface IPrinterSetupTelnetClient
{
    Task<PrinterSetupTelnetLogin> TryLoginAsync(
        string ipAddress,
        int port,
        string username,
        string password,
        TimeSpan timeout,
        CancellationToken ct = default);
}

/// <summary>
/// Login result plus the live session if the printer let us in.
/// </summary>
public sealed record PrinterSetupTelnetLogin(
    bool Success,
    IPrinterSetupTelnetSession? Session,
    string? ErrorMessage = null);

/// <summary>
/// A logged-in printer telnet session. Send one command, get the printer's text back.
/// </summary>
public interface IPrinterSetupTelnetSession : IAsyncDisposable
{
    Task<string> SendCommandAsync(string command, CancellationToken ct = default);
}
