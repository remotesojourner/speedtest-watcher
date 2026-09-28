using SpeedtestWatcher.Application.Enums;

namespace SpeedtestWatcher.Application.Models;

public readonly record struct LatencySample(double Milliseconds, LoadDirection Direction);
