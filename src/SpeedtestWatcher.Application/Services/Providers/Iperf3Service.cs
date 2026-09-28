using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SpeedtestWatcher.Application.Resources;
using SpeedtestWatcher.Application.Enums;
using SpeedtestWatcher.Application.Models;
using SpeedtestWatcher.Application.Services.Interfaces;
using SpeedtestWatcher.Application.Utils;
using SpeedtestWatcher.Application.Services.Providers.Interfaces;

namespace SpeedtestWatcher.Application.Services.Providers;

internal sealed partial class Iperf3Service : ISpeedtestProviderService
{
    private const int PingAttempts = 5;
    private const int UdpDurationSeconds = 5;
    private const string UdpBitrate = "1M";
    private const string ConnectTimeoutMilliseconds = "5000";

    private static readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan _udpTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<Iperf3Service> _logger;

    public Iperf3Service(ILogger<Iperf3Service> logger)
    {
        _logger = logger;
    }

    public SpeedtestProvider Provider => SpeedtestProvider.Iperf3;

    public string Title => "iperf3";

    public string Description => ApplicationStrings.Iperf3Description;

    public string BinaryName => "iperf3";

    public ServerCatalog? Servers => null;

    public string? DownloadUrl(PlatformTarget target) => null;

    public ToolArguments BuildArguments(RunOptions options) =>
        new([.. TcpArguments(ProbeTarget.Parse(options.CustomServerUrl ?? ""), options), "-R"]);

    public SpeedtestExecutionResult ParseResult(ToolOutput output, RunOptions options)
    {
        var server = ProbeTarget.Parse(options.CustomServerUrl ?? "");
        if (ReadTransfer(output) is not { } download) return ToolFailure.Because(ExplainFailure(output, server));

        return new SpeedtestExecutionResult
        {
            Success = true,
            Download = download.MegabitsPerSecond,
            DownloadBytes = download.Bytes,
            Time = download.Seconds,
            ServerName = server?.ToString(),
            ServerHost = server?.Host
        };
    }

    public async Task<SpeedtestExecutionResult> RunAsync(ICliProcessService processes, string binaryPath, RunOptions options, CancellationToken cancellationToken = default)
    {
        if (ProbeTarget.Parse(options.CustomServerUrl ?? "") is not { } server)
            return ToolFailure.Because(ApplicationStrings.NoIperf3ServersConfigured);

        var downloadRun = await processes.RunAsync(Title, binaryPath, BuildArguments(options).Arguments, cancellationToken: cancellationToken);
        if (downloadRun.Output is not { } downloadOutput) return ToolFailure.Because(downloadRun.FailureMessage!);
        if (ReadTransfer(downloadOutput) is not { } download) return ToolFailure.Because(ExplainFailure(downloadOutput, server));

        var uploadRun = await processes.RunAsync(Title, binaryPath, TcpArguments(server, options), cancellationToken: cancellationToken);
        if (uploadRun.Output is not { } uploadOutput) return ToolFailure.Because(uploadRun.FailureMessage!);
        if (ReadTransfer(uploadOutput) is not { } upload) return ToolFailure.Because(ExplainFailure(uploadOutput, server));

        var udpRun = await processes.RunAsync(Title, binaryPath, UdpArguments(server, options), _udpTimeout, cancellationToken);
        var udp = udpRun.Output is { } udpOutput ? ReadTransfer(udpOutput) : null;
        if (udp is not { Packets: > 0 })
        {
            LogUdpTestFailed(server.ToString(), UdpFailure(udpRun, udp, server));
            udp = null;
        }

        if (await MeasurePingAsync(server, cancellationToken) is not { } ping)
            return ToolFailure.Because(ApplicationStrings.Format(ApplicationStrings.Iperf3ServerUnreachable, server));

        return new SpeedtestExecutionResult
        {
            Success = true,
            Ping = (int)Math.Round(ping),
            Jitter = udp?.JitterMilliseconds,
            PacketLoss = udp?.LostPercent,
            Download = download.MegabitsPerSecond,
            Upload = upload.MegabitsPerSecond,
            DownloadBytes = download.Bytes,
            UploadBytes = upload.Bytes,
            Time = download.Seconds + upload.Seconds,
            ServerName = server.ToString(),
            ServerHost = server.Host
        };
    }

    private string? UdpFailure(ProcessOutcome run, Transfer? reading, ProbeTarget server) => (run.Output, reading) switch
    {
        (null, _) => run.FailureMessage,
        (_, not null) => "no packets arrived",
        ({ } output, null) => ExplainFailure(output, server)
    };

    private static async Task<double?> MeasurePingAsync(ProbeTarget server, CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(server.Host, cancellationToken);
        }
        catch (SocketException)
        {
            return null;
        }

        var handshakes = await Task.WhenAll(Enumerable.Range(0, PingAttempts).Select(_ => HandshakeAsync(addresses, server.Port, cancellationToken)));
        var answered = handshakes.OfType<double>().Order().ToList();
        return answered.Count > 0 ? answered[answered.Count / 2] : null;
    }

    private static async Task<double?> HandshakeAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attempt.CancelAfter(_pingTimeout);

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        var started = Stopwatch.GetTimestamp();
        try
        {
            await socket.ConnectAsync(addresses, port, attempt.Token);
            return Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private static List<string> TcpArguments(ProbeTarget? server, RunOptions options)
    {
        var arguments = new List<string>();

        if (server != null)
            arguments.AddRange(["-c", server.Host, "-p", server.Port.ToString(CultureInfo.InvariantCulture)]);

        arguments.AddRange(["-J", "--connect-timeout", ConnectTimeoutMilliseconds]);

        if (!string.IsNullOrEmpty(options.NetworkInterface))
            arguments.Add($"--bind={options.NetworkInterface}");

        return arguments;
    }

    private static List<string> UdpArguments(ProbeTarget server, RunOptions options) =>
        [.. TcpArguments(server, options), "-u", "-b", UdpBitrate, "-t", UdpDurationSeconds.ToString(CultureInfo.InvariantCulture), "-R"];

    private static Transfer? ReadTransfer(ToolOutput output)
    {
        if (ParseDocument(output.Output) is not { } root || root.TryGetProperty("error", out _)) return null;
        if (Received(root) is not { } sum || JsonOutput.Number(sum, "bits_per_second") is not { } bitsPerSecond) return null;

        return new Transfer(
            Math.Round(bitsPerSecond / 1_000_000.0, 2),
            JsonOutput.Number(sum, "bytes") is { } bytes ? (long)bytes : null,
            JsonOutput.Number(sum, "seconds") is { } seconds ? (int)Math.Round(seconds) : 0,
            JsonOutput.Number(sum, "jitter_ms") is { } jitter ? Math.Round(jitter, 2) : null,
            JsonOutput.Number(sum, "lost_percent") is { } lost ? Math.Round(lost, 2) : null,
            JsonOutput.Number(sum, "packets") is { } packets ? (long)packets : null);
    }

    private string ExplainFailure(ToolOutput output, ProbeTarget? server)
    {
        if (ParseDocument(output.Output) is { } root && JsonOutput.Text(root, "error") is { } error)
            return Explain(error, server?.ToString() ?? "the configured address");

        return ToolFailure.Unrecognised(Title, output).Error!;
    }

    private string Explain(string error, string server)
    {
        if (error.Contains("busy running a test", StringComparison.OrdinalIgnoreCase))
            return ApplicationStrings.Format(ApplicationStrings.Iperf3ServerBusy, server);

        if (error.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
            return ApplicationStrings.Format(ApplicationStrings.Iperf3ServerRefused, server);

        if (error.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || error.Contains("No route to host", StringComparison.OrdinalIgnoreCase))
            return ApplicationStrings.Format(ApplicationStrings.Iperf3ServerUnreachable, server);

        return $"{Title} stopped with an error: {error}";
    }

    private static JsonElement? Received(JsonElement root)
    {
        if (!root.TryGetProperty("end", out var end)) return null;
        if (end.TryGetProperty("sum_received", out var received)) return received;
        return end.TryGetProperty("sum", out var sum) ? sum : null;
    }

    private static JsonElement? ParseDocument(string output)
    {
        var text = output.Trim();
        if (text.Length == 0) return null;

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record Transfer(double MegabitsPerSecond, long? Bytes, int Seconds, double? JitterMilliseconds, double? LostPercent, long? Packets);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The iperf3 UDP test against {Server} gave no packet loss or jitter ({Reason}). The server's UDP port is probably closed")]
    private partial void LogUdpTestFailed(string server, string? reason);
}
