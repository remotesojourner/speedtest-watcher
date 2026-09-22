using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One result to import, in the format of a JSON export. Ids in the file are ignored.
/// </summary>
public sealed record TestImportRow
{
    /// <summary>
    /// When the test ran. A time without an offset is read as UTC. A result with the same timestamp as a stored one is skipped.
    /// </summary>
    /// <example>2026-09-14T08:05:00Z</example>
    public DateTime Created { get; init; } = DateTime.UtcNow;

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
    /// Why the test failed or was skipped.
    /// </summary>
    public string? Error { get; init; }

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
        Error = Error,
        ServerId = ServerId,
        ServerName = ServerName,
        ServerHost = ServerHost,
        ResultId = ResultId
    };
}
