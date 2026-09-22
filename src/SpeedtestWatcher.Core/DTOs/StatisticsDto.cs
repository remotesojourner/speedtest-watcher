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
