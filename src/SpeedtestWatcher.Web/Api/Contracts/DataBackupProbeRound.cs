using SpeedtestWatcher.Application.Storage;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// One connection check in a data backup. The latency chart is built from these.
/// </summary>
public sealed record DataBackupProbeRound
{
    /// <summary>
    /// When the check ran.
    /// </summary>
    /// <example>2026-09-14T08:05:00Z</example>
    public DateTime At { get; init; }

    /// <summary>
    /// Whether at least one target answered.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// How many targets answered.
    /// </summary>
    /// <example>3</example>
    public int Answered { get; init; }

    /// <summary>
    /// How many targets were asked.
    /// </summary>
    /// <example>3</example>
    public int Asked { get; init; }

    /// <summary>
    /// The fastest target's round trip time, in milliseconds, or <c>null</c> when none answered.
    /// </summary>
    /// <example>11.8</example>
    public double? FastestMilliseconds { get; init; }

    /// <summary>
    /// Whether a speedtest was running at the same time.
    /// </summary>
    public bool DuringTest { get; init; }

    public static DataBackupProbeRound From(DataBackupProbeRoundRow row) => new()
    {
        At = row.At,
        Passed = row.Passed,
        Answered = row.Answered,
        Asked = row.Asked,
        FastestMilliseconds = row.FastestMilliseconds,
        DuringTest = row.DuringTest
    };

    public DataBackupProbeRoundRow ToRow() => new()
    {
        At = At,
        Passed = Passed,
        Answered = Answered,
        Asked = Asked,
        FastestMilliseconds = FastestMilliseconds,
        DuringTest = DuringTest
    };
}
