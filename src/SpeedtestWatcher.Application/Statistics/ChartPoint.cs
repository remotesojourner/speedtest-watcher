namespace SpeedtestWatcher.Application.Statistics;

public sealed record ChartPoint(
    DateTime Time,
    bool Failed,
    string? Error,
    int? Ping,
    double? Jitter,
    double? Download,
    double? Upload,
    double? BufferbloatDown,
    double? BufferbloatUp,
    int? Duration);
