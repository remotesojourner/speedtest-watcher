namespace SpeedtestWatcher.Application.Models;

public sealed record BufferbloatReading(
    double IdleMilliseconds,
    double LoadedMilliseconds,
    double LoadedTailMilliseconds,
    double? DownloadMilliseconds,
    double? UploadMilliseconds);
