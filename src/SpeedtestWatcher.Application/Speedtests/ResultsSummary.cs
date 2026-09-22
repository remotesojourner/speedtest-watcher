namespace SpeedtestWatcher.Application.Speedtests;

public sealed record ResultsSummary(Speedtest? Latest, Speedtest? LatestCompleted, int Total);
