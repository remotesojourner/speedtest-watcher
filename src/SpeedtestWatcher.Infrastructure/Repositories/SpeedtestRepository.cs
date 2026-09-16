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

    // The History filters, shared by the list, its counts and exports so all three agree on what matches.
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
        int count = 0;
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
        // A plain date covers that whole day; a full timestamp is taken as given, so "last 24 hours" is exact.
        var from = ParseBound(fromDate, DateTime.UtcNow.AddDays(-7).Date, endOfDay: false);
        var to = ParseBound(toDate, DateTime.UtcNow, endOfDay: true);

        static DateTime ParseBound(string raw, DateTime fallback, bool endOfDay)
        {
            if (DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return endOfDay ? date.Date.AddDays(1).AddTicks(-1) : date.Date;

            // Timestamps come from the browser in local time; results are stored in UTC.
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal, out var moment))
                return moment;

            return fallback;
        }

        var dbEntries = await _db.Speedtests
            .Where(t => t.Created >= from && t.Created <= to)
            .OrderBy(t => t.Created)
            .ToListAsync(cancellationToken);

        // Averages are built from tests that actually measured something; a skipped test stores a
        // reason in Error but no readings, so it must not count as a failure or drag the numbers.
        var completedTests = dbEntries.Where(e => e.Status == "completed").ToList();
        var dataPointCount = dbEntries.Count;
        const int TARGET_CHART_POINTS = 300;

        var result = new StatisticsDto
        {
            Tests = new TestsCountDto
            {
                Total = dbEntries.Count,
                Failed = dbEntries.Count(e => e.Status == "failed")
            },
            RawDataPoints = dataPointCount,
            Downsampled = dataPointCount > TARGET_CHART_POINTS,
            DateRange = new DateRangeDto
            {
                From = fromDate,
                To = toDate,
                Days = (int)Math.Ceiling((to - from).TotalDays)
            }
        };

        if (completedTests.Count > 0)
        {
            var pings = completedTests.Select(e => e.Ping).ToList();
            var downloads = completedTests.Select(e => e.Download).ToList();
            var uploads = completedTests.Select(e => e.Upload).ToList();
            var times = completedTests.Select(e => e.Time).ToList();
            var jitters = completedTests.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();

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

            // Consistency calculation
            double CalcStdDev(IList<double> values)
            {
                if (values.Count < 2) return 0;
                double mean = values.Average();
                double sumOfSquares = values.Sum(v => Math.Pow(v - mean, 2));
                return Math.Sqrt(sumOfSquares / values.Count);
            }

            double dStdDev = CalcStdDev(downloads);
            double dMean = downloads.Average();
            result.Consistency.Download = new ConsistencyItemDto
            {
                StdDev = Math.Round(dStdDev, 2),
                Consistency = dMean > 0 ? Math.Round(Math.Max(0, 100 - (dStdDev / dMean * 100)), 1) : 100
            };

            double uStdDev = CalcStdDev(uploads);
            double uMean = uploads.Average();
            result.Consistency.Upload = new ConsistencyItemDto
            {
                StdDev = Math.Round(uStdDev, 2),
                Consistency = uMean > 0 ? Math.Round(Math.Max(0, 100 - (uStdDev / uMean * 100)), 1) : 100
            };

            double pStdDev = CalcStdDev(pings.Select(p => (double)p).ToList());
            result.Consistency.Ping = new PingConsistencyDto
            {
                StdDev = Math.Round(pStdDev, 2),
                Jitter = Math.Round(pStdDev, 2)
            };
        }

        // Hourly averages
        for (int h = 0; h < 24; h++)
        {
            var hourEntries = completedTests.Where(e => e.Created.ToLocalTime().Hour == h).ToList();
            var hourJitters = hourEntries.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();

            result.HourlyAverages.Add(new HourlyAverageDto
            {
                Hour = h,
                Download = hourEntries.Count > 0 ? Math.Round(hourEntries.Average(e => e.Download), 2) : null,
                Upload = hourEntries.Count > 0 ? Math.Round(hourEntries.Average(e => e.Upload), 2) : null,
                Ping = hourEntries.Count > 0 ? (int)Math.Round(hourEntries.Average(e => e.Ping)) : null,
                Jitter = hourJitters.Count > 0 ? Math.Round(hourJitters.Average(), 2) : null,
                Count = hourEntries.Count
            });
        }

        // Downsampling / Chart data calculation
        if (dbEntries.Count <= TARGET_CHART_POINTS)
        {
            foreach (var entry in dbEntries)
            {
                // A skipped test carries its reason in Error but is not a failure, so both
                // questions are answered by the status instead: only a completed test has
                // readings to plot, and only a failed one belongs in the failure list.
                bool isFailed = entry.Status == "failed";
                bool hasReadings = entry.Status == "completed";
                result.Labels.Add(entry.Created.ToString("o"));
                result.Failed.Add(isFailed);
                result.Errors.Add(isFailed ? entry.Error : null);

                result.Data.Ping.Add(hasReadings ? entry.Ping : null);
                result.Data.Jitter.Add(hasReadings ? entry.Jitter : null);
                result.Data.Download.Add(hasReadings ? entry.Download : null);
                result.Data.Upload.Add(hasReadings ? entry.Upload : null);
                result.Data.Time.Add(hasReadings ? entry.Time : null);
            }
        }
        else
        {
            long fromMs = new DateTimeOffset(from).ToUnixTimeMilliseconds();
            long toMs = new DateTimeOffset(to).ToUnixTimeMilliseconds();
            double bucketSize = (double)(toMs - fromMs) / TARGET_CHART_POINTS;

            var buckets = new List<(long startTime, long endTime, List<Speedtest> entries)>();
            for (int i = 0; i < TARGET_CHART_POINTS; i++)
            {
                long start = fromMs + (long)(i * bucketSize);
                long end = fromMs + (long)((i + 1) * bucketSize);
                buckets.Add((start, end, new List<Speedtest>()));
            }

            foreach (var entry in dbEntries)
            {
                long entryMs = new DateTimeOffset(entry.Created).ToUnixTimeMilliseconds();
                int bucketIndex = (int)Math.Min(Math.Floor((entryMs - fromMs) / bucketSize), TARGET_CHART_POINTS - 1);
                if (bucketIndex >= 0 && bucketIndex < TARGET_CHART_POINTS)
                {
                    buckets[bucketIndex].entries.Add(entry);
                }
            }

            foreach (var bucket in buckets)
            {
                var valid = bucket.entries.Where(e => e.Status == "completed").ToList();
                int failedCount = bucket.entries.Count(e => e.Status == "failed");

                if (valid.Count == 0)
                {
                    if (failedCount > 0)
                    {
                        var midDate = DateTimeOffset.FromUnixTimeMilliseconds(bucket.startTime + (long)(bucketSize / 2)).UtcDateTime;
                        result.Labels.Add(midDate.ToString("o"));
                        result.Failed.Add(true);
                        result.Errors.Add($"{failedCount} failed in period");
                        result.Data.Ping.Add(null);
                        result.Data.Jitter.Add(null);
                        result.Data.Download.Add(null);
                        result.Data.Upload.Add(null);
                        result.Data.Time.Add(null);
                    }
                    continue;
                }

                var midTime = DateTimeOffset.FromUnixTimeMilliseconds(bucket.startTime + (long)(bucketSize / 2)).UtcDateTime;
                var validJitters = valid.Where(e => e.Jitter.HasValue).Select(e => e.Jitter!.Value).ToList();

                result.Labels.Add(midTime.ToString("o"));
                result.Failed.Add(failedCount > 0);
                result.Errors.Add(failedCount > 0 ? $"{failedCount} failed in period" : null);

                result.Data.Ping.Add((int)Math.Round(valid.Average(e => e.Ping)));
                result.Data.Jitter.Add(validJitters.Count > 0 ? Math.Round(validJitters.Average(), 2) : null);
                result.Data.Download.Add(Math.Round(valid.Average(e => e.Download), 2));
                result.Data.Upload.Add(Math.Round(valid.Average(e => e.Upload), 2));
                result.Data.Time.Add((int)Math.Round(valid.Average(e => e.Time)));
            }
        }

        result.DataPoints = result.Labels.Count;
        return result;
    }
}
