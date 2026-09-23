using System.Diagnostics;
using System.Net.Sockets;

namespace SpeedtestWatcher.Application.Monitoring;

internal sealed class TcpConnectionProbe : IConnectionProbe
{
    public async Task<ProbeResult> ProbeAsync(IReadOnlyList<ProbeTarget> targets, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (targets.Count == 0) return new ProbeResult(0, 0, null);

        var attempts = await Task.WhenAll(targets.Select(target => ConnectAsync(target, timeout, cancellationToken)));
        var answered = attempts.OfType<double>().ToList();

        return new ProbeResult(answered.Count, targets.Count, answered.Count > 0 ? answered.Min() : null);
    }

    private static async Task<double?> ConnectAsync(ProbeTarget target, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attempt.CancelAfter(timeout);

        using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        var started = Stopwatch.GetTimestamp();
        try
        {
            await socket.ConnectAsync(target.Host, target.Port, attempt.Token);
            return Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
