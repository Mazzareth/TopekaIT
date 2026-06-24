using System.Net.Sockets;
using System.Text;
using TopekaIT.Core.Ports;

namespace TopekaIT.Web.Services;

/// <summary>
/// Plain TCP/telnet client for PrintNet setup. It is intentionally small because the command safety lives in Core.
/// </summary>
public sealed class PrinterSetupTelnetClient : IPrinterSetupTelnetClient
{
    public async Task<PrinterSetupTelnetLogin> TryLoginAsync(
        string ipAddress,
        int port,
        string username,
        string password,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var client = new TcpClient();
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeout);
            await client.ConnectAsync(ipAddress, port, timeoutCts.Token);

            var session = new PrinterSetupTelnetSession(client, timeout);
            var initial = await session.ReadUntilIdleAsync(ct);
            if (ContainsLoginPrompt(initial))
            {
                await session.WriteLineAsync(username, ct);
                initial = await session.ReadUntilIdleAsync(ct);
            }
            else if (!LooksLikePrompt(initial))
            {
                await session.WriteLineAsync(username, ct);
                initial = await session.ReadUntilIdleAsync(ct);
            }

            if (ContainsPasswordPrompt(initial))
            {
                await session.WriteLineAsync(password, ct);
                initial = await session.ReadUntilIdleAsync(ct);
            }

            if (ContainsAuthFailure(initial) || ContainsLoginPrompt(initial))
            {
                await session.DisposeAsync();
                return new PrinterSetupTelnetLogin(false, null, "Login failed.");
            }

            return new PrinterSetupTelnetLogin(true, session);
        }
        catch (OperationCanceledException)
        {
            client.Dispose();
            return new PrinterSetupTelnetLogin(false, null, "Connection timed out.");
        }
        catch (Exception ex) when (ex is SocketException or IOException)
        {
            client.Dispose();
            return new PrinterSetupTelnetLogin(false, null, ex.Message);
        }
    }

    private static bool ContainsLoginPrompt(string text) =>
        text.Contains("login", StringComparison.OrdinalIgnoreCase)
        || text.Contains("username", StringComparison.OrdinalIgnoreCase)
        || text.Contains("user:", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsPasswordPrompt(string text) =>
        text.Contains("password", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAuthFailure(string text) =>
        text.Contains("incorrect", StringComparison.OrdinalIgnoreCase)
        || text.Contains("invalid", StringComparison.OrdinalIgnoreCase)
        || text.Contains("failed", StringComparison.OrdinalIgnoreCase)
        || text.Contains("denied", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePrompt(string text)
    {
        var trimmed = text.TrimEnd();
        return trimmed.EndsWith(">", StringComparison.Ordinal)
            || trimmed.EndsWith("#", StringComparison.Ordinal)
            || trimmed.EndsWith("$", StringComparison.Ordinal);
    }
}

/// <summary>
/// A live printer command session. It reads until the printer prompt comes back or the timeout wins.
/// </summary>
internal sealed class PrinterSetupTelnetSession : IPrinterSetupTelnetSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly TimeSpan _timeout;

    public PrinterSetupTelnetSession(TcpClient client, TimeSpan timeout)
    {
        _client = client;
        _stream = client.GetStream();
        _timeout = timeout;
    }

    public async Task<string> SendCommandAsync(string command, CancellationToken ct = default)
    {
        await WriteLineAsync(command, ct);
        return await ReadUntilIdleAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _client.Dispose();
    }

    internal async Task WriteLineAsync(string value, CancellationToken ct)
    {
        var bytes = Encoding.ASCII.GetBytes(value + "\r\n");
        await _stream.WriteAsync(bytes, ct);
        await _stream.FlushAsync(ct);
    }

    internal async Task<string> ReadUntilIdleAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        var output = new StringBuilder();
        var started = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - started < _timeout)
        {
            ct.ThrowIfCancellationRequested();
            if (_stream.DataAvailable)
            {
                var read = await _stream.ReadAsync(buffer, ct);
                if (read == 0)
                {
                    break;
                }

                output.Append(Encoding.ASCII.GetString(buffer, 0, read));
                
                var trimmed = output.ToString().TrimEnd();
                if (trimmed.EndsWith(">", StringComparison.Ordinal)
                    || trimmed.EndsWith("#", StringComparison.Ordinal)
                    || trimmed.EndsWith("$", StringComparison.Ordinal))
                {
                    break;
                }
                continue;
            }

            await Task.Delay(50, ct);
        }

        return output.ToString();
    }
}
