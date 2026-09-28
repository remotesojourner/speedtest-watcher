using SpeedtestWatcher.Application.Models.Entities;

namespace SpeedtestWatcher.Application.Models;

public sealed record ResultsSummary(Speedtest? Latest, Speedtest? LatestCompleted, int Total);
