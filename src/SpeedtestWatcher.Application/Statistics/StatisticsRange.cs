namespace SpeedtestWatcher.Application.Statistics;

public sealed record StatisticsRange(string From, string To, DateTime FromUtc, DateTime ToUtc, TimeZoneInfo TimeZone);
