using System.Text.Json.Serialization;

namespace SpeedtestWatcher.Core.DTOs;

public class MetricStatsDto<T>
{
    [JsonPropertyName("min")]
    public T Min { get; set; } = default!;

    [JsonPropertyName("max")]
    public T Max { get; set; } = default!;

    [JsonPropertyName("avg")]
    public T Avg { get; set; } = default!;
}

public class TestsCountDto
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }
}

public class ChartSeriesDataDto
{
    [JsonPropertyName("ping")]
    public List<int?> Ping { get; set; } = [];

    [JsonPropertyName("jitter")]
    public List<double?> Jitter { get; set; } = [];

    [JsonPropertyName("download")]
    public List<double?> Download { get; set; } = [];

    [JsonPropertyName("upload")]
    public List<double?> Upload { get; set; } = [];

    [JsonPropertyName("time")]
    public List<int?> Time { get; set; } = [];
}

public class HourlyAverageDto
{
    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("download")]
    public double? Download { get; set; }

    [JsonPropertyName("upload")]
    public double? Upload { get; set; }

    [JsonPropertyName("ping")]
    public int? Ping { get; set; }

    [JsonPropertyName("jitter")]
    public double? Jitter { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class ConsistencyItemDto
{
    [JsonPropertyName("stdDev")]
    public double StdDev { get; set; }

    [JsonPropertyName("consistency")]
    public double Consistency { get; set; }
}

public class PingConsistencyDto
{
    [JsonPropertyName("stdDev")]
    public double StdDev { get; set; }

    [JsonPropertyName("jitter")]
    public double Jitter { get; set; }
}

public class ConsistencyDto
{
    [JsonPropertyName("download")]
    public ConsistencyItemDto Download { get; set; } = new();

    [JsonPropertyName("upload")]
    public ConsistencyItemDto Upload { get; set; } = new();

    [JsonPropertyName("ping")]
    public PingConsistencyDto Ping { get; set; } = new();
}

public class DateRangeDto
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("days")]
    public int Days { get; set; }
}

public class StatisticsDto
{
    [JsonPropertyName("tests")]
    public TestsCountDto Tests { get; set; } = new();

    [JsonPropertyName("ping")]
    public MetricStatsDto<int>? Ping { get; set; }

    [JsonPropertyName("jitter")]
    public MetricStatsDto<double>? Jitter { get; set; }

    [JsonPropertyName("download")]
    public MetricStatsDto<double>? Download { get; set; }

    [JsonPropertyName("upload")]
    public MetricStatsDto<double>? Upload { get; set; }

    [JsonPropertyName("time")]
    public MetricStatsDto<int>? Time { get; set; }

    [JsonPropertyName("data")]
    public ChartSeriesDataDto Data { get; set; } = new();

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = [];

    [JsonPropertyName("failed")]
    public List<bool> Failed { get; set; } = [];

    [JsonPropertyName("errors")]
    public List<string?> Errors { get; set; } = [];

    [JsonPropertyName("hourlyAverages")]
    public List<HourlyAverageDto> HourlyAverages { get; set; } = [];

    [JsonPropertyName("consistency")]
    public ConsistencyDto Consistency { get; set; } = new();

    [JsonPropertyName("dataPoints")]
    public int DataPoints { get; set; }

    [JsonPropertyName("rawDataPoints")]
    public int RawDataPoints { get; set; }

    [JsonPropertyName("downsampled")]
    public bool Downsampled { get; set; }

    [JsonPropertyName("dateRange")]
    public DateRangeDto DateRange { get; set; } = new();
}
