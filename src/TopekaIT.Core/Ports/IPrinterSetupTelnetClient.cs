namespace TopekaIT.Core.Ports;

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

public sealed record PrinterSetupTelnetLogin(
    bool Success,
    IPrinterSetupTelnetSession? Session,
    string? ErrorMessage = null);

public interface IPrinterSetupTelnetSession : IAsyncDisposable
{
    Task<string> SendCommandAsync(string command, CancellationToken ct = default);
}
