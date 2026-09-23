using SpeedtestWatcher.Application.Statistics;

namespace SpeedtestWatcher.Web.Api.Contracts;

/// <summary>
/// Summary figures and chart points for a period.
/// </summary>
public sealed record StatisticsResponse
{
    /// <summary>
    /// The period the figures cover.
    /// </summary>
    public required StatisticsPeriodResponse DateRange { get; init; }

    /// <summary>
    /// How many tests ran in the period and how many of them failed.
    /// </summary>
    public required TestCountsResponse Tests { get; init; }

    /// <summary>
    /// Ping of the completed tests, in milliseconds. <c>null</c> when none completed.
    /// </summary>
    public required MetricSummaryResponse? Ping { get; init; }

    /// <summary>
    /// Jitter of the completed tests, in milliseconds. <c>null</c> when none reported jitter.
    /// </summary>
    public required MetricSummaryResponse? Jitter { get; init; }

    /// <summary>
    /// Download speed of the completed tests, in Mbps. <c>null</c> when none completed.
    /// </summary>
    public required MetricSummaryResponse? Download { get; init; }

    /// <summary>
    /// Upload speed of the completed tests, in Mbps. <c>null</c> when none completed.
    /// </summary>
    public required MetricSummaryResponse? Upload { get; init; }

    /// <summary>
    /// How long the completed tests took, in seconds. <c>null</c> when none completed.
    /// </summary>
    public required MetricSummaryResponse? Time { get; init; }

    /// <summary>
    /// How steady the completed tests' readings were.
    /// </summary>
    public required ConsistencyResponse Consistency { get; init; }

    /// <summary>
    /// Packet loss across the completed tests that measured it, as percentages. Only Ookla measures it.
    /// </summary>
    public required MetricSummaryResponse? PacketLoss { get; init; }

    /// <summary>
    /// Bufferbloat in milliseconds across the completed tests that have a figure, or <c>null</c> when none do.
    /// </summary>
    public required MetricSummaryResponse? Bufferbloat { get; init; }

    /// <summary>
    /// Bytes the tests in the period moved, download and upload together, as far as the providers reported them.
    /// </summary>
    /// <example>991777425</example>
    public required long DataUsedBytes { get; init; }

    /// <summary>
    /// Averages of the completed tests for each hour of the day, 0 to 23, in the requested time zone.
    /// </summary>
    public required IReadOnlyList<HourlyAverageResponse> HourlyAverages { get; init; }

    /// <summary>
    /// Chart points, oldest first. Up to 300 tests give one point each. More are averaged into 300 equal slices of the period, and slices without tests are left out.
    /// </summary>
    public required IReadOnlyList<ChartPointResponse> Points { get; init; }

    /// <summary>
    /// How many tests ran in the period, before any averaging.
    /// </summary>
    /// <example>4</example>
    public required int RawDataPoints { get; init; }

    /// <summary>
    /// Whether the points were averaged down from more tests.
    /// </summary>
    public required bool Downsampled { get; init; }

    public static StatisticsResponse From(SpeedtestStatistics statistics) => new()
    {
        DateRange = new StatisticsPeriodResponse { From = statistics.DateRange.From, To = statistics.DateRange.To, Days = statistics.DateRange.Days },
        Tests = new TestCountsResponse { Total = statistics.Tests.Total, Failed = statistics.Tests.Failed },
        Ping = MetricSummaryResponse.From(statistics.Ping),
        Jitter = MetricSummaryResponse.From(statistics.Jitter),
        Download = MetricSummaryResponse.From(statistics.Download),
        Upload = MetricSummaryResponse.From(statistics.Upload),
        Time = MetricSummaryResponse.From(statistics.Time),
        Consistency = ConsistencyResponse.From(statistics.Consistency),
        PacketLoss = MetricSummaryResponse.From(statistics.PacketLoss),
        Bufferbloat = MetricSummaryResponse.From(statistics.Bufferbloat),
        DataUsedBytes = statistics.DataUsedBytes,
        HourlyAverages = statistics.HourlyAverages.Select(HourlyAverageResponse.From).ToList(),
        Points = statistics.ChartPoints.Select(ChartPointResponse.From).ToList(),
        RawDataPoints = statistics.RawDataPoints,
        Downsampled = statistics.Downsampled
    };
}
