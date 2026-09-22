using SpeedtestWatcher.Application.Speedtests;
using SpeedtestWatcher.Core.DTOs;

namespace SpeedtestWatcher.Web.Api;

public static class StatisticsMapping
{
    public static StatisticsDto ToDto(this SpeedtestStatistics statistics)
    {
        var dto = new StatisticsDto
        {
            Tests = statistics.Tests,
            Ping = statistics.Ping,
            Jitter = statistics.Jitter,
            Download = statistics.Download,
            Upload = statistics.Upload,
            Time = statistics.Time,
            HourlyAverages = statistics.HourlyAverages.ToList(),
            Consistency = statistics.Consistency,
            RawDataPoints = statistics.RawDataPoints,
            Downsampled = statistics.Downsampled,
            DateRange = statistics.DateRange,
            DataPoints = statistics.ChartPoints.Count
        };

        foreach (var point in statistics.ChartPoints)
        {
            dto.Labels.Add(point.Time.ToString("o"));
            dto.Failed.Add(point.Failed);
            dto.Errors.Add(point.Error);
            dto.Data.Ping.Add(point.Ping);
            dto.Data.Jitter.Add(point.Jitter);
            dto.Data.Download.Add(point.Download);
            dto.Data.Upload.Add(point.Upload);
            dto.Data.Time.Add(point.Duration);
        }

        return dto;
    }
}
