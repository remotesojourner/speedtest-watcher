using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One result in a data backup, in the format of a JSON results export.
/// </summary>
public sealed record DataBackupSpeedtest
{
    /// <summary>
    /// When the test ran. A time without an offset is read as UTC. Required: a backup with a result missing it is rejected. A result with the same timestamp as a stored one is skipped.
    /// </summary>
    /// <example>2026-09-14T08:05:00Z</example>
    public DateTime? Created { get; init; }

    /// <summary>
    /// <c>completed</c>, <c>failed</c> or <c>skipped</c>. Anything else counts as <c>failed</c> when there's an error, and as <c>completed</c> otherwise.
    /// </summary>
    /// <example>completed</example>
    public string? Status { get; init; }

    /// <summary>
    /// <c>auto</c> or <c>custom</c>. Anything else counts as <c>auto</c>.
    /// </summary>
    /// <example>auto</example>
    public string? Type { get; init; }

    /// <summary>
    /// Latency in milliseconds.
    /// </summary>
    /// <example>12</example>
    public int Ping { get; init; }

    /// <summary>
    /// Jitter in milliseconds.
    /// </summary>
    /// <example>0.4</example>
    public double? Jitter { get; init; }

    /// <summary>
    /// Download speed in Mbps.
    /// </summary>
    /// <example>941.25</example>
    public double Download { get; init; }

    /// <summary>
    /// Upload speed in Mbps.
    /// </summary>
    /// <example>110.5</example>
    public double Upload { get; init; }

    /// <summary>
    /// How long the test took, in seconds.
    /// </summary>
    /// <example>14</example>
    public int Time { get; init; }

    /// <summary>
    /// Whether the result met its targets.
    /// </summary>
    public bool? Healthy { get; init; }

    /// <summary>
    /// The ping target, in milliseconds, when the test ran.
    /// </summary>
    public int? ThresholdPing { get; init; }

    /// <summary>
    /// The download target, in Mbps, when the test ran.
    /// </summary>
    public double? ThresholdDownload { get; init; }

    /// <summary>
    /// The upload target, in Mbps, when the test ran.
    /// </summary>
    public double? ThresholdUpload { get; init; }

    /// <summary>
    /// The packet loss maximum, as a percentage, when the test ran.
    /// </summary>
    public double? ThresholdPacketLoss { get; init; }

    /// <summary>
    /// The bufferbloat maximum, in milliseconds, when the test ran.
    /// </summary>
    public double? ThresholdBufferbloat { get; init; }

    /// <summary>
    /// Why the test failed or was skipped.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Packet loss as a percentage.
    /// </summary>
    /// <example>0</example>
    public double? PacketLoss { get; init; }

    /// <summary>
    /// Bufferbloat while downloading, in milliseconds, when the export carried it.
    /// </summary>
    /// <example>5.1</example>
    public double? BufferbloatDown { get; init; }

    /// <summary>
    /// Bufferbloat while uploading, in milliseconds, when the export carried it.
    /// </summary>
    /// <example>42.8</example>
    public double? BufferbloatUp { get; init; }

    /// <summary>
    /// The idle median latency, in milliseconds, when the export carried it.
    /// </summary>
    /// <example>11.4</example>
    public double? LatencyIdle { get; init; }

    /// <summary>
    /// The median latency under load, in milliseconds, when the export carried it.
    /// </summary>
    /// <example>13.2</example>
    public double? LatencyLoaded { get; init; }

    /// <summary>
    /// The 95th percentile latency under load, in milliseconds, when the export carried it.
    /// </summary>
    /// <example>64.2</example>
    public double? LatencyLoadedTail { get; init; }

    /// <summary>
    /// Bytes downloaded during the test.
    /// </summary>
    /// <example>903347628</example>
    public long? DownloadBytes { get; init; }

    /// <summary>
    /// Bytes uploaded during the test.
    /// </summary>
    /// <example>88429797</example>
    public long? UploadBytes { get; init; }

    /// <summary>
    /// The address the connection check saw when the test ran, or <c>null</c> for read-only visitors.
    /// </summary>
    /// <example>203.0.113.9</example>
    public string? PublicIp { get; init; }

    /// <summary>
    /// The id of the server the test ran against.
    /// </summary>
    public int ServerId { get; init; }

    /// <summary>
    /// The name of the server the test ran against.
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// The host of the server the test ran against.
    /// </summary>
    public string? ServerHost { get; init; }

    /// <summary>
    /// The provider's own id for the result.
    /// </summary>
    public string? ResultId { get; init; }

    public static DataBackupSpeedtest From(SpeedtestImportRow row) => new()
    {
        Created = row.Created,
        Status = row.Status,
        Type = row.Type,
        Ping = row.Ping,
        Jitter = row.Jitter,
        Download = row.Download,
        Upload = row.Upload,
        Time = row.Time,
        Healthy = row.Healthy,
        ThresholdPing = row.ThresholdPing,
        ThresholdDownload = row.ThresholdDownload,
        ThresholdUpload = row.ThresholdUpload,
        ThresholdPacketLoss = row.ThresholdPacketLoss,
        ThresholdBufferbloat = row.ThresholdBufferbloat,
        Error = row.Error,
        PacketLoss = row.PacketLoss,
        BufferbloatDown = row.BufferbloatDown,
        BufferbloatUp = row.BufferbloatUp,
        LatencyIdle = row.LatencyIdle,
        LatencyLoaded = row.LatencyLoaded,
        LatencyLoadedTail = row.LatencyLoadedTail,
        DownloadBytes = row.DownloadBytes,
        UploadBytes = row.UploadBytes,
        PublicIp = row.PublicIp,
        ServerId = row.ServerId,
        ServerName = row.ServerName,
        ServerHost = row.ServerHost,
        ResultId = row.ResultId
    };

    public SpeedtestImportRow ToImportRow() => new()
    {
        Created = Created,
        Status = Status,
        Type = Type,
        Ping = Ping,
        Jitter = Jitter,
        Download = Download,
        Upload = Upload,
        Time = Time,
        Healthy = Healthy,
        ThresholdPing = ThresholdPing,
        ThresholdDownload = ThresholdDownload,
        ThresholdUpload = ThresholdUpload,
        ThresholdPacketLoss = ThresholdPacketLoss,
        ThresholdBufferbloat = ThresholdBufferbloat,
        Error = Error,
        PacketLoss = PacketLoss,
        BufferbloatDown = BufferbloatDown,
        BufferbloatUp = BufferbloatUp,
        LatencyIdle = LatencyIdle,
        LatencyLoaded = LatencyLoaded,
        LatencyLoadedTail = LatencyLoadedTail,
        DownloadBytes = DownloadBytes,
        UploadBytes = UploadBytes,
        PublicIp = PublicIp,
        ServerId = ServerId,
        ServerName = ServerName,
        ServerHost = ServerHost,
        ResultId = ResultId
    };
}
