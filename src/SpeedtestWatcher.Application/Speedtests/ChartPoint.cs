namespace SpeedtestWatcher.Application.Speedtests;

public sealed record ChartPoint(
    DateTime Time,
    bool Failed,
    string? Error,
    int? Ping,
    double? Jitter,
    double? Download,
    double? Upload,
    int? Duration);
