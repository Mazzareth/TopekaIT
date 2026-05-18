using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using TopekaIT.Core.Domain.Entities;
using TopekaIT.Core.Services;
using TopekaIT.Infrastructure.Repositories;
using TopekaIT.Infrastructure.Tenant;

namespace TopekaIT.Web.Services;

/// <summary>
/// Listens on TCP and UDP ports (default 4010) for printer log messages.
/// Printers can be configured to send status/error messages to this endpoint.
/// Each message is parsed and stored as a PrinterEvent, matched to a printer by IP address.
/// </summary>
public class PrinterLogSinkService : BackgroundService
{
    private readonly PrinterRouteResolver _printerRoutes;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly ILogger<PrinterLogSinkService> _logger;
    private readonly int _tcpPort;
    private readonly int _udpPort;
    private static readonly TimeSpan PartialMessageFlushInterval = TimeSpan.FromSeconds(1);

    public PrinterLogSinkService(
        PrinterRouteResolver printerRoutes,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<PrinterLogSinkService> logger,
        IConfiguration configuration)
    {
        _printerRoutes = printerRoutes;
        _dataProtectionProvider = dataProtectionProvider;
        _logger = logger;
        var defaultPort = configuration.GetValue<int>("PrinterLogSink:Port", 4010);
        _tcpPort = configuration.GetValue("PrinterLogSink:TcpPort", defaultPort);
        _udpPort = configuration.GetValue("PrinterLogSink:UdpPort", defaultPort);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.WhenAll(
            RunTcpListenerAsync(stoppingToken),
            RunUdpListenerAsync(stoppingToken));
    }

    private async Task RunTcpListenerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var listener = new TcpListener(IPAddress.Any, _tcpPort);
            try
            {
                listener.Start();
                _logger.LogInformation("Printer Log Sink listening for TCP on port {Port}", _tcpPort);

                while (!stoppingToken.IsCancellationRequested)
                {
                    TcpClient client;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogWarning(ex, "Printer Log Sink TCP accept error on port {Port}; restarting listener", _tcpPort);
                        break;
                    }

                    // Keep accepting printer connections while each stream drains independently.
                    _ = HandleClientAsync(client, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer Log Sink TCP listener error on port {Port}; will restart", _tcpPort);
            }
            finally
            {
                listener.Stop();
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Printer Log Sink TCP listener stopped.");
    }

    private async Task RunUdpListenerAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            try
            {
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, _udpPort));
                _logger.LogInformation("Printer Log Sink listening for UDP on port {Port}", _udpPort);

                while (!stoppingToken.IsCancellationRequested)
                {
                    UdpReceiveResult result;
                    try
                    {
                        result = await udp.ReceiveAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogWarning(ex, "Printer Log Sink UDP receive error on port {Port}; restarting listener", _udpPort);
                        break;
                    }

                    _ = HandleUdpMessageAsync(result, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer Log Sink UDP listener error on port {Port}; will restart", _udpPort);
            }

            if (!stoppingToken.IsCancellationRequested)
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Printer Log Sink UDP listener stopped.");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
        var remoteIp = PrinterIpNormalizer.Normalize(remoteEndPoint?.Address);
        _logger.LogInformation("Accepted printer log connection from {Ip}", remoteIp);

        try
        {
            using (client)
            {
                await using var stream = client.GetStream();

                var route = await ResolvePrinterAsync(remoteIp, ct);
                if (route == null)
                {
                    _logger.LogWarning("No printer registered for IP {Ip}; incoming printer log messages will be discarded", remoteIp);
                    await DrainClientAsync(stream, remoteIp, ct);
                    return;
                }

                await ReadMessagesAsync(stream, route, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error handling connection from {Ip}", remoteIp);
        }
    }

    private async Task HandleUdpMessageAsync(UdpReceiveResult result, CancellationToken ct)
    {
        var remoteIp = PrinterIpNormalizer.Normalize(result.RemoteEndPoint.Address);
        var payload = Encoding.UTF8.GetString(result.Buffer);

        try
        {
            var route = await ResolvePrinterAsync(remoteIp, ct);
            if (route == null)
            {
                _logger.LogWarning("No printer registered for UDP source IP {Ip}; discarded message: {Message}", remoteIp, payload.Trim());
                return;
            }

            await PersistMessageAsync(payload, route, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Error handling UDP printer log message from {Ip}", remoteIp);
        }
    }

    private async Task<PrinterRoute?> ResolvePrinterAsync(string ip, CancellationToken ct)
        => await _printerRoutes.ResolveAsync(ip, ct);

    private async Task ReadMessagesAsync(NetworkStream stream, PrinterRoute route, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var pending = new StringBuilder();
        var decoder = Encoding.UTF8.GetDecoder();
        var chars = new char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        Task<int>? readTask = null;

        while (!ct.IsCancellationRequested)
        {
            readTask ??= stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).AsTask();
            var completed = await Task.WhenAny(readTask, Task.Delay(PartialMessageFlushInterval, ct));

            if (completed != readTask)
            {
                await PersistPendingMessageAsync(pending, route, ct);
                continue;
            }

            var read = await readTask;
            readTask = null;

            if (read == 0)
            {
                await PersistPendingMessageAsync(pending, route, ct);
                return;
            }

            var charCount = decoder.GetChars(buffer, 0, read, chars, 0, flush: false);
            pending.Append(chars, 0, charCount);
            await PersistCompleteMessagesAsync(pending, route, ct);
        }
    }

    private async Task DrainClientAsync(NetworkStream stream, string remoteIp, CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct) > 0)
        {
            _logger.LogDebug("Discarded printer log payload from unregistered IP {Ip}", remoteIp);
        }
    }

    private async Task PersistCompleteMessagesAsync(StringBuilder pending, PrinterRoute route, CancellationToken ct)
    {
        while (true)
        {
            var newlineIndex = IndexOfNewline(pending);
            if (newlineIndex < 0)
            {
                return;
            }

            var line = pending.ToString(0, newlineIndex);
            var removeLength = newlineIndex + 1;
            if (line.EndsWith('\r'))
            {
                line = line[..^1];
            }

            pending.Remove(0, removeLength);
            await PersistMessageAsync(line, route, ct);
        }
    }

    private async Task PersistPendingMessageAsync(StringBuilder pending, PrinterRoute route, CancellationToken ct)
    {
        if (pending.Length == 0)
        {
            return;
        }

        var line = pending.ToString();
        pending.Clear();
        await PersistMessageAsync(line, route, ct);
    }

    private async Task PersistMessageAsync(string line, PrinterRoute route, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        var (eventType, severity, message) = ParseLine(line);
        var alert = PrinterAlertNormalizer.Normalize(message, eventType, severity);

        var ev = new PrinterEvent
        {
            PrinterId = route.PrinterId,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            RawMessage = message,
            Severity = alert.Severity,
            AlertKey = alert.AlertKey,
            AlertTitle = alert.AlertTitle,
            AlertCategory = alert.AlertCategory,
            AlertDetail = alert.AlertDetail,
            FriendlyMessage = alert.FriendlyMessage,
            AlertTrainingLevel = alert.TrainingLevel,
        };

        try
        {
            var eventRepo = new PrinterEventRepository(new DirectDivisionDbContextFactory(route.ConnectionString, _dataProtectionProvider));
            await eventRepo.AddAsync(ev, ct);
            _logger.LogInformation("Captured printer log event for {PrinterId}: {EventType} {Message}", route.PrinterId, eventType, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist PrinterEvent for printer {PrinterId}", route.PrinterId);
        }
    }

    private static int IndexOfNewline(StringBuilder value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Parses a raw log line into (EventType, Severity, Message).
    /// Handles common T8000 log formats. Extend this as real log formats are discovered.
    /// </summary>
    private static (string EventType, string? Severity, string Message) ParseLine(string line)
    {
        string? severity = null;
        string eventType = "Info";

        var upper = line.ToUpperInvariant();

        if (upper.Contains("ERROR") ||
            upper.Contains("FAULT") ||
            upper.Contains("FAIL") ||
            upper.Contains("PRINTER ERROR") ||
            upper.Contains(" ER_") ||
            upper.StartsWith("ER_"))
        {
            severity = "Error";
            eventType = "Error";
        }
        else if (upper.Contains("WARN") || upper.Contains("LOW") || upper.Contains("EMPTY"))
        {
            severity = "Warning";
            eventType = "Warning";
        }
        else if (upper.Contains("JOB") || upper.Contains("PRINT") || upper.Contains("START"))
        {
            eventType = "JobStarted";
            severity = "Info";
        }

        return (eventType, severity, line.Trim());
    }
}
