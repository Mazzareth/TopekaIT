using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Ports;

namespace TopekaIT.Web.Services;

public class LantronixFuelClient : ILantronixFuelClient
{
    private const int MaxResponseBytes = 8192;
    private static readonly TimeSpan IdleAfterFirstBytes = TimeSpan.FromMilliseconds(600);

    public async Task<LantronixPollTransportResult> PollInventoryAsync(
        LantronixDevice device,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var host = !string.IsNullOrWhiteSpace(device.IpAddress)
            ? device.IpAddress.Trim()
            : device.Hostname.Trim();

        if (string.IsNullOrWhiteSpace(host))
        {
            return new LantronixPollTransportResult(false, null, null, "Device host is blank.");
        }

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            using var client = new TcpClient { NoDelay = true };
            await client.ConnectAsync(host, device.Port, timeoutCts.Token);

            await using var stream = client.GetStream();
            var command = BuildCommand(device.PollCommand);
            await stream.WriteAsync(command, timeoutCts.Token);
            await stream.FlushAsync(timeoutCts.Token);

            var raw = await ReadResponseAsync(stream, timeout, timeoutCts.Token);
            stopwatch.Stop();

            return string.IsNullOrWhiteSpace(raw)
                ? new LantronixPollTransportResult(false, (int)stopwatch.ElapsedMilliseconds, raw, "No response from fuel controller.")
                : new LantronixPollTransportResult(true, (int)stopwatch.ElapsedMilliseconds, raw, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new LantronixPollTransportResult(false, (int)stopwatch.ElapsedMilliseconds, null, "Timed out waiting for fuel controller response.");
        }
        catch (SocketException ex)
        {
            stopwatch.Stop();
            return new LantronixPollTransportResult(false, (int)stopwatch.ElapsedMilliseconds, null, ex.SocketErrorCode.ToString());
        }
        catch (IOException ex)
        {
            stopwatch.Stop();
            return new LantronixPollTransportResult(false, (int)stopwatch.ElapsedMilliseconds, null, ex.Message);
        }
    }

    private static byte[] BuildCommand(string command)
    {
        var normalized = command.Trim();
        if (normalized.Length == 0)
        {
            normalized = LantronixDeviceDefaults.InventoryCommand;
        }

        var payload = normalized[0] == '\u0001'
            ? normalized
            : $"\u0001{normalized}";

        return Encoding.ASCII.GetBytes(payload);
    }

    private static async Task<string> ReadResponseAsync(NetworkStream stream, TimeSpan firstByteTimeout, CancellationToken ct)
    {
        var buffer = new byte[1024];
        using var memory = new MemoryStream();

        while (memory.Length < MaxResponseBytes)
        {
            using var readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            readCts.CancelAfter(memory.Length == 0 ? firstByteTimeout : IdleAfterFirstBytes);

            int read;
            try
            {
                read = await stream.ReadAsync(buffer, readCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested && memory.Length > 0)
            {
                break;
            }

            if (read == 0)
            {
                break;
            }

            memory.Write(buffer, 0, read);
        }

        return CleanResponse(Encoding.ASCII.GetString(memory.ToArray()));
    }

    private static string CleanResponse(string response)
    {
        var cleaned = new string(response
            .Where(ch => ch is '\r' or '\n' or '\t' || !char.IsControl(ch))
            .ToArray());

        return cleaned.Trim();
    }
}
