namespace SpeedtestWatcher.Application.Models;

public readonly record struct Readings(
    int Ping, double Download, double Upload, double? PacketLoss = null, double? BufferbloatDown = null, double? BufferbloatUp = null);
