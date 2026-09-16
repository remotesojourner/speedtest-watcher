using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SpeedtestWatcher.Core.DTOs;
using SpeedtestWatcher.Core.Interfaces;
using SpeedtestWatcher.Core.Models;
using SpeedtestWatcher.Infrastructure.Data;

namespace SpeedtestWatcher.Infrastructure.Repositories;

public class SpeedtestRepository : ISpeedtestRepository
{
    private readonly SpeedtestWatcherDbContext _db;

    public SpeedtestRepository(SpeedtestWatcherDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(Speedtest test, CancellationToken cancellationToken = default)
    {
        _db.Speedtests.Add(test);
        await _db.SaveChangesAsync(cancellationToken);
        return test.Id;
    }

    public async Task<Speedtest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests.FindAsync([id], cancellationToken);
    }

    public async Task<Speedtest?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .OrderByDescending(t => t.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Speedtest?> GetLatestCompletedAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .Where(t => t.Status == "completed")
            .OrderByDescending(t => t.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Speedtest>> ListTestsAsync(int? afterId, int limit, string? status = null, string? type = null, bool? healthy = null, CancellationToken cancellationToken = default)
    {
        var query = Filter(status, type, healthy);
        if (afterId.HasValue && afterId.Value > 0)
        {
            query = query.Where(t => t.Id < afterId.Value);
        }

        return await query
            .OrderByDescending(t => t.Created)
            .Take(limit > 0 ? limit : 10)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Speedtest>> ListMatchingAsync(string? status, string? type, bool? healthy, IReadOnlyCollection<int>? ids = null, CancellationToken cancellationToken = default)
    {
        var query = Filter(status, type, healthy);
        if (ids != null)
        {
            query = query.Where(t => ids.Contains(t.Id));
        }

        return await query
            .OrderByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountMatchingAsync(string? status, string? type, bool? healthy, CancellationToken cancellationToken = default) =>
        Filter(status, type, healthy).CountAsync(cancellationToken);

    private IQueryable<Speedtest> Filter(string? status, string? type, bool? healthy)
    {
        var query = _db.Speedtests.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(t => t.Status == status);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(t => t.Type == type);
        }

        if (healthy.HasValue)
        {
            query = query.Where(t => t.Healthy == healthy.Value);
        }

        return query;
    }

    public async Task<List<Speedtest>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests
            .OrderByDescending(t => t.Created)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Speedtests.FindAsync([id], cancellationToken);
        if (entity == null) return false;

        _db.Speedtests.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.ExecuteSqlRawAsync("DELETE FROM speedtests", cancellationToken);
        return true;
    }

    public async Task<int> ImportTestsAsync(IEnumerable<Speedtest> tests, CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var test in tests)
        {
            if (test.Type != "custom" && test.Type != "auto")
                test.Type = "auto";

            if (test.Status != "completed" && test.Status != "failed" && test.Status != "skipped")
                test.Status = string.IsNullOrEmpty(test.Error) ? "completed" : "failed";

            _db.Speedtests.Add(test);
            count++;
        }
        await _db.SaveChangesAsync(cancellationToken);
        return count;
    }

    public async Task<int> RemoveOldTestsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        if (retentionDays <= 0) return 0;

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        return await _db.Speedtests
            .Where(t => t.Created <= cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Speedtests.CountAsync(cancellationToken);
    }

    public async Task<StatisticsDto> GetStatisticsAsync(string fromDate, string toDate, CancellationToken cancellationToken = default)
    {
        var from = ParseRangeBound(fromDate, DateTime.UtcNow.AddDays(-7).Date, endOfDay: false);
        var to = ParseRangeBound(toDate, DateTime.UtcNow, endOfDay: true);

        var entries = await _db.Speedtests
            .Where(t => t.Created >= from && t.Created <= to)
            .OrderBy(t => t.Created)
            .ToListAsync(cancellationToken);
        var completed = entries.Where(e => e.Status == "completed").ToList();

        var result = new StatisticsDto
        {
            Tests = new TestsCountDto
            {
                Total = entries.Count,
                Failed = entries.Count(e => e.Status == "failed")
            },
            RawDataPoints = entries.Count,
            Downsampled = entries.Count > MaxChartPoints,
            DateRange = new DateRangeDto
            {
                From = fromDate,
                To = toDate,
                Days = (int)Math.Ceiling((to - from).TotalDays)
            }
        };

        if (completed.Count > 0)
        {
            AddValueRanges(result, completed);
            AddConsistency(result, completed);
        }

        AddHourlyAverages(result, completed);

        if (entries.Count <= MaxChartPoints)
            AddChartPointPerResult(result, entries);
        else
            AddChartPointPerTimeBucket(result, entries, from, to);

        result.DataPoints = result.Labels.Count;
        return result;
    }

    private const int MaxChartPoints = 300;

    private static DateTime ParseRangeBound(string raw, DateTime fallback, bool endOfDay)
    {
        if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return endOfDay ? date.Date.AddDays(1).AddTicks(-1) : date.Date;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var moment))
            return moment;

        return fallback;
    }

    private static void AddValueRanges(StatisticsDto result, List<Speedtest> completed)
    {
        var pings = completed.Select(e => e.Ping).ToList();
        var downloads = completed.Select(e => e.Download).ToList();
        var uploads = completed.Select(e => e.Upload).ToList();
        var times = completed.Select(e => e.Time).ToList();
        var jitters = completed.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();

        result.Ping = new MetricStatsDto<int>
        {
            Min = pings.Min(),
            Max = pings.Max(),
            Avg = (int)Math.Round(pings.Average())
        };

        result.Download = new MetricStatsDto<double>
        {
            Min = Math.Round(downloads.Min(), 2),
            Max = Math.Round(downloads.Max(), 2),
            Avg = Math.Round(downloads.Average(), 2)
        };

        result.Upload = new MetricStatsDto<double>
        {
            Min = Math.Round(uploads.Min(), 2),
            Max = Math.Round(uploads.Max(), 2),
            Avg = Math.Round(uploads.Average(), 2)
        };

        result.Time = new MetricStatsDto<int>
        {
            Min = times.Min(),
            Max = times.Max(),
            Avg = (int)Math.Round(times.Average())
        };

        if (jitters.Count > 0)
        {
            result.Jitter = new MetricStatsDto<double>
            {
                Min = Math.Round(jitters.Min(), 2),
                Max = Math.Round(jitters.Max(), 2),
                Avg = Math.Round(jitters.Average(), 2)
            };
        }
    }

    private static void AddConsistency(StatisticsDto result, List<Speedtest> completed)
    {
        result.Consistency.Download = ConsistencyOf(completed.Select(e => e.Download).ToList());
        result.Consistency.Upload = ConsistencyOf(completed.Select(e => e.Upload).ToList());

        var pingStdDev = StandardDeviation(completed.Select(e => (double)e.Ping).ToList());
        result.Consistency.Ping = new PingConsistencyDto
        {
            StdDev = Math.Round(pingStdDev, 2),
            Jitter = Math.Round(pingStdDev, 2)
        };
    }

    private static ConsistencyItemDto ConsistencyOf(List<double> values)
    {
        var stdDev = StandardDeviation(values);
        var mean = values.Average();
        return new ConsistencyItemDto
        {
            StdDev = Math.Round(stdDev, 2),
            Consistency = mean > 0 ? Math.Round(Math.Max(0, 100 - (stdDev / mean * 100)), 1) : 100
        };
    }

    private static double StandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;
        var mean = values.Average();
        var sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(sumOfSquares / values.Count);
    }

    private static void AddHourlyAverages(StatisticsDto result, List<Speedtest> completed)
    {
        for (var hour = 0; hour < 24; hour++)
        {
            var inHour = completed.Where(e => e.Created.ToLocalTime().Hour == hour).ToList();
            var jitters = inHour.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();

            result.HourlyAverages.Add(new HourlyAverageDto
            {
                Hour = hour,
                Download = inHour.Count > 0 ? Math.Round(inHour.Average(e => e.Download), 2) : null,
                Upload = inHour.Count > 0 ? Math.Round(inHour.Average(e => e.Upload), 2) : null,
                Ping = inHour.Count > 0 ? (int)Math.Round(inHour.Average(e => e.Ping)) : null,
                Jitter = jitters.Count > 0 ? Math.Round(jitters.Average(), 2) : null,
                Count = inHour.Count
            });
        }
    }

    private static void AddChartPointPerResult(StatisticsDto result, List<Speedtest> entries)
    {
        foreach (var entry in entries)
        {
            var failed = entry.Status == "failed";
            var hasReadings = entry.Status == "completed";
            AddChartPoint(result, entry.Created, failed, failed ? entry.Error : null,
                hasReadings ? entry.Ping : null,
                hasReadings ? entry.Jitter : null,
                hasReadings ? entry.Download : null,
                hasReadings ? entry.Upload : null,
                hasReadings ? entry.Time : null);
        }
    }

    private static void AddChartPointPerTimeBucket(StatisticsDto result, List<Speedtest> entries, DateTime from, DateTime to)
    {
        var fromMs = new DateTimeOffset(from).ToUnixTimeMilliseconds();
        var toMs = new DateTimeOffset(to).ToUnixTimeMilliseconds();
        var bucketSize = (double)(toMs - fromMs) / MaxChartPoints;

        var buckets = Enumerable.Range(0, MaxChartPoints)
            .Select(i => (Start: fromMs + (long)(i * bucketSize), Entries: new List<Speedtest>()))
            .ToList();

        foreach (var entry in entries)
        {
            var entryMs = new DateTimeOffset(entry.Created).ToUnixTimeMilliseconds();
            var bucketIndex = (int)Math.Min(Math.Floor((entryMs - fromMs) / bucketSize), MaxChartPoints - 1);
            if (bucketIndex >= 0 && bucketIndex < MaxChartPoints)
            {
                buckets[bucketIndex].Entries.Add(entry);
            }
        }

        foreach (var bucket in buckets)
        {
            var valid = bucket.Entries.Where(e => e.Status == "completed").ToList();
            var failedCount = bucket.Entries.Count(e => e.Status == "failed");
            if (valid.Count == 0 && failedCount == 0) continue;

            var midpoint = DateTimeOffset.FromUnixTimeMilliseconds(bucket.Start + (long)(bucketSize / 2)).UtcDateTime;
            var failedSummary = failedCount > 0 ? $"{failedCount} failed in period" : null;

            if (valid.Count == 0)
            {
                AddChartPoint(result, midpoint, true, failedSummary, null, null, null, null, null);
                continue;
            }

            var jitters = valid.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();
            AddChartPoint(result, midpoint, failedCount > 0, failedSummary,
                (int)Math.Round(valid.Average(e => e.Ping)),
                jitters.Count > 0 ? Math.Round(jitters.Average(), 2) : null,
                Math.Round(valid.Average(e => e.Download), 2),
                Math.Round(valid.Average(e => e.Upload), 2),
                (int)Math.Round(valid.Average(e => e.Time)));
        }
    }

    private static void AddChartPoint(StatisticsDto result, DateTime time, bool failed, string? error,
        int? ping, double? jitter, double? download, double? upload, int? duration)
    {
        result.Labels.Add(time.ToString("o"));
        result.Failed.Add(failed);
        result.Errors.Add(error);
        result.Data.Ping.Add(ping);
        result.Data.Jitter.Add(jitter);
        result.Data.Download.Add(download);
        result.Data.Upload.Add(upload);
        result.Data.Time.Add(duration);
    }
}
