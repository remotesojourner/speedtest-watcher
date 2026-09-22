using System.Globalization;
using SpeedtestWatcher.Application.Common;
using SpeedtestWatcher.Application.Speedtests;

namespace SpeedtestWatcher.Application.Statistics;

public sealed class StatisticsService
{
    public const int MaxChartPoints = 300;

    private readonly ISpeedtestRepository _results;

    public StatisticsService(ISpeedtestRepository results)
    {
        _results = results;
    }

    public async Task<OperationResult<SpeedtestStatistics>> GetAsync(string? from, string? to, string? timeZoneId, CancellationToken cancellationToken = default)
    {
        var timeZone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(timeZoneId) && !TimeZones.TryFindTimeZone(timeZoneId, out timeZone))
            return OperationResult.Invalid($"{timeZoneId} isn't a time zone this server knows. Use an IANA name such as Europe/London.");

        var now = DateTime.UtcNow;
        var today = TimeZones.InTimeZone(now, timeZone).Date;
        var fromDate = from ?? today.AddDays(-7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toDate = to ?? today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var range = new StatisticsRange(
            fromDate,
            toDate,
            RangeBound(fromDate, now.AddDays(-7), endOfDay: false, timeZone),
            RangeBound(toDate, now, endOfDay: true, timeZone),
            timeZone);

        var rows = await _results.ListCreatedBetweenAsync(range.FromUtc, range.ToUtc, cancellationToken);
        return OperationResult.Ok(Compute(rows, range));
    }

    public static SpeedtestStatistics Compute(IReadOnlyList<Speedtest> rows, StatisticsRange range)
    {
        var completed = rows.Where(row => row.Status == TestStatus.Completed).ToList();
        var hasCompleted = completed.Count > 0;

        return new SpeedtestStatistics(
            Tests: new TestsCountDto { Total = rows.Count, Failed = rows.Count(row => row.Status == TestStatus.Failed) },
            Ping: hasCompleted ? WholeNumberRange(completed.Select(row => row.Ping).ToList()) : null,
            Jitter: completed.Any(row => row.Jitter.HasValue) ? DecimalRange(completed.Where(row => row.Jitter.HasValue).Select(row => row.Jitter!.Value).ToList()) : null,
            Download: hasCompleted ? DecimalRange(completed.Select(row => row.Download).ToList()) : null,
            Upload: hasCompleted ? DecimalRange(completed.Select(row => row.Upload).ToList()) : null,
            Time: hasCompleted ? WholeNumberRange(completed.Select(row => row.Time).ToList()) : null,
            ChartPoints: rows.Count <= MaxChartPoints ? PointPerResult(rows) : PointPerTimeBucket(rows, range.FromUtc, range.ToUtc),
            HourlyAverages: HourlyAverages(completed, range.TimeZone),
            Consistency: hasCompleted ? Consistency(completed) : new ConsistencyDto(),
            RawDataPoints: rows.Count,
            Downsampled: rows.Count > MaxChartPoints,
            DateRange: new DateRangeDto
            {
                From = range.From,
                To = range.To,
                Days = (int)Math.Ceiling((range.ToUtc - range.FromUtc).TotalDays)
            });
    }

    private static DateTime RangeBound(string raw, DateTime fallback, bool endOfDay, TimeZoneInfo timeZone)
    {
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            return endOfDay
                ? TimeZones.WallClockToUtc(day.Date.AddDays(1), timeZone).AddTicks(-1)
                : TimeZones.WallClockToUtc(day.Date, timeZone);

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment))
            return moment.Kind == DateTimeKind.Unspecified ? TimeZones.WallClockToUtc(moment, timeZone) : moment.ToUniversalTime();

        return fallback;
    }

    private static MetricStatsDto<int> WholeNumberRange(List<int> values) => new()
    {
        Min = values.Min(),
        Max = values.Max(),
        Avg = (int)Math.Round(values.Average())
    };

    private static MetricStatsDto<double> DecimalRange(List<double> values) => new()
    {
        Min = Math.Round(values.Min(), 2),
        Max = Math.Round(values.Max(), 2),
        Avg = Math.Round(values.Average(), 2)
    };

    private static ConsistencyDto Consistency(List<Speedtest> completed)
    {
        var pingStandardDeviation = Math.Round(StandardDeviation(completed.Select(row => (double)row.Ping).ToList()), 2);
        return new ConsistencyDto
        {
            Download = ConsistencyOf(completed.Select(row => row.Download).ToList()),
            Upload = ConsistencyOf(completed.Select(row => row.Upload).ToList()),
            Ping = new PingConsistencyDto { StdDev = pingStandardDeviation, Jitter = pingStandardDeviation }
        };
    }

    private static ConsistencyItemDto ConsistencyOf(List<double> values)
    {
        var standardDeviation = StandardDeviation(values);
        var mean = values.Average();
        return new ConsistencyItemDto
        {
            StdDev = Math.Round(standardDeviation, 2),
            Consistency = mean > 0 ? Math.Round(Math.Max(0, 100 - (standardDeviation / mean * 100)), 1) : 100
        };
    }

    private static double StandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        return Math.Sqrt(values.Sum(value => Math.Pow(value - mean, 2)) / values.Count);
    }

    private static List<HourlyAverageDto> HourlyAverages(List<Speedtest> completed, TimeZoneInfo timeZone)
    {
        var byHour = completed.ToLookup(row => TimeZones.InTimeZone(row.Created, timeZone).Hour);
        return Enumerable.Range(0, 24).Select(hour =>
        {
            var inHour = byHour[hour].ToList();
            var jitters = inHour.Where(row => row.Jitter.HasValue).Select(row => row.Jitter!.Value).ToList();
            return new HourlyAverageDto
            {
                Hour = hour,
                Download = inHour.Count > 0 ? Math.Round(inHour.Average(row => row.Download), 2) : null,
                Upload = inHour.Count > 0 ? Math.Round(inHour.Average(row => row.Upload), 2) : null,
                Ping = inHour.Count > 0 ? (int)Math.Round(inHour.Average(row => row.Ping)) : null,
                Jitter = jitters.Count > 0 ? Math.Round(jitters.Average(), 2) : null,
                Count = inHour.Count
            };
        }).ToList();
    }

    private static List<ChartPoint> PointPerResult(IReadOnlyList<Speedtest> rows) => rows.Select(row =>
    {
        var failed = row.Status == TestStatus.Failed;
        var hasReadings = row.Status == TestStatus.Completed;
        return new ChartPoint(
            row.Created,
            failed,
            failed ? row.Error : null,
            hasReadings ? row.Ping : null,
            hasReadings ? row.Jitter : null,
            hasReadings ? row.Download : null,
            hasReadings ? row.Upload : null,
            hasReadings ? row.Time : null);
    }).ToList();

    private static List<ChartPoint> PointPerTimeBucket(IReadOnlyList<Speedtest> rows, DateTime fromUtc, DateTime toUtc)
    {
        var fromMs = new DateTimeOffset(fromUtc).ToUnixTimeMilliseconds();
        var toMs = new DateTimeOffset(toUtc).ToUnixTimeMilliseconds();
        var bucketSize = (double)(toMs - fromMs) / MaxChartPoints;

        var buckets = Enumerable.Range(0, MaxChartPoints)
            .Select(i => (Start: fromMs + (long)(i * bucketSize), Rows: new List<Speedtest>()))
            .ToList();

        foreach (var row in rows)
        {
            var rowMs = new DateTimeOffset(row.Created).ToUnixTimeMilliseconds();
            var bucketIndex = (int)Math.Min(Math.Floor((rowMs - fromMs) / bucketSize), MaxChartPoints - 1);
            if (bucketIndex >= 0 && bucketIndex < MaxChartPoints) buckets[bucketIndex].Rows.Add(row);
        }

        var points = new List<ChartPoint>();
        foreach (var bucket in buckets)
        {
            var valid = bucket.Rows.Where(row => row.Status == TestStatus.Completed).ToList();
            var failedCount = bucket.Rows.Count(row => row.Status == TestStatus.Failed);
            if (valid.Count == 0 && failedCount == 0) continue;

            var midpoint = DateTimeOffset.FromUnixTimeMilliseconds(bucket.Start + (long)(bucketSize / 2)).UtcDateTime;
            var failedSummary = failedCount > 0 ? $"{failedCount} failed in period" : null;

            if (valid.Count == 0)
            {
                points.Add(new ChartPoint(midpoint, true, failedSummary, null, null, null, null, null));
                continue;
            }

            var jitters = valid.Where(row => row.Jitter.HasValue).Select(row => row.Jitter!.Value).ToList();
            points.Add(new ChartPoint(
                midpoint,
                failedCount > 0,
                failedSummary,
                (int)Math.Round(valid.Average(row => row.Ping)),
                jitters.Count > 0 ? Math.Round(jitters.Average(), 2) : null,
                Math.Round(valid.Average(row => row.Download), 2),
                Math.Round(valid.Average(row => row.Upload), 2),
                (int)Math.Round(valid.Average(row => row.Time))));
        }

        return points;
    }
}
