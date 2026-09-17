namespace SpeedtestWatcher.Core.DTOs;

public class MetricStatsDto<T>
{
    public T Min { get; set; } = default!;
    public T Max { get; set; } = default!;
    public T Avg { get; set; } = default!;
}

public class TestsCountDto
{
    public int Total { get; set; }
    public int Failed { get; set; }
}

public class ChartSeriesDataDto
{
    public List<int?> Ping { get; set; } = [];
    public List<double?> Jitter { get; set; } = [];
    public List<double?> Download { get; set; } = [];
    public List<double?> Upload { get; set; } = [];
    public List<int?> Time { get; set; } = [];
}

public class HourlyAverageDto
{
    public int Hour { get; set; }
    public double? Download { get; set; }
    public double? Upload { get; set; }
    public int? Ping { get; set; }
    public double? Jitter { get; set; }
    public int Count { get; set; }
}

public class ConsistencyItemDto
{
    public double StdDev { get; set; }
    public double Consistency { get; set; }
}

public class PingConsistencyDto
{
    public double StdDev { get; set; }
    public double Jitter { get; set; }
}

public class ConsistencyDto
{
    public ConsistencyItemDto Download { get; set; } = new();
    public ConsistencyItemDto Upload { get; set; } = new();
    public PingConsistencyDto Ping { get; set; } = new();
}

public class DateRangeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public int Days { get; set; }
}

public class StatisticsDto
{
    public TestsCountDto Tests { get; set; } = new();
    public MetricStatsDto<int>? Ping { get; set; }
    public MetricStatsDto<double>? Jitter { get; set; }
    public MetricStatsDto<double>? Download { get; set; }
    public MetricStatsDto<double>? Upload { get; set; }
    public MetricStatsDto<int>? Time { get; set; }
    public ChartSeriesDataDto Data { get; set; } = new();
    public List<string> Labels { get; set; } = [];
    public List<bool> Failed { get; set; } = [];
    public List<string?> Errors { get; set; } = [];
    public List<HourlyAverageDto> HourlyAverages { get; set; } = [];
    public ConsistencyDto Consistency { get; set; } = new();
    public int DataPoints { get; set; }
    public int RawDataPoints { get; set; }
    public bool Downsampled { get; set; }
    public DateRangeDto DateRange { get; set; } = new();
}
