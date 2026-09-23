using System.Globalization;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One stored speedtest result. Failed and skipped results have <c>-1</c> for ping, download and upload.
/// </summary>
public sealed record SpeedtestResponse
{
    /// <summary>
    /// The result's id.
    /// </summary>
    /// <example>42</example>
    public required int Id { get; init; }

    /// <summary>
    /// When the test ran, in UTC.
    /// </summary>
    /// <example>2026-09-14T20:35:00Z</example>
    public required DateTime Created { get; init; }

    /// <summary>
    /// Whether the test completed, failed, or was skipped before it ran.
    /// </summary>
    public required TestStatus Status { get; init; }

    /// <summary>
    /// What started the test: <c>auto</c> for the schedule, <c>custom</c> for a manual run.
    /// </summary>
    public required TestType Type { get; init; }

    /// <summary>
    /// Latency in milliseconds, or <c>-1</c> when the test didn't complete.
    /// </summary>
    /// <example>12</example>
    public required int Ping { get; init; }

    /// <summary>
    /// Jitter in milliseconds, when the provider reports it.
    /// </summary>
    /// <example>0.4</example>
    public required double? Jitter { get; init; }

    /// <summary>
    /// Download speed in Mbps, or <c>-1</c> when the test didn't complete.
    /// </summary>
    /// <example>941.25</example>
    public required double Download { get; init; }

    /// <summary>
    /// Upload speed in Mbps, or <c>-1</c> when the test didn't complete.
    /// </summary>
    /// <example>110.5</example>
    public required double Upload { get; init; }

    /// <summary>
    /// How long the test took, in seconds.
    /// </summary>
    /// <example>14</example>
    public required int Time { get; init; }

    /// <summary>
    /// Whether the result met the targets it was judged against. <c>null</c> when the test didn't complete or no targets were set.
    /// </summary>
    public required bool? Healthy { get; init; }

    /// <summary>
    /// The highest acceptable ping, in milliseconds, when the test ran.
    /// </summary>
    /// <example>25</example>
    public required int? ThresholdPing { get; init; }

    /// <summary>
    /// The lowest acceptable download speed, in Mbps, when the test ran.
    /// </summary>
    /// <example>900</example>
    public required double? ThresholdDownload { get; init; }

    /// <summary>
    /// The lowest acceptable upload speed, in Mbps, when the test ran.
    /// </summary>
    /// <example>100</example>
    public required double? ThresholdUpload { get; init; }

    /// <summary>
    /// Why the test failed or was skipped.
    /// </summary>
    public required string? Error { get; init; }

    /// <summary>
    /// The id of the server the test ran against, or <c>0</c> when there was none.
    /// </summary>
    /// <example>12345</example>
    public required int ServerId { get; init; }

    /// <summary>
    /// The name of the server the test ran against.
    /// </summary>
    /// <example>Acme Fibre</example>
    public required string? ServerName { get; init; }

    /// <summary>
    /// The host of the server the test ran against.
    /// </summary>
    /// <example>speed.acme.example</example>
    public required string? ServerHost { get; init; }

    /// <summary>
    /// The provider's own id for the result, such as Ookla's result id.
    /// </summary>
    public required string? ResultId { get; init; }

    /// <summary>
    /// Packet loss as a percentage, when the provider measures it. Only Ookla does.
    /// </summary>
    /// <example>0</example>
    public required double? PacketLoss { get; init; }

    /// <summary>
    /// How much longer the line took to answer under load than when idle, in milliseconds. The app measures this itself around every test, the same way for every provider.
    /// </summary>
    /// <example>18.5</example>
    public required double? Bufferbloat { get; init; }

    /// <summary>
    /// The median answer time in the three seconds before the test started, in milliseconds.
    /// </summary>
    /// <example>13.2</example>
    public required double? LatencyIdle { get; init; }

    /// <summary>
    /// The median answer time while the test was running, in milliseconds.
    /// </summary>
    /// <example>31.7</example>
    public required double? LatencyLoaded { get; init; }

    /// <summary>
    /// The 95th percentile answer time while the test was running, in milliseconds. This is the tail that calls and games feel, where the median is what the line usually did.
    /// </summary>
    /// <example>64.2</example>
    public required double? LatencyLoadedTail { get; init; }

    /// <summary>
    /// Bytes downloaded during the test, when the provider reports them.
    /// </summary>
    /// <example>903347628</example>
    public required long? DownloadBytes { get; init; }

    /// <summary>
    /// Bytes uploaded during the test, when the provider reports them.
    /// </summary>
    /// <example>88429797</example>
    public required long? UploadBytes { get; init; }

    /// <summary>
    /// The public IP the test ran from, when the pre-test check looked it up. Only for full access; read-only visitors get <c>null</c>.
    /// </summary>
    /// <example>203.0.113.9</example>
    public required string? PublicIp { get; init; }

    public static SpeedtestResponse From(SpeedtestDto test) => new()
    {
        Id = test.Id,
        Created = DateTime.Parse(test.Created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Status = test.Status,
        Type = test.Type,
        Ping = test.Ping,
        Jitter = test.Jitter,
        Download = test.Download,
        Upload = test.Upload,
        Time = test.Time,
        Healthy = test.Healthy,
        ThresholdPing = test.ThresholdPing,
        ThresholdDownload = test.ThresholdDownload,
        ThresholdUpload = test.ThresholdUpload,
        Error = test.Error,
        ServerId = test.ServerId,
        ServerName = test.ServerName,
        ServerHost = test.ServerHost,
        ResultId = test.ResultId,
        PacketLoss = test.PacketLoss,
        Bufferbloat = test.Bufferbloat,
        LatencyIdle = test.LatencyIdle,
        LatencyLoaded = test.LatencyLoaded,
        LatencyLoadedTail = test.LatencyLoadedTail,
        DownloadBytes = test.DownloadBytes,
        UploadBytes = test.UploadBytes,
        PublicIp = test.PublicIp
    };
}
